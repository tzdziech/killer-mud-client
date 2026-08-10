using Avalonia.Headless.XUnit;
using MudClient.App.Controls;
using MudClient.App.ViewModels;
using Xunit;

namespace MudClient.App.Tests;

[Collection(AvaloniaUiCollection.Name)]
public sealed class SearchTeacherDialogTests
{
    [AvaloniaFact]
    public void ItemFilter_MatchesTeacherName()
    {
        var dialog = new SearchTeacherDialog([new TeacherSearchEntry("Mistrz Moran", "Mistrz Moran")]);

        Assert.NotNull(dialog.NameBox.ItemFilter);
        Assert.True(dialog.NameBox.ItemFilter!(
            "moran", new TeacherSearchEntry("Mistrz Moran", "Mistrz Moran")));
    }

    [AvaloniaFact]
    public void ItemFilter_MatchesSkillNameEvenWhenTeacherNameDiffers()
    {
        var dialog = new SearchTeacherDialog([]);
        var entry = new TeacherSearchEntry("Mistrz Moran", "Mistrz Moran | dragon strike 65–95 | vertical kick");

        Assert.True(dialog.NameBox.ItemFilter!("dragon strike", entry));
        Assert.True(dialog.NameBox.ItemFilter!("vertical kick", entry));
    }

    [AvaloniaFact]
    public void ItemFilter_NoMatch_ReturnsFalse()
    {
        var dialog = new SearchTeacherDialog([]);
        var entry = new TeacherSearchEntry("Mistrz Moran", "Mistrz Moran | dragon strike 65–95");

        Assert.False(dialog.NameBox.ItemFilter!("nieznajomy", entry));
    }

    [AvaloniaFact]
    public void ItemFilter_EmptySearchText_ReturnsFalse()
    {
        var dialog = new SearchTeacherDialog([]);
        var entry = new TeacherSearchEntry("Mistrz Moran", "Mistrz Moran");

        Assert.False(dialog.NameBox.ItemFilter!(string.Empty, entry));
    }
}
