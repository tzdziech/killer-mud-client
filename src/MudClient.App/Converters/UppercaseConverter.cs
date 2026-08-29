using System.Globalization;
using Avalonia.Data.Converters;

namespace MudClient.App.Converters;

/// <summary>Converts text to uppercase. Used for spell labels in Group panel buttons.</summary>
public sealed class UppercaseConverter : IValueConverter
{
    public static readonly UppercaseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
        {
            return value;
        }

        return text.ToUpperInvariant();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
