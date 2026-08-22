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

    [Fact]
    public void GetSpellsNeedingMemorization_UnmemorizedEntry_IsReturned()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization(
            ["armor", "haste"], ArmorAndBlessMemorized);

        Assert.Equal(["haste"], result);
    }

    [Fact]
    public void GetSpellsNeedingMemorization_AlreadyBeingMemorized_IsNotReturned()
    {
        var memorizing = new[] { new MemorizedSpell(1, 1, "haste", Memed: false, Meming: true) };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization(["haste"], memorizing);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpellsNeedingMemorization_EverythingMemorized_ReturnsEmpty()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingMemorization(
            ["armor", "bless"], ArmorAndBlessMemorized);

        Assert.Empty(result);
    }

    [Fact]
    public void GetSpellsNeedingCast_SkipsAlreadyActiveBuff()
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "armor" };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            ["armor", "bless"], active, ArmorAndBlessMemorized);

        Assert.Equal(["bless"], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_PreservesTheConfiguredOrder()
    {
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            ["bless", "armor"], NoActiveAffects, ArmorAndBlessMemorized);

        Assert.Equal(["bless", "armor"], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_NotYetMemorized_IsSkipped()
    {
        // GetSpellsNeedingMemorization's job first — a room hop must never try to cast something
        // that isn't ready.
        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            ["armor", "haste"], NoActiveAffects, ArmorAndBlessMemorized);

        Assert.Equal(["armor"], result);
    }

    [Fact]
    public void GetSpellsNeedingCast_EverythingAlreadyActive_ReturnsEmpty()
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "armor", "bless" };

        var result = AutoFarmCastSequencePolicy.GetSpellsNeedingCast(
            ["armor", "bless"], active, ArmorAndBlessMemorized);

        Assert.Empty(result);
    }
}
