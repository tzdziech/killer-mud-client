namespace MudClient.App.Models;

/// <summary>Read-only skill row rendered in Character Status.</summary>
public sealed record KnownSkillEntry(
    string Name,
    int LearnableFromTeachers,
    int Current,
    int ItemBonus,
    string HistoryToolTip)
{
    public string ValueDisplay => $"{LearnableFromTeachers} / {Current} + {ItemBonus}";
}

/// <summary>A level-grouped view of known skills.</summary>
public sealed record KnownSkillLevel(int? Level, IReadOnlyList<KnownSkillEntry> Skills)
{
    public string Header => Level is { } level ? $"Poziom {level}" : "Nowo poznane";
}

/// <summary>Read-only spell row rendered in Character Status.</summary>
public sealed record KnownSpellEntry(string Name, int CastingLevel)
{
    public string ValueDisplay => $"({CastingLevel})";
}

/// <summary>A circle-grouped view of known spells.</summary>
public sealed record KnownSpellCircle(int Circle, IReadOnlyList<KnownSpellEntry> Spells)
{
    public string Header => $"Krąg {Circle}";
}

/// <summary>A spell shown by <c>spells all</c> with an empty casting-level field.</summary>
public sealed record MissingSpellEntry(string Name);

/// <summary>Missing spells grouped by the circle printed in the full spell list.</summary>
public sealed record MissingSpellCircle(int? Circle, IReadOnlyList<MissingSpellEntry> Spells)
{
    public string Header => Circle is { } circle ? $"Krąg {circle}" : "Krąg nieznany";
}
