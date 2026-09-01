using MudClient.Core.BuffTimers;

namespace MudClient.App.Models;

public sealed class BuffHistoryDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public BuffCharacterKey? Character { get; set; }

    public List<BuffMeasurement> Measurements { get; set; } = [];

    public List<ActiveBuffCheckpoint> ActiveCheckpoints { get; set; } = [];
}
