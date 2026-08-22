using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

public enum HealthRecoveryAction
{
    /// <summary>Cast a heal spell on self — it's already memorized.</summary>
    CastHeal,

    /// <summary>Memorize a heal spell — not memorized yet, and not already in progress.</summary>
    MemorizeHeal,

    /// <summary>No heal spell configured, or one is already being memorized — just rest.</summary>
    Rest,
}

/// <summary>The action <see cref="HealthRecoveryPolicy.GetRecoveryAction"/> resolved to, plus
/// which spell from the priority list it applies to. <c>SpellName</c> is set for
/// <see cref="HealthRecoveryAction.Rest"/> too when a candidate is already being memorized (so a
/// caller that cares can see what it's waiting on), and null when nothing in the list applies at
/// all (blank/empty list, or every candidate already being memorized... see xmldoc).</summary>
public sealed record HealthRecoveryDecision(HealthRecoveryAction Action, string? SpellName);

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

    /// <summary>Picks a heal spell from <paramref name="healSpellNames"/> (ordered strongest to
    /// weakest) and what to do with it: cast the strongest one that's already memorized, or —
    /// if none are — memorize the strongest one not already being memorized, or rest while one
    /// from the list is already in flight. An empty/blank-only list means no self-heal is
    /// configured — resting is the only option.</summary>
    public static HealthRecoveryDecision GetRecoveryAction(
        IReadOnlyList<string> healSpellNames, IReadOnlyList<MemorizedSpell> memorizedSpells)
    {
        var candidates = healSpellNames.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
        if (candidates.Count == 0)
        {
            return new HealthRecoveryDecision(HealthRecoveryAction.Rest, null);
        }

        foreach (var name in candidates)
        {
            if (AutowalkRecoveryPolicy.HasMemorizedSpell(memorizedSpells, name))
            {
                return new HealthRecoveryDecision(HealthRecoveryAction.CastHeal, name);
            }
        }

        foreach (var name in candidates)
        {
            if (AutowalkRecoveryPolicy.IsMemorizingSpell(memorizedSpells, name))
            {
                return new HealthRecoveryDecision(HealthRecoveryAction.Rest, name);
            }
        }

        return new HealthRecoveryDecision(HealthRecoveryAction.MemorizeHeal, candidates[0]);
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
    /// a Char.Vitals GMCP update rather than waiting for the next room arrival, and which spell
    /// (from the priority list) to cast. Only ever resolves to a cast for an already-memorized
    /// spell (see <see cref="GetRecoveryAction"/>) — while fighting there's no point requesting
    /// <see cref="HealthRecoveryAction.MemorizeHeal"/> or <see cref="HealthRecoveryAction.Rest"/>,
    /// those stay the room-arrival flow's job. Safe to call on every single vitals tick without
    /// spamming duplicate casts: <paramref name="skillTimeouts"/> (Char.Skills.Timeout) reports
    /// the spell still on cooldown for every tick between the first cast and the server clearing
    /// it, so this stays false throughout.</summary>
    public static (bool ShouldCast, string? SpellName) ShouldCastCombatHeal(
        bool autoFarmActive,
        int? hp,
        int? maxHp,
        int thresholdPercent,
        IReadOnlyList<string> healSpellNames,
        IReadOnlyList<MemorizedSpell> memorizedSpells,
        IReadOnlyDictionary<string, bool> skillTimeouts)
    {
        if (!autoFarmActive || !IsBelowThreshold(hp, maxHp, thresholdPercent))
        {
            return (false, null);
        }

        var decision = GetRecoveryAction(healSpellNames, memorizedSpells);
        if (decision.Action != HealthRecoveryAction.CastHeal || decision.SpellName is not { } spellName)
        {
            return (false, null);
        }

        var onCooldown = skillTimeouts.TryGetValue(spellName, out var timeout) && timeout;
        return onCooldown ? (false, null) : (true, spellName);
    }
}
