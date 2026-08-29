using System.Globalization;
using Avalonia.Data.Converters;

namespace MudClient.App.Converters;

/// <summary>Converts a memorization count (int) to a visual dot representation.
/// For example: 0 → "", 1 → "●", 2 → "●●", 3 → "●●●"
/// Used in GroupPanelView to display spell memorization status as dots instead of numbers.</summary>
public sealed class MemoCountDotsConverter : IValueConverter
{
    public static readonly MemoCountDotsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int memoCount || memoCount <= 0)
        {
            return string.Empty;
        }

        return string.Concat(Enumerable.Repeat("●", memoCount));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
