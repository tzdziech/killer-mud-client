using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace MudClient.App.Services;

/// <summary>
/// Temporary diagnostic capture of complete incoming Telnet text lines. Lines are queued so
/// filesystem writes never block MudSession's receive loop. Outgoing commands are deliberately
/// outside this service's API and therefore cannot be captured accidentally.
/// </summary>
public sealed class TelnetLineCapture : IAsyncDisposable
{
    private readonly Channel<CapturedTelnetLine> _lines = Channel.CreateUnbounded<CapturedTelnetLine>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    private readonly Task _writerTask;
    private int _completed;

    public TelnetLineCapture(string directory, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        var clock = timeProvider ?? TimeProvider.System;
        var startedAt = clock.GetUtcNow();
        Directory.CreateDirectory(directory);
        Path = System.IO.Path.Combine(
            directory,
            $"telnet-{startedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.jsonl");
        TimeProvider = clock;
        _writerTask = WriteLinesAsync();
    }

    public string Path { get; }

    private TimeProvider TimeProvider { get; }

    /// <summary>Queues one complete incoming line. Returns false after capture has stopped.</summary>
    public bool TryRecord(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return Volatile.Read(ref _completed) == 0
               && _lines.Writer.TryWrite(new CapturedTelnetLine(TimeProvider.GetUtcNow(), line));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
        {
            _lines.Writer.TryComplete();
        }

        await _writerTask.ConfigureAwait(false);
    }

    private async Task WriteLinesAsync()
    {
        await using var stream = new FileStream(
            Path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        await foreach (var entry in _lines.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(entry)).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }

    private sealed record CapturedTelnetLine(DateTimeOffset TimestampUtc, string Line);
}
