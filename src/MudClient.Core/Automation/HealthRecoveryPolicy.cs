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
}
