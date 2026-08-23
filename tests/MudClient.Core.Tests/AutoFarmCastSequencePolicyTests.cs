using MudClient.Core.Automation;
using MudClient.Core.Gmcp;

namespace MudClient.Core.Tests;

public sealed class AutoFarmCastSequencePolicyTests
{
    private static readonly MemorizedSpell[] ArmorAndBlessMemorized =
    [
        new MemorizedSpell(1, 1, "armor", Memed: true, Meming: false),
        new MemorizedSpell(2, 1, "bless", Memed: true, Meming: false),
    ];

    private static readonly HashSet<string> NoActiveAffects = new(StringComparer.OrdinalIgnoreCase);

    private static AutoFarmCastSpell Buff(string name) => new(name, Offensive: false);

    private static AutoFarmCastSpell Offensive(string name) => new(name, Offensive: true);

    [Fact]
    public void GetSpellsNeedingMemorization_UnmemorizedEntry_IsReturned()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization(
            [Buff("armor"), Buff("haste")], ArmorAndBlessMemorized);

        Assert.Equal([Buff("haste")], result);
    }

    [Fact]
    public void GetSpellsNeedingMemorization_AlreadyBeingMemorized_IsNotReturned()
    {
        var memorizing = new[] { new MemorizedSpell(1, 1, "haste", Memed: false, Meming: true) };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization([Buff("haste")], memorizing);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpellsNeedingMemorization_EverythingMemorized_ReturnsEmpty()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization(
            [Buff("armor"), Buff("bless")], ArmorAndBlessMemorized);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpellsNeedingCast_SkipsAlreadyActiveBuff()
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "armor" };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Buff("armor"), Buff("bless")], active, ArmorAndBlessMemorized);

        Assert.Equal([Buff("bless")], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_PreservesTheConfiguredOrder()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Buff("bless"), Buff("armor")], NoActiveAffects, ArmorAndBlessMemorized);

        Assert.Equal([Buff("bless"), Buff("armor")], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_NotYetMemorized_IsSkipped()
    {
        // GetSpellsNeedingMemorization's job first — combat starting must never try to cast
        // something that isn't ready.
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Buff("armor"), Buff("haste")], NoActiveAffects, ArmorAndBlessMemorized);

        Assert.Equal([Buff("armor")], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_EverythingAlreadyActive_ReturnsEmpty()
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "armor", "bless" };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Buff("armor"), Buff("bless")], active, ArmorAndBlessMemorized);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpellsNeedingCast_OffensiveEntry_IsNeverSkippedForBeingActive()
    {
        // An offensive spell has no "already active" state — even if its name somehow matched an
        // active affect, it must still fire every time, unlike a buff.
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "magic missile" };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Offensive("magic missile")], active, [new MemorizedSpell(1, 1, "magic missile", Memed: true, Meming: false)]);

        Assert.Equal([Offensive("magic missile")], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_MixOfBuffAndOffensive_BothReturnedInOrder()
    {
        var spells = new[]
        {
            new MemorizedSpell(1, 1, "armor", Memed: true, Meming: false),
            new MemorizedSpell(2, 1, "magic missile", Memed: true, Meming: false),
        };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            [Buff("armor"), Offensive("magic missile")], NoActiveAffects, spells);

        Assert.Equal([Buff("armor"), Offensive("magic missile")], result);
    }
}
