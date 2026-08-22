using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

/// <summary>
/// Pure decisions for auto-farm's "cast these on every room entry, in this order" spell sequence
/// — distinct from <see cref="HealthRecoveryPolicy"/> (HP-triggered self-heal) and the plain
/// "keep memorized" required/opportunistic list: this one actually casts each spell (skipping
/// whichever are already an active buff), in the user's own defined order, mem'ing anything not
/// yet memorized first via the same room-arrival maintenance pass the heal/required-spell lists
/// already use.
/// </summary>
public static class AutoFarmCastSequencePolicy
{
    /// <summary>Sequence entries not memorized and not already being memorized — these need a
    /// fresh "mem" during the farm's maintenance pass (alongside the heal spell and required
    /// "keep memorized" list) before any of them can be cast.</summary>
    public static IReadOnlyList<string> GetSpellsNeedingMemorization(
        IReadOnlyList<string> castSpellNames, IReadOnlyList<MemorizedSpell> memorizedSpells) =>
        castSpellNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name =>
                !AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, name) &&
                !AutowalkRecoveryPolicy.IsMemorizingSpell(memorizedSpells, name))
            .ToArray();

    /// <summary>Sequence entries, in the user's own defined order, that are memorized but not
    /// currently an active buff — what to actually send "cast &lt;name&gt; self" for before the
    /// farm's next room hop. Skips anything <see cref="GetSpellsNeedingMemorization"/> would still
    /// flag, so a hop never fires a cast for a spell that isn't ready yet — that spell simply
    /// stays skipped this pass and gets picked up once memorized, the next time the sequence
    /// runs. <paramref name="activeAffectNames"/> is expected already normalized/case-insensitive
    /// the same way the buffs panel's own live Char.Affects tracking is.</summary>
    public static IReadOnlyList<string> GetSpellsNeedingCast(
        IReadOnlyList<string> castSpellNames,
        IReadOnlySet<string> activeAffectNames,
        IReadOnlyList<MemorizedSpell> memorizedSpells) =>
        castSpellNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, name))
            .Where(name => !activeAffectNames.Contains(name.Trim()))
            .ToArray();
}
