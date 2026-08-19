namespace MudClient.App.Models;

/// <summary>A user-defined shortcut for casting a spell/skill on a specific group member — e.g.
/// label "cc" for spell name "cure critical" — shown as a small button next to each member in
/// the Group panel. Persisted by <see cref="Services.GroupSpellStore"/>.</summary>
public sealed class GroupSpellShortcut
{
    public string Label { get; set; } = string.Empty;

    public string SpellName { get; set; } = string.Empty;
}

public sealed class GroupSpellDocument
{
    public List<GroupSpellShortcut> Entries { get; set; } = [];
}
