using System.Text;
using OrisJewelCloudExport;
using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Source;

Console.OutputEncoding = Encoding.UTF8;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

ExportSettings settings;
try
{
    settings = ExportSettings.Load(args);
}
catch (Exception ex) when (ex is InvalidOperationException or FormatException or InvalidDataException)
{
    // Validation failures, unconvertible values (e.g. MaxAttempts = "abc") or malformed appsettings.json
    Log.Error($"Invalid configuration: {ex.Message}");
    return ExitCodes.ConfigurationError;
}

try
{
    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds) };
    var apiClient = new OrisApiClient(httpClient, new Uri(settings.ApiUrl), settings.MaxAttempts, retryBaseDelay: TimeSpan.FromSeconds(2));
    await new ExportJob(apiClient, settings).RunAsync(cancellation.Token);
    return ExitCodes.Success;
}
catch (OrisApiException ex)
{
    Log.Error(ex.Message);
    return ExitCodes.SourceError;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Log.Error("Cancelled by user.");
    return ExitCodes.Cancelled;
}
catch (Exception ex)
{
    Log.Error($"Unexpected error: {ex}");
    return ExitCodes.UnexpectedError;
}

internal static class ExitCodes
{
    public const int Success = 0;
    public const int UnexpectedError = 1;
    public const int SourceError = 2;
    public const int ConfigurationError = 3;
    public const int Cancelled = 4;
}
