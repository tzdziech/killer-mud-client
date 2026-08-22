using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using MudClient.App.Controls;
using MudClient.App.Models;

namespace MudClient.App.Converters;

/// <summary>Builds the same rich "help" tooltip <see cref="AbilitySkillTreeCanvas"/> shows on
/// hover (name, levels, syntax, description, ...) for an <see cref="AbilitySkillTreeEntry"/> row
/// outside the canvas itself — used by the "Sprawdź co zyskasz" list in KilleropediaSkillsView, so
/// the two hover experiences never drift apart.</summary>
public sealed class AbilityTooltipConverter : IValueConverter
{
    public static readonly AbilityTooltipConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is AbilitySkillTreeEntry ability ? AbilitySkillTreeCanvas.BuildTooltip(ability) : AvaloniaProperty.UnsetValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
