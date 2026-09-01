using System.Text.Json;
using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class TelnetLineCaptureTests : IAsyncLifetime
{
    private readonly string _directory = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "KillerMudClient_TelnetCaptureTest_" + Guid.NewGuid().ToString("N"));

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DisposeAsync_WritesQueuedLinesAsUtf8JsonLines()
    {
        var capture = new TelnetLineCapture(_directory);

        Assert.True(capture.TryRecord("Zdobywasz 12 500 punktów doświadczenia za trolla."));
        Assert.True(capture.TryRecord("Tekst z cudzysłowem: \"próba\""));
        await capture.DisposeAsync();

        var lines = await File.ReadAllLinesAsync(capture.Path, TestContext.Current.CancellationToken);
        Assert.Equal(2, lines.Length);
        Assert.Equal(
            "Zdobywasz 12 500 punktów doświadczenia za trolla.",
            JsonDocument.Parse(lines[0]).RootElement.GetProperty("Line").GetString());
        Assert.Equal(
            "Tekst z cudzysłowem: \"próba\"",
            JsonDocument.Parse(lines[1]).RootElement.GetProperty("Line").GetString());
        Assert.True(JsonDocument.Parse(lines[0]).RootElement.TryGetProperty("TimestampUtc", out _));
    }

    [Fact]
    public async Task TryRecord_AfterDispose_ReturnsFalse()
    {
        var capture = new TelnetLineCapture(_directory);
        await capture.DisposeAsync();

        Assert.False(capture.TryRecord("nie zapisuj"));
        Assert.Empty(await File.ReadAllLinesAsync(capture.Path, TestContext.Current.CancellationToken));
    }
}
