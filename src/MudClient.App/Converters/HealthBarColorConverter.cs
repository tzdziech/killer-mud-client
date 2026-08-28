using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MudClient.App.Converters;

public sealed class HealthBarColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        int hpScale;
        
        if (value is int i)
        {
            hpScale = i;
        }
        else if (value is int?)
        {
            hpScale = ((int?)value).Value;
        }
        else
        {
            return new SolidColorBrush(Colors.Red);
        }

        if (hpScale <= 2)
        {
            return new SolidColorBrush(Color.Parse("#FFD700"));      // Żółty
        }
        else if (hpScale <= 4)
        {
            return new SolidColorBrush(Color.Parse("#FF7700"));    // Pomarańczowy
        }
        else
        {
            return new SolidColorBrush(Colors.Red);       // Czerwony
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
    {
        throw new NotSupportedException();
    }
}
