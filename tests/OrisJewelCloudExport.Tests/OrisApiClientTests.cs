using System.Net;
using OrisJewelCloudExport.Source;

namespace OrisJewelCloudExport.Tests;

public class OrisApiClientTests
{
    private static readonly Uri Endpoint = new("https://example.test/api/watch/all");

    [Fact]
    public void Rejects_invalid_json() =>
        Assert.Throws<OrisApiException>(() => OrisApiClient.ParseWatches("<html>error</html>"));

    [Fact]
    public void Rejects_a_response_that_is_not_an_array() =>
        Assert.Throws<OrisApiException>(() => OrisApiClient.ParseWatches("""{"message":"maintenance"}"""));

    [Fact]
    public void Skips_array_items_that_are_not_objects()
    {
        var watches = OrisApiClient.ParseWatches("""[{"id":1}, null, 42, {"id":2}]""");
        Assert.Equal(2, watches.Count);
    }

    [Fact]
    public async Task Retries_transient_failures_then_succeeds()
    {
        var handler = new StubHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var client = new OrisApiClient(new HttpClient(handler), Endpoint, maxAttempts: 3, retryBaseDelay: TimeSpan.Zero);

        var watches = await client.FetchWatchesAsync(CancellationToken.None);

        Assert.Single(watches);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Does_not_retry_client_errors()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound);
        var client = new OrisApiClient(new HttpClient(handler), Endpoint, maxAttempts: 3, retryBaseDelay: TimeSpan.Zero);

        await Assert.ThrowsAsync<OrisApiException>(() => client.FetchWatchesAsync(CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Gives_up_after_max_attempts()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError);
        var client = new OrisApiClient(new HttpClient(handler), Endpoint, maxAttempts: 3, retryBaseDelay: TimeSpan.Zero);

        await Assert.ThrowsAsync<OrisApiException>(() => client.FetchWatchesAsync(CancellationToken.None));
        Assert.Equal(3, handler.Calls);
    }

    /// <summary>Returns the given status codes in order (repeating the last one); 200 responses carry one watch.</summary>
    private sealed class StubHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = statuses[Math.Min(Calls, statuses.Length - 1)];
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(status == HttpStatusCode.OK ? """[{"id":1}]""" : "error"),
            });
        }
    }
}
