using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Mapping;
using OrisJewelCloudExport.Output;
using OrisJewelCloudExport.Reporting;
using OrisJewelCloudExport.Source;

namespace OrisJewelCloudExport;

/// <summary>
/// The export pipeline: download → map to JewelCloud columns → append SRC_ columns → write CSV → report.
/// </summary>
public sealed class ExportJob(OrisApiClient apiClient, ExportSettings settings)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Log.Info($"Downloading watches from {settings.ApiUrl} ...");
        var records = await apiClient.FetchWatchesAsync(cancellationToken);
        if (records.Count == 0)
            throw new OrisApiException("The Oris API returned no watches; the existing output file was left untouched.");
        Log.Info($"Received {records.Count} watch records.");

        var diagnostics = new MappingDiagnostics();
        var mapper = new TimepieceMapper(settings, diagnostics);
        var watches = records.Select(OrisWatch.FromJson).ToList();
        var mappedRows = watches.Select(mapper.Map).ToList();
        var source = SourceTable.Build(records);

        var header = TimepieceColumns.All.Concat(source.Columns).ToList();
        var rows = mappedRows.Zip(source.Rows, (mapped, src) =>
            (IReadOnlyList<string?>)TimepieceColumns.All.Select(c => mapped.GetValueOrDefault(c))
                .Concat(source.Columns.Select(c => src.GetValueOrDefault(c)))
                .ToList());

        CsvFileWriter.Write(settings.OutputPath, header, rows);

        WarnOnDuplicateStyleNumbers(mappedRows);
        PrintSummary(mappedRows, source, diagnostics);
        Log.Info($"Wrote {mappedRows.Count} rows ({TimepieceColumns.All.Count} JewelCloud + {source.Columns.Count} SRC_ columns) to {Path.GetFullPath(settings.OutputPath)}");

        if (!string.IsNullOrWhiteSpace(settings.ReportPath))
        {
            var report = ExportReportBuilder.Build(settings, watches, mappedRows, source, diagnostics, DateTimeOffset.UtcNow);
            HtmlReportWriter.Write(settings.ReportPath, report);
            Log.Info($"Wrote HTML report to {Path.GetFullPath(settings.ReportPath)}");
        }
    }

    private static void WarnOnDuplicateStyleNumbers(List<IReadOnlyDictionary<string, string?>> rows)
    {
        var duplicates = rows
            .Select(r => r.GetValueOrDefault(TimepieceColumns.StyleNumber))
            .Where(s => s is not null)
            .GroupBy(s => s, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
            Log.Warn($"{duplicates.Count} StyleNumber values occur more than once, e.g. '{duplicates[0]}'.");

        int missing = rows.Count(r => r.GetValueOrDefault(TimepieceColumns.StyleNumber) is null);
        if (missing > 0)
            Log.Warn($"{missing} rows have no StyleNumber (source 'reference' missing); JewelCloud cannot import them.");
    }

    private static void PrintSummary(List<IReadOnlyDictionary<string, string?>> rows, SourceTable source, MappingDiagnostics diagnostics)
    {
        Log.Info("");
        Log.Info("JewelCloud column coverage (rows populated):");
        foreach (string column in TimepieceColumns.All)
        {
            int filled = rows.Count(r => !string.IsNullOrEmpty(r.GetValueOrDefault(column)));
            Log.Info($"  {column,-20} {filled,5} / {rows.Count}");
        }

        var notes = diagnostics.Entries.ToList();
        if (notes.Count > 0)
        {
            Log.Info("");
            Log.Info("Blank-value reasons (column: reason x count):");
            foreach (var (column, message, count) in notes)
                Log.Info($"  {column}: {message.ReplaceLineEndings(" ")} x {count}");
        }

        Log.Info("");
        Log.Info($"SRC_ columns ({source.Columns.Count}): {string.Join(", ", source.Columns)}");
        Log.Info("");
    }
}
