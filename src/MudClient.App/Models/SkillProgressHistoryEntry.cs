namespace MudClient.App.Models;

/// <summary>Display projection of one persisted skill-improvement event.</summary>
public sealed record SkillProgressHistoryEntry(
    string SkillName,
    DateTimeOffset When,
    int Current,
    int LearnableFromTeachers,
    int ItemBonus,
    bool IsInCombat,
    string? EnemyName,
    string? RoomVnum)
{
    public string WhenDisplay => When.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

    public string ValueDisplay => $"{LearnableFromTeachers} / {Current} + {ItemBonus}";

    public string ContextDisplay => IsInCombat
        ? $"Walka: {EnemyName ?? "przeciwnik nieustalony"}"
        : "Poza walką";

    public string ToolTip => $"{WhenDisplay}\n{ContextDisplay}{(RoomVnum is { Length: > 0 } vnum ? $"\nPokój: {vnum}" : string.Empty)}\nStan po poprawie: {ValueDisplay}";
}
