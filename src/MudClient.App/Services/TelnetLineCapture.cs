using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using MudClient.Core.Gmcp;

namespace MudClient.App.Services;

/// <summary>
/// Writes one ordered JSONL stream containing incoming terminal lines and GMCP messages.
/// The historical name is retained because this replaces the former Telnet-only diagnostic
/// capture instead of introducing a second, competing capture path.
/// </summary>
public sealed class TelnetLineCapture : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly Channel<CaptureEntry> _entries = Channel.CreateUnbounded<CaptureEntry>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly object _recordLock = new();
    private readonly Task _writerTask;
    private long _sequence;
    private int _completed;

    public TelnetLineCapture(string directory, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        TimeProvider = timeProvider ?? TimeProvider.System;
        var startedAt = TimeProvider.GetUtcNow();
        SessionId = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        Path = System.IO.Path.Combine(directory, $"combat-{startedAt:yyyyMMdd-HHmmss}-{SessionId}.jsonl");
        _writerTask = WriteEntriesAsync();
    }

    public string Path { get; }
    public string SessionId { get; }
    private TimeProvider TimeProvider { get; }

    /// <summary>Queues one complete incoming line after Telnet decoding.</summary>
    public bool TryRecord(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return TryRecord((seq, tsUtc, monoTicks) => new CaptureEntry(
            seq, tsUtc, monoTicks, "TELNET", SessionId, Text: line));
    }

    /// <summary>Queues raw GMCP before application resolvers process it.</summary>
    public bool TryRecord(GmcpMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return TryRecord((seq, tsUtc, monoTicks) => new CaptureEntry(
            seq, tsUtc, monoTicks, "GMCP", SessionId, Package: message.Package, Json: message.Json));
    }

    public async ValueTask DisposeAsync()
    {
        lock (_recordLock)
        {
            if (Interlocked.Exchange(ref _completed, 1) == 0)
            {
                _entries.Writer.TryComplete();
            }
        }

        await _writerTask.ConfigureAwait(false);
    }

    private bool TryRecord(Func<long, DateTimeOffset, long, CaptureEntry> createEntry)
    {
        lock (_recordLock)
        {
            if (Volatile.Read(ref _completed) != 0)
            {
                return false;
            }

            var entry = createEntry(++_sequence, TimeProvider.GetUtcNow(), TimeProvider.GetTimestamp());
            return _entries.Writer.TryWrite(entry);
        }
    }

    private async Task WriteEntriesAsync()
    {
        await using var stream = new FileStream(
            Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        await foreach (var entry in _entries.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(entry, JsonOptions)).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
    }

    private sealed record CaptureEntry(
        long Seq,
        DateTimeOffset TsUtc,
        long MonoTicks,
        string Source,
        string SessionId,
        string? Text = null,
        string? Package = null,
        string? Json = null);
}
