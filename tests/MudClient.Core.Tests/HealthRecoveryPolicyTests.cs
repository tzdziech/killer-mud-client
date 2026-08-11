using MudClient.Core.Automation;
using MudClient.Core.Gmcp;

namespace MudClient.Core.Tests;

public sealed class HealthRecoveryPolicyTests
{
    [Fact]
    public void IsBelowThreshold_AtThreshold_ReturnsTrue()
    {
        Assert.True(HealthRecoveryPolicy.IsBelowThreshold(30, 100, 30));
    }

    [Fact]
    public void IsBelowThreshold_AboveThreshold_ReturnsFalse()
    {
        Assert.False(HealthRecoveryPolicy.IsBelowThreshold(31, 100, 30));
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(30, null)]
    [InlineData(30, 0)]
    public void IsBelowThreshold_MissingOrInvalidData_ReturnsFalse(int? hp, int? maxHp)
    {
        Assert.False(HealthRecoveryPolicy.IsBelowThreshold(hp, maxHp, 30));
    }

    [Fact]
    public void GetRecoveryAction_BlankHealSpellName_AlwaysRests()
    {
        var spells = new[] { new MemorizedSpell(1, 1, "heal", Memed: true, Meming: false) };

        Assert.Equal(HealthRecoveryAction.Rest, HealthRecoveryPolicy.GetRecoveryAction("", spells));
        Assert.Equal(HealthRecoveryAction.Rest, HealthRecoveryPolicy.GetRecoveryAction("   ", spells));
    }

    [Fact]
    public void GetRecoveryAction_HealSpellMemorized_CastsIt()
    {
        var spells = new[] { new MemorizedSpell(1, 1, "heal", Memed: true, Meming: false) };

        Assert.Equal(HealthRecoveryAction.CastHeal, HealthRecoveryPolicy.GetRecoveryAction("heal", spells));
    }

    [Fact]
    public void GetRecoveryAction_HealSpellNotMemorizedOrMeming_MemorizesIt()
    {
        Assert.Equal(HealthRecoveryAction.MemorizeHeal, HealthRecoveryPolicy.GetRecoveryAction("heal", []));
    }

    [Fact]
    public void GetRecoveryAction_HealSpellAlreadyBeingMemorized_JustRests()
    {
        var spells = new[] { new MemorizedSpell(1, 1, "heal", Memed: false, Meming: true) };

        Assert.Equal(HealthRecoveryAction.Rest, HealthRecoveryPolicy.GetRecoveryAction("heal", spells));
    }

    [Fact]
    public void GetRecoveryAction_IsCaseInsensitiveOnSpellName()
    {
        var spells = new[] { new MemorizedSpell(1, 1, "Heal", Memed: true, Meming: false) };

        Assert.Equal(HealthRecoveryAction.CastHeal, HealthRecoveryPolicy.GetRecoveryAction("heal", spells));
    }
}
