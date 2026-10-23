namespace OrisJewelCloudExport.Reporting;

/// <summary>Everything the HTML report shows, computed once per run and embedded in the page as JSON.</summary>
public sealed record ExportReport(
    DateTimeOffset GeneratedAt,
    string ApiUrl,
    string CsvFileName,
    int RecordCount,
    IReadOnlyList<ColumnCoverage> Columns,
    IReadOnlyList<ValueBreakdown> MappedValues,
    IReadOnlyList<ValueBreakdown> SourceOnlyValues,
    PriceSummary Price,
    IReadOnlyList<SourceColumnFill> SourceColumns,
    IReadOnlyList<QualityIssue> Quality,
    IReadOnlyList<BlankReason> BlankReasons);

public sealed record ColumnCoverage(string Name, string Treatment, string? Source, string Rule, int Populated);

/// <summary>Value counts for one column, most frequent first. Blank cells are counted under a null label.</summary>
public sealed record ValueBreakdown(string Column, string Note, IReadOnlyList<ValueCount> Values);

public sealed record ValueCount(string? Label, int Count);

public sealed record PriceSummary(
    string Currency,
    bool IncludeInactivePrices,
    int Exported,
    int ExcludedInactive,
    int MissingPrice,
    int OtherCurrency,
    int InvalidAmount,
    IReadOnlyList<decimal> ExportedAmounts);

public sealed record SourceColumnFill(string Name, int Filled);

public sealed record QualityIssue(string Issue, int Records, string Handling);

public sealed record BlankReason(string Column, string Reason, int Count);
