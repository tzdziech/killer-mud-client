using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.Core.BuffTimers;

namespace MudClient.App.Tests;

public sealed class SkillProgressHistoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "SkillProgressHistoryTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveLoad_IsScopedToCharacterAndRetainsCombatContext()
    {
        var store = new SkillProgressHistoryStore(_directory);
        var ranger = BuffCharacterKey.Create("killer-mud.pl", 4004, "Ranger");
        var mage = BuffCharacterKey.Create("killer-mud.pl", 4004, "Mage");
        var when = new DateTimeOffset(2026, 9, 23, 20, 15, 0, TimeSpan.Zero);
        store.Save(new SkillProgressHistoryDocument
        {
            Character = ranger,
            Events =
            [
                new SkillProgressHistoryEvent
                {
                    SkillName = "staff",
                    When = when,
                    Current = 74,
                    LearnableFromTeachers = 2,
                    ItemBonus = 0,
                    IsInCombat = true,
                    EnemyName = "ork",
                    RoomVnum = "1234",
                },
            ],
        });

        var entry = Assert.Single(store.Load(ranger).Events);

        Assert.Equal("staff", entry.SkillName);
        Assert.Equal(when, entry.When);
        Assert.Equal("ork", entry.EnemyName);
        Assert.True(entry.IsInCombat);
        Assert.Equal("1234", entry.RoomVnum);
        Assert.Empty(store.Load(mage).Events);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
