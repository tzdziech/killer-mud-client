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

    /// <summary>Number of memorized copies of this buff's spell.</summary>
    private int _memorizedCount;
    public int MemoizedCount
    {
        get => _memorizedCount;
        set => SetProperty(ref _memorizedCount, value);
    }

    /// <summary>Number of used copies of this buff's spell.</summary>
    private int _usedCount;
    public int UsedCount
    {
        get => _usedCount;
        set => SetProperty(ref _usedCount, value);
    }

    /// <summary>Display string for memorization status: "-" if not found, "N/M" if found.</summary>
    public string MemoStatus =>
        MemoizedCount == 0 && UsedCount == 0
            ? "-"
            : $"{MemoizedCount}/{UsedCount}";

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
