using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.Core.BuffTimers;

namespace MudClient.App.Tests;

public sealed class BuffHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "BuffHistoryTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveLoadAndClear_AreScopedToCharacter()
    {
        var store = new BuffHistoryStore(_directory);
        var gandalf = BuffCharacterKey.Create("killer-mud.pl", 4004, "Gandalf");
        var saruman = BuffCharacterKey.Create("killer-mud.pl", 4004, "Saruman");
        var document = new BuffHistoryDocument
        {
            Character = gandalf,
            Measurements = [new BuffMeasurement(
                Guid.NewGuid(), "armor", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow,
                60, 20, 10, 50, BuffMeasurementEndReason.NaturalExpiration, DateTimeOffset.UtcNow)],
        };

        store.Save(document);

        Assert.Single(store.Load(gandalf).Measurements);
        Assert.Empty(store.Load(saruman).Measurements);
        store.Clear(gandalf);
        Assert.Empty(store.Load(gandalf).Measurements);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
