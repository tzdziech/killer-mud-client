using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MudClient.App.Converters;

/// <summary>Reddens a group spell-shortcut button (see GroupPanelView.axaml) when the caster
/// doesn't currently have that spell memorized and ready — a quick warning that clicking it will
/// fail rather than cast. Bound as [SpellName, MainWindowViewModel.MemorizedSpellNames]; returns
/// <see cref="AvaloniaProperty.UnsetValue"/> when memorized (or on missing/malformed input) so the
/// button's own style (hover, pressed, etc.) keeps controlling its background instead of being
/// pinned to a fixed "normal" brush.</summary>
public sealed class SpellMemorizedBrushConverter : IMultiValueConverter
{
    public static readonly SpellMemorizedBrushConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 ||
            values[0] is not string spellName ||
            values[1] is not IEnumerable<string> memorizedNames)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (memorizedNames.Contains(spellName, StringComparer.OrdinalIgnoreCase))
        {
            return AvaloniaProperty.UnsetValue;
        }

        return Application.Current?.TryFindResource("MudBrushCrimson", out var brush) == true
            ? brush
            : Brushes.Crimson;
    }
}
