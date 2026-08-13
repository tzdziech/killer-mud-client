using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

public enum HealthRecoveryAction
{
    /// <summary>Cast the configured heal spell on self — it's already memorized.</summary>
    CastHeal,

    /// <summary>Memorize the configured heal spell — not memorized yet, and not already
    /// in progress.</summary>
    MemorizeHeal,

    /// <summary>No heal spell configured, or one is already being memorized — just rest.</summary>
    Rest,
}

/// <summary>
/// Pure HP-threshold recovery decisions for auto-farm — the HP analogue of
/// <see cref="AutowalkRecoveryPolicy.GetLowMovementAction"/>, but for a self-cast healing spell
/// instead of "refresh". Unlike movement recovery this always resolves to *some* action; callers
/// only consult <see cref="GetRecoveryAction"/> after <see cref="IsBelowThreshold"/> is already true.
/// </summary>
public static class HealthRecoveryPolicy
{
    public static bool IsBelowThreshold(int? hp, int? maxHp, int thresholdPercent)
    {
        if (hp is null || maxHp is null || maxHp <= 0)
        {
            return false;
        }

        return (long)hp.Value * 100 <= (long)maxHp.Value * thresholdPercent;
    }

    /// <summary>Blank <paramref name="healSpellName"/> means no self-heal spell is configured —
    /// resting is the only option. Otherwise: cast if memorized, memorize if not (and not already
    /// being memorized), or rest while a memorize is already in flight.</summary>
    public static HealthRecoveryAction GetRecoveryAction(
        string healSpellName, IReadOnlyList<MemorizedSpell> memorizedSpells)
    {
        if (string.IsNullOrWhiteSpace(healSpellName))
        {
            return HealthRecoveryAction.Rest;
        }

        if (AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, healSpellName))
        {
            return HealthRecoveryAction.CastHeal;
        }

        return AutowalkRecoveryPolicy.IsMemorizingSpell(memorizedSpells, healSpellName)
            ? HealthRecoveryAction.Rest
            : HealthRecoveryAction.MemorizeHeal;
    }

    /// <summary>Which of <paramref name="requiredSpellNames"/> need a fresh "mem" — present in
    /// the list, but neither memorized nor already being memorized. Blank entries are ignored.
    /// Used for a "always keep these spells memorized" loadout, alongside (not instead of) the
    /// single heal spell handled by <see cref="GetRecoveryAction"/>.</summary>
    public static IReadOnlyList<string> GetSpellsNeedingMemorization(
        IReadOnlyList<string> requiredSpellNames, IReadOnlyList<MemorizedSpell> memorizedSpells) =>
        requiredSpellNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name =>
                !AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, name) &&
                !AutowalkRecoveryPolicy.IsMemorizingSpell(memorizedSpells, name))
            .ToArray();

    /// <summary>Whether an auto-farm heal cast should fire right now, mid-combat, in reaction to
    /// a Char.Vitals GMCP update rather than waiting for the next room arrival. Only ever resolves
    /// to <c>true</c> for an already-memorized spell (see <see cref="GetRecoveryAction"/>) — while
    /// fighting there's no point requesting <see cref="HealthRecoveryAction.MemorizeHeal"/> or
    /// <see cref="HealthRecoveryAction.Rest"/>, those stay the room-arrival flow's job. Safe to
    /// call on every single vitals tick without spamming duplicate casts: <paramref
    /// name="skillTimeouts"/> (Char.Skills.Timeout) reports the spell still on cooldown for every
    /// tick between the first cast and the server clearing it, so this stays false throughout.</summary>
    public static bool ShouldCastCombatHeal(
        bool autoFarmActive,
        int? hp,
        int? maxHp,
        int thresholdPercent,
        string healSpellName,
        IReadOnlyList<MemorizedSpell> memorizedSpells,
        IReadOnlyDictionary<string, bool> skillTimeouts)
    {
        if (!autoFarmActive || !IsBelowThreshold(hp, maxHp, thresholdPercent))
        {
            return false;
        }

        if (GetRecoveryAction(healSpellName, memorizedSpells) != HealthRecoveryAction.CastHeal)
        {
            return false;
        }

        return !(skillTimeouts.TryGetValue(healSpellName, out var onCooldown) && onCooldown);
    }
}
