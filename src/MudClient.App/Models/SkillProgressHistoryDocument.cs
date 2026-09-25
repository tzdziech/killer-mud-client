using MudClient.Core.BuffTimers;

namespace MudClient.App.Models;

/// <summary>Durable, server-scoped history of confirmed natural skill improvements.</summary>
public sealed class SkillProgressHistoryDocument
{
    public BuffCharacterKey? Character { get; set; }

    public List<SkillProgressHistoryEvent> Events { get; set; } = [];
}

/// <summary>One observed "Stajesz się lepszy..." message and its live GMCP context.</summary>
public sealed class SkillProgressHistoryEvent
{
    public string SkillName { get; set; } = string.Empty;

    public DateTimeOffset When { get; set; }

    public int Current { get; set; }

    public int LearnableFromTeachers { get; set; }

    public int ItemBonus { get; set; }

    public bool IsInCombat { get; set; }

    /// <summary>Only a direct, current Room.People opponent is stored; otherwise null.</summary>
    public string? EnemyName { get; set; }

    public string? RoomVnum { get; set; }
}
