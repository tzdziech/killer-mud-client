using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace MudClient.App.Converters;

/// <summary>
/// Picks one of two named brush resources based on a bool, e.g. for the "Buffy" panel buttons
/// (MemSpellsPanelView.axaml) whose name/background/bracket color depends on whether the spell is
/// currently listed in Char.MemSpell. ConverterParameter is "TrueKey|FalseKey"; returns
/// <see cref="AvaloniaProperty.UnsetValue"/> on malformed input or a missing resource key.
/// </summary>
public sealed class BoolToResourceBrushConverter : IValueConverter
{
    public static readonly BoolToResourceBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool flag || parameter is not string keys)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var parts = keys.Split('|');
        if (parts.Length != 2)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var key = flag ? parts[0] : parts[1];
        return Application.Current?.TryFindResource(key, out var brush) == true
            ? brush
            : AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
