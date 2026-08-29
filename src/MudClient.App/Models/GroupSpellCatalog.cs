using CommunityToolkit.Mvvm.ComponentModel;

namespace MudClient.App.Models;

/// <summary>A user-defined shortcut for casting a spell/skill on a specific group member — e.g.
/// label "cc" for spell name "cure critical" — shown as a small button next to each member in
/// the Group panel. Persisted by <see cref="Services.GroupSpellStore"/>.</summary>
public sealed partial class GroupSpellShortcut : ObservableObject
{
    public string Label { get; set; } = string.Empty;

    public string SpellName { get; set; } = string.Empty;

    /// <summary>Number of memorized copies of this spell (updated in real-time). Only shown for spells, not skills.</summary>
    [ObservableProperty]
    private int memoCount;

    /// <summary>True if SpellName resolves to a skill (not a spell) via Killeropedia database.</summary>
    [ObservableProperty]
    private bool isSkill;
}

public sealed class GroupSpellDocument
{
    public List<GroupSpellShortcut> Entries { get; set; } = [];
}
