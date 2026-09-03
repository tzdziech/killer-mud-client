using System.Text.Json;
using MudClient.Core.Gmcp;

namespace MudClient.App.Services;

/// <summary>Owns automatic/manual capture lifecycle for one MUD connection at a time.</summary>
public sealed class CombatSessionCaptureCoordinator
{
    private readonly string _directory;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private TelnetLineCapture? _capture;
    private bool _automaticStartObserved;

    public CombatSessionCaptureCoordinator(string directory, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string? ActivePath
    {
        get { lock (_sync) return _capture?.Path; }
    }

    public string StartManual()
    {
        lock (_sync)
        {
            _capture ??= new TelnetLineCapture(_directory, _timeProvider);
            return _capture.Path;
        }
    }

    public void RecordTelnet(string line)
    {
        lock (_sync) _capture?.TryRecord(line);
    }

    /// <summary>Starts on the first trustworthy login signal and records that signal as entry one.</summary>
    public void ObserveGmcp(GmcpMessage message)
    {
        lock (_sync)
        {
            if (_capture is null && !_automaticStartObserved && IsNamedCharacterVitals(message))
            {
                _automaticStartObserved = true;
                _capture = new TelnetLineCapture(_directory, _timeProvider);
            }

            _capture?.TryRecord(message);
        }
    }

    public async Task<string?> StopAsync(bool connectionClosed = false)
    {
        TelnetLineCapture? capture;
        lock (_sync)
        {
            capture = _capture;
            _capture = null;
            _automaticStartObserved = !connectionClosed;
        }

        if (capture is null)
        {
            return null;
        }

        await capture.DisposeAsync().ConfigureAwait(false);
        return capture.Path;
    }

    internal static bool IsNamedCharacterVitals(GmcpMessage message)
    {
        if (!string.Equals(message.Package, "Char.Vitals", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(message.Json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(message.Json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // Malformed GMCP is not a trustworthy login signal.
        }

        return false;
    }
}
