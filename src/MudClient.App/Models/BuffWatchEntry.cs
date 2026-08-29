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

    /// <summary>True when the buff is present in the latest Char.Affects.</summary>
    [ObservableProperty]
    private bool _isActive;

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(IsKnownActive));
        OnPropertyChanged(nameof(IsKnownInactive));
        OnPropertyChanged(nameof(IsUnknown));
    }

    /// <summary>Number of memorized copies of this buff's spell.</summary>
    private int _memorizedCount;
    public int MemoizedCount
    {
        get => _memorizedCount;
        set
        {
            if (SetProperty(ref _memorizedCount, value))
            {
                OnPropertyChanged(nameof(MemoStatus));
                OnPropertyChanged(nameof(IsKnown));
                OnPropertyChanged(nameof(IsKnownActive));
                OnPropertyChanged(nameof(IsKnownInactive));
                OnPropertyChanged(nameof(IsUnknown));
            }
        }
    }

    /// <summary>Number of used copies of this buff's spell.</summary>
    private int _usedCount;
    public int UsedCount
    {
        get => _usedCount;
        set
        {
            if (SetProperty(ref _usedCount, value))
            {
                OnPropertyChanged(nameof(MemoStatus));
                OnPropertyChanged(nameof(IsKnown));
                OnPropertyChanged(nameof(IsKnownActive));
                OnPropertyChanged(nameof(IsKnownInactive));
                OnPropertyChanged(nameof(IsUnknown));
            }
        }
    }

    /// <summary>Display string for memorization status: "-" if not found, "N/M" if found.</summary>
    public string MemoStatus =>
        MemoizedCount == 0 && UsedCount == 0
            ? "-"
            : $"{MemoizedCount}/{UsedCount}";

    /// <summary>True if this buff is known (has any memorized or used copies).</summary>
    public bool IsKnown => MemoizedCount > 0 || UsedCount > 0;

    /// <summary>True if this is a known active buff (should show stats with green colors).</summary>
    public bool IsKnownActive => IsKnown && IsActive;

    /// <summary>True if this is a known inactive buff (should show stats with red colors).</summary>
    public bool IsKnownInactive => IsKnown && !IsActive;

    /// <summary>True if this is an unknown buff (should show gray [-] regardless of active state).</summary>
    public bool IsUnknown => !IsKnown;

    /// <summary>Icon for unknown buff status: [+] if active, [!] if inactive (but always gray).</summary>
    public string UnknownStatusIcon => IsActive ? "[+]" : "[!]";

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
}
