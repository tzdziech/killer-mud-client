using System.Net;
using System.Text;
using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class AppUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdateAsync_NewerVersionPublished_ReturnsAvailability()
    {
        var client = new HttpClient(new StaticHandler(_ => JsonResponse("""
            { "schemaVersion": 1, "version": "0.5.0", "prerelease": true }
            """)));
        var service = new AppUpdateService(client, new Uri("https://example.test/app-version.json"), "0.4.0");

        var update = await service.CheckForUpdateAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(update);
        Assert.Equal("0.5.0", update.Version);
        Assert.True(update.Prerelease);
        Assert.Equal("https://grzyboll.github.io/killer-mud-client/index.html", update.DownloadPageUri.ToString());
    }

    [Fact]
    public async Task CheckForUpdateAsync_SameOrOlderVersionPublished_ReturnsNull()
    {
        var client = new HttpClient(new StaticHandler(_ => JsonResponse("""
            { "schemaVersion": 1, "version": "0.4.0", "prerelease": false }
            """)));
        var service = new AppUpdateService(client, new Uri("https://example.test/app-version.json"), "0.4.0");

        Assert.Null(await service.CheckForUpdateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckForUpdateAsync_DevSuffixIgnoredForComparison_TreatsSameCoreVersionAsUpToDate()
    {
        // "-dev.N" is stripped for comparison (see ParseVersion) — a dev build of the same
        // core version must not perpetually nag about "0.4.0" being newer than "0.4.0-dev.3".
        var client = new HttpClient(new StaticHandler(_ => JsonResponse("""
            { "schemaVersion": 1, "version": "0.4.0", "prerelease": true }
            """)));
        var service = new AppUpdateService(client, new Uri("https://example.test/app-version.json"), "0.4.0-dev.3");

        Assert.Null(await service.CheckForUpdateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckForUpdateAsync_UnsupportedSchemaVersion_ReturnsNull()
    {
        var client = new HttpClient(new StaticHandler(_ => JsonResponse("""
            { "schemaVersion": 2, "version": "9.9.9", "prerelease": false }
            """)));
        var service = new AppUpdateService(client, new Uri("https://example.test/app-version.json"), "0.4.0");

        Assert.Null(await service.CheckForUpdateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckForUpdateAsync_MissingVersionField_ReturnsNull()
    {
        var client = new HttpClient(new StaticHandler(_ => JsonResponse("""
            { "schemaVersion": 1, "prerelease": false }
            """)));
        var service = new AppUpdateService(client, new Uri("https://example.test/app-version.json"), "0.4.0");

        Assert.Null(await service.CheckForUpdateAsync(TestContext.Current.CancellationToken));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class StaticHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
