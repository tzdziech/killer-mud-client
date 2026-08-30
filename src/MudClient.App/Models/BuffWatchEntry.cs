using CommunityToolkit.Mvvm.ComponentModel;

namespace MudClient.App.Models;

/// <summary>
/// A buff the user wants to keep active, matched by name against
/// Char.Affects GMCP entries. Stored per profile.
/// </summary>
public sealed partial class BuffWatchEntry : ObservableObject
{
    public BuffWatchEntry(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Affect name as typed by the user; also used as the spell name
    /// in the recast command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// True when the buff is present in the latest Char.Affects. Independent of
    /// <see cref="IsListedInMemSpell"/> — this alone drives the button's border/bracket-before
    /// color in MemSpellsPanelView.axaml, so the active/inactive signal stays visible even for a
    /// spell not currently in Char.MemSpell.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>Number of matching entries in the latest Char.MemSpell with Memed == true.</summary>
    private int _memoizedCount;
    public int MemoizedCount
    {
        get => _memoizedCount;
        set
        {
            if (SetProperty(ref _memoizedCount, value))
            {
                OnPropertyChanged(nameof(IsListedInMemSpell));
                OnPropertyChanged(nameof(BracketAfterText));
                OnPropertyChanged(nameof(IsReadyBracketGreen));
            }
        }
    }

    /// <summary>
    /// Number of matching entries in the latest Char.MemSpell with Memed == false (includes slots
    /// currently Meming — not yet usable).
    /// </summary>
    private int _usedCount;
    public int UsedCount
    {
        get => _usedCount;
        set
        {
            if (SetProperty(ref _usedCount, value))
            {
                OnPropertyChanged(nameof(IsListedInMemSpell));
                OnPropertyChanged(nameof(BracketAfterText));
            }
        }
    }

    /// <summary>
    /// True when this spell name appears at all in the latest Char.MemSpell list. Drives the
    /// button's name color, background, enabled state, and whether the bracket-after is shown —
    /// independent of <see cref="IsActive"/>.
    /// </summary>
    public bool IsListedInMemSpell => MemoizedCount > 0 || UsedCount > 0;

    /// <summary>E.g. "[2/1]" — memoized/used counts. Hidden entirely when not listed.</summary>
    public string BracketAfterText => $"[{MemoizedCount}/{UsedCount}]";

    /// <summary>True (green) when at least one matching spell is ready to cast.</summary>
    public bool IsReadyBracketGreen => MemoizedCount > 0;

    /// <summary>
    /// Normalizes an affect name for comparison: the server appends a
    /// parenthesized counter to some affects (e.g. "mirror image (7)"),
    /// which must be ignored when matching against the user's list.
    /// </summary>
    public static string NormalizeName(string name)
    {
        var open = name.IndexOf('(');
        if (open >= 0)
        {
            name = name[..open];
        }

        return name.Trim();
    }

    /// <summary>
    /// Like <see cref="NormalizeName"/>, but additionally strips a leading "mass " so group
    /// versions of a spell (e.g. "mass aid") match the plain Char.Affects entry the server sends
    /// for either version (e.g. "aid") — used only for matching against Char.Affects, never for
    /// Char.MemSpell, where "mass aid" and "aid" are genuinely different memorized spells that must
    /// stay counted separately.
    /// </summary>
    public static string NormalizeAffectName(string name)
    {
        var normalized = NormalizeName(name);
        return normalized.StartsWith("mass ", StringComparison.OrdinalIgnoreCase)
            ? normalized["mass ".Length..].TrimStart()
            : normalized;
    }
}
