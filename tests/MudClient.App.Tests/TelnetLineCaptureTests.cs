using System.Text.Json;
using MudClient.App.Services;
using MudClient.Core.Gmcp;

namespace MudClient.App.Tests;

public sealed class TelnetLineCaptureTests : IAsyncLifetime
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "KillerMudClient_CombatCaptureTest_" + Guid.NewGuid().ToString("N"));

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsync_WritesBothSourcesWithSharedOrderedFormat()
    {
        var capture = new TelnetLineCapture(_directory);

        Assert.True(capture.TryRecord("Zdobywasz 12 500 punktów doświadczenia."));
        Assert.True(capture.TryRecord(new GmcpMessage("Char.Vitals", """{"hp":123}""")));
        Assert.True(capture.TryRecord("Tekst z cudzysłowem: \"próba\""));
        await capture.DisposeAsync();

        var documents = (await File.ReadAllLinesAsync(capture.Path, TestContext.Current.CancellationToken))
            .Select(line => JsonDocument.Parse(line))
            .ToArray();
        try
        {
            Assert.Equal([1L, 2L, 3L], documents.Select(d => d.RootElement.GetProperty("seq").GetInt64()));
            Assert.Equal(["TELNET", "GMCP", "TELNET"], documents.Select(d => d.RootElement.GetProperty("source").GetString()));
            var monoTicks = documents.Select(d => d.RootElement.GetProperty("monoTicks").GetInt64()).ToArray();
            Assert.True(monoTicks.SequenceEqual(monoTicks.Order()));
            Assert.All(documents, document =>
            {
                var root = document.RootElement;
                Assert.Equal(capture.SessionId, root.GetProperty("sessionId").GetString());
                Assert.Equal(JsonValueKind.String, root.GetProperty("tsUtc").ValueKind);
                Assert.Equal(JsonValueKind.Number, root.GetProperty("monoTicks").ValueKind);
            });
            Assert.Equal("Zdobywasz 12 500 punktów doświadczenia.", documents[0].RootElement.GetProperty("text").GetString());
            Assert.Equal("Char.Vitals", documents[1].RootElement.GetProperty("package").GetString());
            Assert.Equal("""{"hp":123}""", documents[1].RootElement.GetProperty("json").GetString());
            Assert.False(documents[0].RootElement.TryGetProperty("package", out _));
            Assert.Matches(@"combat-\d{8}-\d{6}-[a-f0-9]{32}\.jsonl$", capture.Path);
        }
        finally
        {
            foreach (var document in documents) document.Dispose();
        }
    }

    [Fact]
    public async Task TryRecord_AfterDispose_ReturnsFalse()
    {
        var capture = new TelnetLineCapture(_directory);
        await capture.DisposeAsync();

        Assert.False(capture.TryRecord("nie zapisuj"));
        Assert.False(capture.TryRecord(new GmcpMessage("Room.Info", "{}")));
        Assert.Empty(await File.ReadAllLinesAsync(capture.Path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Coordinator_DoesNotCreateOrWriteBeforeNamedVitals()
    {
        var coordinator = new CombatSessionCaptureCoordinator(_directory);

        coordinator.RecordTelnet("Login:");
        coordinator.ObserveGmcp(new GmcpMessage("Core.Hello", "{}"));
        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"hp":10}"""));
        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", "not-json"));

        Assert.Null(coordinator.ActivePath);
        Assert.False(Directory.Exists(_directory));
        Assert.Null(await coordinator.StopAsync());
    }

    [Fact]
    public async Task Coordinator_StartsOnNamedVitals_RecordsInitiatingGmcpAndFollowingTerminal()
    {
        var coordinator = new CombatSessionCaptureCoordinator(_directory);
        coordinator.RecordTelnet("Hasło: sekret");

        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"name":"Aldur","hp":10}"""));
        var path = Assert.IsType<string>(coordinator.ActivePath);
        coordinator.RecordTelnet("Witaj w świecie.");
        Assert.Equal(path, await coordinator.StopAsync());

        var lines = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(2, lines.Length);
        using var first = JsonDocument.Parse(lines[0]);
        using var second = JsonDocument.Parse(lines[1]);
        Assert.Equal("GMCP", first.RootElement.GetProperty("source").GetString());
        Assert.Equal("Char.Vitals", first.RootElement.GetProperty("package").GetString());
        Assert.Equal("TELNET", second.RootElement.GetProperty("source").GetString());
        Assert.Equal("Witaj w świecie.", second.RootElement.GetProperty("text").GetString());
        Assert.DoesNotContain("sekret", string.Join('\n', lines));
    }

    [Fact]
    public async Task Coordinator_ConnectionCloseFlushesAndAllowsNextConnectionToAutoStart()
    {
        var coordinator = new CombatSessionCaptureCoordinator(_directory);
        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"name":"Aldur"}"""));
        coordinator.RecordTelnet("pierwsza sesja");
        var firstPath = await coordinator.StopAsync(connectionClosed: true);

        coordinator.RecordTelnet("między sesjami");
        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"name":"Aldur"}"""));
        coordinator.RecordTelnet("druga sesja");
        var secondPath = await coordinator.StopAsync(connectionClosed: true);

        Assert.NotEqual(firstPath, secondPath);
        Assert.Equal(2, (await File.ReadAllLinesAsync(firstPath!, TestContext.Current.CancellationToken)).Length);
        Assert.Equal(2, (await File.ReadAllLinesAsync(secondPath!, TestContext.Current.CancellationToken)).Length);
    }

    [Fact]
    public async Task Coordinator_ManualStopPreventsAutomaticRestartUntilConnectionCloses()
    {
        var coordinator = new CombatSessionCaptureCoordinator(_directory);
        coordinator.StartManual();
        await coordinator.StopAsync();

        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"name":"Aldur"}"""));
        Assert.Null(coordinator.ActivePath);

        await coordinator.StopAsync(connectionClosed: true);
        coordinator.ObserveGmcp(new GmcpMessage("Char.Vitals", """{"name":"Aldur"}"""));
        Assert.NotNull(coordinator.ActivePath);
        await coordinator.StopAsync(connectionClosed: true);
    }
}
