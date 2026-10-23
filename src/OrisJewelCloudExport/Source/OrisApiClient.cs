using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OrisJewelCloudExport.Source;

/// <summary>
/// Downloads the Oris watch catalogue and returns every record as raw JSON, so that no source
/// property is lost before the SRC_ columns are built.
/// </summary>
public sealed class OrisApiClient(HttpClient httpClient, Uri endpoint, int maxAttempts, TimeSpan retryBaseDelay)
{
    public async Task<IReadOnlyList<JsonObject>> FetchWatchesAsync(CancellationToken cancellationToken)
    {
        string body = await DownloadAsync(cancellationToken);
        return ParseWatches(body);
    }

    /// <summary>
    /// GETs the endpoint, retrying transient failures (network errors, timeouts, HTTP 408/429/5xx)
    /// with exponential back-off. Other HTTP errors fail immediately.
    /// </summary>
    private async Task<string> DownloadAsync(CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(endpoint, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(cancellationToken);

                string status = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                if (!IsTransient(response.StatusCode) || attempt >= maxAttempts)
                    throw new OrisApiException($"Oris API returned {status} for {endpoint}.");

                Log.Warn($"Oris API returned {status} (attempt {attempt}/{maxAttempts}); retrying.");
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                Log.Warn($"Oris API request failed: {ex.Message} (attempt {attempt}/{maxAttempts}); retrying.");
            }
            catch (HttpRequestException ex)
            {
                throw new OrisApiException($"Could not reach the Oris API at {endpoint}: {ex.Message}", ex);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                Log.Warn($"Oris API request timed out (attempt {attempt}/{maxAttempts}); retrying.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new OrisApiException($"Oris API request timed out after {httpClient.Timeout.TotalSeconds:0}s.", ex);
            }

            await Task.Delay(retryBaseDelay * Math.Pow(2, attempt - 1), cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;

    /// <summary>
    /// Validates the response shape: it must be a JSON array. Array items that are not JSON objects
    /// are skipped with a warning rather than failing the whole export.
    /// </summary>
    internal static IReadOnlyList<JsonObject> ParseWatches(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new OrisApiException($"Oris API response is not valid JSON: {ex.Message}", ex);
        }

        if (root is not JsonArray items)
            throw new OrisApiException($"Expected a JSON array of watches but received {Describe(root)}.");

        var watches = new List<JsonObject>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is JsonObject watch)
                watches.Add(watch);
            else
                Log.Warn($"Skipped item {i} of the API response: expected a JSON object but found {Describe(items[i])}.");
        }
        return watches;
    }

    private static string Describe(JsonNode? node) => node?.GetValueKind().ToString() ?? "null";
}
