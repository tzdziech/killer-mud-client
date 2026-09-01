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
