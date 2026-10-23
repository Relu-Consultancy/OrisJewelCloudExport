using System.Globalization;
using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Mapping;
using OrisJewelCloudExport.Source;
using C = OrisJewelCloudExport.Mapping.TimepieceColumns;

namespace OrisJewelCloudExport.Reporting;

/// <summary>Summarises one export run (mapped rows, raw source and diagnostics) for the HTML report.</summary>
public static class ExportReportBuilder
{
    private const int MaxBarsPerBreakdown = 12;

    // Mapped columns whose value distribution is worth showing; free-text columns are left out.
    private static readonly string[] BreakdownColumns =
        [C.IsActive, C.MetalType, C.WatchCaseMaterial, C.WatchEnergy, C.WatchMovement, C.Collections];

    public static ExportReport Build(
        ExportSettings settings,
        IReadOnlyList<OrisWatch> watches,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows,
        SourceTable source,
        MappingDiagnostics diagnostics,
        DateTimeOffset generatedAt)
    {
        var catalog = TimepieceColumnCatalog.All.ToDictionary(c => c.Name);

        var columns = TimepieceColumnCatalog.All
            .Select(c => new ColumnCoverage(c.Name, c.Treatment.ToString(), c.Source, c.Rule,
                rows.Count(r => !string.IsNullOrEmpty(r.GetValueOrDefault(c.Name)))))
            .ToList();

        var mappedValues = BreakdownColumns
            .Select(column => new ValueBreakdown(column, catalog[column].Rule,
                CountValues(rows.Select(r => r.GetValueOrDefault(column)))))
            .ToList();

        var sourceOnlyValues = new List<ValueBreakdown>
        {
            new("color", $"Not mapped to {C.WatchDialColor}: {catalog[C.WatchDialColor].Rule}.",
                CountValues(source.Rows.Select(r => OrisValueNormalizer.CleanText(r.GetValueOrDefault("SRC_color"))))),
            new("size", $"Not mapped to {C.WatchSize}: {catalog[C.WatchSize].Rule}.",
                CountValues(source.Rows.Select(r => OrisValueNormalizer.CleanText(r.GetValueOrDefault("SRC_size"))))),
        };

        var sourceColumns = source.Columns
            .Select(c => new SourceColumnFill(c, source.Rows.Count(r => !string.IsNullOrEmpty(r.GetValueOrDefault(c)))))
            .ToList();

        var blankReasons = diagnostics.Entries
            .Select(e => new BlankReason(e.Column, e.Message.ReplaceLineEndings(" "), e.Count))
            .ToList();

        return new ExportReport(
            generatedAt,
            settings.ApiUrl,
            Path.GetFileName(settings.OutputPath),
            rows.Count,
            columns,
            mappedValues,
            sourceOnlyValues,
            SummarisePrices(settings, watches, rows),
            sourceColumns,
            FindQualityIssues(source),
            blankReasons);
    }

    /// <summary>Most frequent values first; the long tail is folded into one "other" bar. Null = blank cell.</summary>
    private static List<ValueCount> CountValues(IEnumerable<string?> values)
    {
        var counts = values
            .GroupBy(v => string.IsNullOrEmpty(v) ? null : v)
            .Select(g => new ValueCount(g.Key, g.Count()))
            .OrderByDescending(v => v.Label is not null)
            .ThenByDescending(v => v.Count)
            .ThenBy(v => v.Label, StringComparer.Ordinal)
            .ToList();

        var present = counts.Where(v => v.Label is not null).ToList();
        if (present.Count <= MaxBarsPerBreakdown)
            return counts;

        var head = present.Take(MaxBarsPerBreakdown - 1).ToList();
        var tail = present.Skip(MaxBarsPerBreakdown - 1).ToList();
        head.Add(new ValueCount($"{tail.Count} other values", tail.Sum(v => v.Count)));
        head.AddRange(counts.Where(v => v.Label is null));
        return head;
    }

    private static PriceSummary SummarisePrices(
        ExportSettings settings, IReadOnlyList<OrisWatch> watches, IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        // Same order of checks as TimepieceMapper.MapRetailPrice, so the groups add up to the record count.
        var exported = rows
            .Select(r => r.GetValueOrDefault(C.RetailPrice))
            .Where(p => p is not null)
            .Select(p => decimal.Parse(p!, CultureInfo.InvariantCulture))
            .ToList();
        int missing = watches.Count(w => w.Price is null);
        int otherCurrency = watches.Count(w => w.Price is not null
            && !string.Equals(OrisValueNormalizer.CleanText(w.Price.Currency), settings.PriceCurrency, StringComparison.OrdinalIgnoreCase));
        int excludedInactive = settings.IncludeInactivePrices ? 0 : watches.Count(w => w.Price is not null
            && string.Equals(OrisValueNormalizer.CleanText(w.Price.Currency), settings.PriceCurrency, StringComparison.OrdinalIgnoreCase)
            && w.Price.Active != true);
        int invalid = watches.Count - exported.Count - missing - otherCurrency - excludedInactive;

        return new PriceSummary(settings.PriceCurrency, settings.IncludeInactivePrices,
            exported.Count, excludedInactive, missing, otherCurrency, invalid, exported);
    }

    private static List<QualityIssue> FindQualityIssues(SourceTable source)
    {
        int Count(Func<string, bool> predicate) => source.Rows.Count(r => r.Values.Any(predicate));

        return new List<QualityIssue>
        {
            new("Leading or trailing whitespace", Count(v => v.Length > 0 && v != v.Trim()),
                "Trimmed in mapped columns; kept verbatim in SRC_"),
            new("Line break inside a value", Count(v => v.Contains('\r') || v.Contains('\n')),
                "Trimmed in mapped columns; quoted in the CSV"),
            new("Double quote inside a value", Count(v => v.Contains('"')),
                "Escaped as \"\" in the CSV"),
            new("Non-ASCII characters", Count(v => v.Any(ch => ch > 127)),
                "Kept; the CSV is UTF-8 with BOM"),
            new("Image URL contains spaces",
                source.Rows.Count(r => (r.GetValueOrDefault("SRC_imageUrl") ?? "").Contains(' ')
                                       || (r.GetValueOrDefault("SRC_image2Url") ?? "").Contains(' ')),
                "Percent-encoded in ImagePath and ImagePath2"),
        }.Where(issue => issue.Records > 0).ToList();
    }
}
