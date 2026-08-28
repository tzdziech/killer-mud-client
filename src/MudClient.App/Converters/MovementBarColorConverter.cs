using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MudClient.App.Converters;

public sealed class MovementBarColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        int mvScale;
        
        if (value is int i)
        {
            mvScale = i;
        }
        else if (value is int?)
        {
            mvScale = ((int?)value).Value;
        }
        else
        {
            return new SolidColorBrush(Colors.Teal);
        }

        if (mvScale <= 1)
        {
            return new SolidColorBrush(Colors.Blue);      // Blue - critical
        }
        else if (mvScale <= 2)
        {
            return new SolidColorBrush(Colors.Teal);    // Teal - medium
        }
        else
        {
            return new SolidColorBrush(Colors.Green);       // Green - full
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        throw new NotSupportedException();
    }
}
