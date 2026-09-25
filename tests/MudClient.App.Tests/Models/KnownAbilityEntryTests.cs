using MudClient.App.Models;

namespace MudClient.App.Tests.Models;

public sealed class KnownAbilityEntryTests
{
    [Fact]
    public void KnownSkillEntry_DescribesCurrentTrainingReserveAndItemBonus()
    {
        var entry = new KnownSkillEntry("axe", 10, 8, 2, string.Empty, string.Empty, false, null);

        Assert.Equal(10, entry.EffectiveLevel);
        Assert.Equal(20, entry.PotentialTrainingLevel);
        Assert.Equal(100, entry.TrainingBarMaximum);
        Assert.Equal("8", entry.CurrentDisplay);
        Assert.Equal("10", entry.EffectiveDisplay);
        Assert.Equal("+10 do wyuczenia → 20", entry.TeacherReserveDisplay);
        Assert.Equal("+2", entry.ItemBonusDisplay);
    }

    [Fact]
    public void KnownSkillEntry_ExpandsBarPastOneHundredWithoutWhiteRemainder()
    {
        var entry = new KnownSkillEntry("axe", 0, 87, 15, string.Empty, string.Empty, false, null);

        Assert.Equal(102, entry.EffectiveLevel);
        Assert.Equal(102, entry.TrainingBarMaximum);
    }

    [Fact]
    public void KnownSkillEntry_MindLimit_DescribesTheServerReportedLimit()
    {
        var entry = new KnownSkillEntry("twohanded weapon", 0, 87, 5, string.Empty, string.Empty, true, 86);

        Assert.Equal("MAX 86", entry.MindLimitDisplay);
        Assert.Contains("86", entry.MindLimitToolTip);
    }
}
