namespace MudClient.App.Models;

/// <summary>Read-only skill row rendered in Character Status.</summary>
public sealed record KnownSkillEntry(
    string Name,
    int LearnableFromTeachers,
    int Current,
    int ItemBonus,
    string HistoryToolTip,
    string ItemBonusToolTip,
    bool IsMindLimited,
    int? MindLimit)
{
    public int EffectiveLevel => Current + ItemBonus;
    public int PotentialTrainingLevel => EffectiveLevel + LearnableFromTeachers;
    public int TrainingBarMaximum => Math.Max(100, PotentialTrainingLevel);
    public bool HasTeacherReserve => LearnableFromTeachers > 0;
    public bool HasItemBonus => ItemBonus != 0;
    public string CurrentDisplay => Current.ToString();
    public string EffectiveDisplay => EffectiveLevel.ToString();
    public string TeacherReserveDisplay => $"+{LearnableFromTeachers} do wyuczenia → {PotentialTrainingLevel}";
    public string ItemBonusDisplay => $"+{ItemBonus}";
    public string MindLimitDisplay => MindLimit is { } limit ? $"MAX {limit}" : "MAX";
    public string MindLimitToolTip => MindLimit is { } limit
        ? $"Osiągnięto limit nauki zależny od możliwości umysłowych postaci: {limit}. Serwer oznaczył ten skill znakiem #."
        : "Osiągnięto limit nauki zależny od możliwości umysłowych postaci. Serwer oznaczył ten skill znakiem #.";
    public string TrainingProgressToolTip =>
        $"Wyuczenie: {Current}. Premia z przedmiotów: {ItemBonus:+#;-#;0} (efektywnie {EffectiveLevel}). Wykupione u nauczycieli: +{LearnableFromTeachers} (łącznie {PotentialTrainingLevel}).";
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
