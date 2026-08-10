using MudClient.App.Controls;
using MudClient.App.Models;
using Xunit;

namespace MudClient.App.Tests;

public sealed class WorldMapControlTeacherTooltipTests
{
    [Fact]
    public void FormatTeacherTooltip_ListsSkillsAndTricksWithTheirTerms()
    {
        var teacher = new TeacherEntry(
            "100", "Mistrz Moran", "Carrallak", null, "500",
            ["Wojownik"],
            [new TeacherSkillEntry("dragon strike", 65, 95, 65, 90)],
            [new TeacherTrickEntry("vertical kick", 25, 5000)]);

        var text = WorldMapControl.FormatTeacherTooltip([teacher]);

        Assert.StartsWith("Mistrz Moran", text);
        Assert.Contains("dragon strike — zakres 65–95, wymaga od 65, cena 90%", text);
        Assert.Contains("vertical kick — szansa nauki 25%, cena 5000 $", text);
    }

    [Fact]
    public void FormatTeacherTooltip_NoOfferings_SaysSo()
    {
        var teacher = new TeacherEntry("100", "Ktoś", "Region", null, "500", [], [], []);

        var text = WorldMapControl.FormatTeacherTooltip([teacher]);

        Assert.Contains("brak danych o szkoleniu", text);
    }

    [Fact]
    public void FormatTeacherTooltip_MultipleTeachersInSameRoom_AreSeparatedByBlankLine()
    {
        var first = new TeacherEntry("100", "Pierwszy", "Region", null, "500", [], [], []);
        var second = new TeacherEntry("200", "Drugi", "Region", null, "500", [], [], []);

        var text = WorldMapControl.FormatTeacherTooltip([first, second]);

        Assert.Contains("Pierwszy\n  brak danych o szkoleniu\n\nDrugi", text);
    }
}
