using Microsoft.Extensions.Configuration;

namespace OrisJewelCloudExport.Configuration;

/// <summary>
/// Run settings, read from appsettings.json (section "Export") and overridable from the command line.
/// </summary>
public sealed class ExportSettings
{
    public const string SectionName = "Export";

    private static readonly Dictionary<string, string> CommandLineSwitches = new()
    {
        ["--url"] = $"{SectionName}:{nameof(ApiUrl)}",
        ["--output"] = $"{SectionName}:{nameof(OutputPath)}",
        ["--report"] = $"{SectionName}:{nameof(ReportPath)}",
        ["--include-inactive-prices"] = $"{SectionName}:{nameof(IncludeInactivePrices)}",
    };

    public string ApiUrl { get; set; } = "https://product-api.oris.ch/api/watch/all";

    /// <summary>Relative paths are resolved against the current working directory.</summary>
    public string OutputPath { get; set; } = "output/oris-jewelcloud-timepieces.csv";

    /// <summary>HTML report of the run (coverage, value breakdowns, data quality). Empty = no report.</summary>
    public string ReportPath { get; set; } = "output/oris-jewelcloud-report.html";

    public int HttpTimeoutSeconds { get; set; } = 120;

    /// <summary>Total download attempts, including the first one. Only transient failures are retried.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>RetailPrice is only populated from Oris prices in this currency.</summary>
    public string PriceCurrency { get; set; } = "USD";

    /// <summary>
    /// When false (default), RetailPrice is left blank unless Oris marks the price as active
    /// (<c>price.active = true</c>). Inactive prices are still preserved in the SRC_price_* columns.
    /// </summary>
    public bool IncludeInactivePrices { get; set; }

    public static ExportSettings Load(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddCommandLine(args, CommandLineSwitches)
            .Build();

        var settings = configuration.GetSection(SectionName).Get<ExportSettings>() ?? new ExportSettings();
        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException($"{nameof(ApiUrl)} must be an absolute http(s) URL but was '{ApiUrl}'.");
        if (string.IsNullOrWhiteSpace(OutputPath))
            throw new InvalidOperationException($"{nameof(OutputPath)} is required.");
        if (HttpTimeoutSeconds <= 0)
            throw new InvalidOperationException($"{nameof(HttpTimeoutSeconds)} must be greater than 0.");
        if (MaxAttempts < 1)
            throw new InvalidOperationException($"{nameof(MaxAttempts)} must be at least 1.");
        if (string.IsNullOrWhiteSpace(PriceCurrency))
            throw new InvalidOperationException($"{nameof(PriceCurrency)} is required.");
    }
}
