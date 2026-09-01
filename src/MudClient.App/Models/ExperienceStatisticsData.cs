using MudClient.Core.Statistics;

namespace MudClient.App.Models;

public sealed class ExperienceStatisticsData
{
    public List<ExperienceSessionData> Sessions { get; set; } = [];
}

public sealed class ExperienceSessionData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
    public List<ExperienceChangeData> Changes { get; set; } = [];
    public List<CombatEncounterData> CombatEncounters { get; set; } = [];

    // Kept only to migrate statistics files written before combat hits were compacted.
    public List<CombatDamageData> CombatDamage { get; set; } = [];
}

public sealed class CombatEncounterData
{
    public string? EnemyName { get; set; }
    public DateTimeOffset When { get; set; }
    public long OwnDamage { get; set; }
    public long GroupDamage { get; set; }
    public int StrongestHit { get; set; }
    public string? StrongestHitAttackerName { get; set; }
    public DateTimeOffset StrongestHitWhen { get; set; }
}

public sealed class CombatDamageData
{
    public int Amount { get; set; }
    public string? EnemyName { get; set; }
    public string? AttackerName { get; set; }
    public bool IsOwnDamage { get; set; }
    public DateTimeOffset When { get; set; }
    public DateTimeOffset? EncounterWhen { get; set; }
}

public sealed class ExperienceChangeData
{
    public ExperienceChangeKind Kind { get; set; }
    public long Amount { get; set; }
    public string? EnemyName { get; set; }
    public int Level { get; set; }
    public long? RemainingToLevel { get; set; }
    public DateTimeOffset When { get; set; }
}
