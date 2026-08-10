using MudClient.App.Controls;
using MudClient.App.Models;
using Xunit;

namespace MudClient.App.Tests;

public sealed class WorldMapControlSpellMobTooltipTests
{
    [Fact]
    public void FormatSpellMobTooltip_ListsRegionClassAndSpells()
    {
        var mob = new SpellMobEntry(
            "100", "Świrnięty mag", "Arras", "Mag",
            ["Healing sleep", "Lightning bolt"], null, false, false, false, false, null);

        var text = WorldMapControl.FormatSpellMobTooltip([mob]);

        Assert.StartsWith("Świrnięty mag", text);
        Assert.Contains("Arras · Mag", text);
        Assert.Contains("zaklęcia: Healing sleep, Lightning bolt", text);
    }

    [Fact]
    public void FormatSpellMobTooltip_NoSpells_SaysSo()
    {
        var mob = new SpellMobEntry("100", "Ktoś", "Region", "Mag", [], null, false, false, false, false, null);

        var text = WorldMapControl.FormatSpellMobTooltip([mob]);

        Assert.Contains("zaklęcia: brak danych", text);
    }

    [Fact]
    public void FormatSpellMobTooltip_DangerousBossLocked_AreTaggedInTheHeader()
    {
        var mob = new SpellMobEntry("100", "Wódz Ogrów", "Forteca", "Kleryk", [], null, false, true, true, true, null);

        var text = WorldMapControl.FormatSpellMobTooltip([mob]);

        Assert.StartsWith("Wódz Ogrów [boss, niebezpieczny, zamknięte/wymaga klucza]", text);
    }

    [Fact]
    public void FormatSpellMobTooltip_IncludesNotesWhenPresent()
    {
        var mob = new SpellMobEntry(
            "100", "Ktoś", "Region", "Mag", [], "Ukryty", false, false, false, false, null);

        var text = WorldMapControl.FormatSpellMobTooltip([mob]);

        Assert.Contains("  Ukryty", text);
    }

    [Fact]
    public void FormatSpellMobTooltip_MultipleMobsInSameRoom_AreSeparatedByBlankLine()
    {
        var first = new SpellMobEntry("100", "Pierwszy", "Region", "Mag", [], null, false, false, false, false, null);
        var second = new SpellMobEntry("100", "Drugi", "Region", "Mag", [], null, false, false, false, false, null);

        var text = WorldMapControl.FormatSpellMobTooltip([first, second]);

        Assert.Contains("Pierwszy\n  Region · Mag\n  zaklęcia: brak danych\n\nDrugi", text);
    }
}
