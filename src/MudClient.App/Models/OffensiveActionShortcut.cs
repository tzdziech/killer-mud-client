using CommunityToolkit.Mvvm.ComponentModel;

namespace MudClient.App.Models;

/// <summary>A user-defined offensive spell/skill shortcut shown as a button in the "Akcje
/// offensywne i definiowalne" panel's Offensywne section — e.g. label "fb" for spell name
/// "fireball". Cast on no particular target (unlike <see cref="GroupSpellShortcut"/>), so the
/// button always sends either "cast &quot;SpellName&quot;" or the matched skill's own syntax; see
/// MainWindowViewModel.CastOffensiveAction.</summary>
public sealed partial class OffensiveActionShortcut : ObservableObject
{
    public string Label { get; set; } = string.Empty;

    public string SpellName { get; set; } = string.Empty;

    /// <summary>Number of memorized copies of this spell (updated in real-time). Only shown for spells, not skills.</summary>
    [ObservableProperty]
    private int memoCount;

    /// <summary>True if SpellName resolves to a skill (not a spell) via Killeropedia database.</summary>
    [ObservableProperty]
    private bool isSkill;

    /// <summary>True while this skill is on cooldown per the latest GMCP Char.Skills.Timeout
    /// snapshot (see MainWindowViewModel.SkillsOnCooldown/UpdateOffensiveActionCooldownStatus).
    /// Always false for spells — Char.Skills.Timeout only ever reports skills.</summary>
    [ObservableProperty]
    private bool isOnCooldown;
}
