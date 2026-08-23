using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

/// <summary>
/// Pure decisions for auto-farm's "cast these the moment combat starts, in this order" spell
/// sequence — distinct from <see cref="HealthRecoveryPolicy"/> (HP-triggered self-heal) and the
/// plain "keep memorized" required/opportunistic list: this one actually casts each spell (a buff
/// skips itself once already an active buff; an offensive spell always fires), in the user's own
/// defined order, mem'ing anything not yet memorized first via the same room-arrival maintenance
/// pass the heal/required-spell lists already use.
/// </summary>
public static class AutoFarmCastSequencePolicy
{
    /// <summary>Sequence entries not memorized and not already being memorized — these need a
    /// fresh "mem" during the farm's maintenance pass (alongside the heal spell and required
    /// "keep memorized" list) before any of them can be cast.</summary>
    public static IReadOnlyList<AutoFarmCastSpell> GetSpellsNeedingMemorization(
        IReadOnlyList<AutoFarmCastSpell> castSpells, IReadOnlyList<MemorizedSpell> memorizedSpells) =>
        castSpells
            .Where(spell => !string.IsNullOrWhiteSpace(spell.Name))
            .Where(spell =>
                !AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, spell.Name) &&
                !AutowalkRecoveryPolicy.IsMemorizingSpell(memorizedSpells, spell.Name))
            .ToArray();

    /// <summary>Sequence entries, in the user's own defined order, that are memorized and ready to
    /// cast right now: a buff entry only once it isn't already an active buff, an offensive entry
    /// unconditionally (there's nothing to check an attack spell against). Skips anything
    /// <see cref="GetSpellsNeedingMemorization"/> would still flag, so combat starting never fires
    /// a cast for a spell that isn't ready yet — that spell simply stays skipped this pass and
    /// gets picked up once memorized, the next time combat starts. <paramref name="activeAffectNames"/>
    /// is expected already normalized/case-insensitive the same way the buffs panel's own live
    /// Char.Affects tracking is.</summary>
    public static IReadOnlyList<AutoFarmCastSpell> GetSpellsNeedingCast(
        IReadOnlyList<AutoFarmCastSpell> castSpells,
        IReadOnlySet<string> activeAffectNames,
        IReadOnlyList<MemorizedSpell> memorizedSpells) =>
        castSpells
            .Where(spell => !string.IsNullOrWhiteSpace(spell.Name))
            .Where(spell => AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, spell.Name))
            .Where(spell => spell.Offensive || !activeAffectNames.Contains(spell.Name.Trim()))
            .ToArray();
}
