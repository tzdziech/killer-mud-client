using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using MudClient.App.Controls;
using MudClient.App.Models;
using Xunit;

namespace MudClient.App.Tests;

public sealed class WorldMapControlSpellMobTooltipTests
{
    private static readonly IReadOnlyDictionary<string, bool> NoKnowledge =
        new Dictionary<string, bool>();

    private static IReadOnlyList<TextBlock> Lines(Control content) =>
        Assert.IsType<StackPanel>(content).Children
            .SelectMany(mobBlock => Assert.IsType<StackPanel>(mobBlock).Children)
            .Cast<TextBlock>()
            .ToArray();

    private static string PlainText(TextBlock line) =>
        line.Inlines is null
            ? line.Text ?? string.Empty
            : string.Concat(line.Inlines.OfType<Run>().Select(run => run.Text));

    [Fact]
    public void FormatSpellMobTooltip_ListsRegionClassAndSpells()
    {
        var mob = new SpellMobEntry(
            "100", "Świrnięty mag", "Arras", "Mag",
            ["Healing sleep", "Lightning bolt"], null, false, false, false, false, null);

        var lines = Lines(WorldMapControl.FormatSpellMobTooltip([mob], NoKnowledge));

        Assert.Equal("Świrnięty mag", lines[0].Text);
        Assert.Equal("  Arras · Mag", lines[1].Text);
        Assert.Equal("  zaklęcia: Healing sleep, Lightning bolt", PlainText(lines[2]));
    }

    [Fact]
    public void FormatSpellMobTooltip_NoSpells_SaysSo()
    {
        var mob = new SpellMobEntry("100", "Ktoś", "Region", "Mag", [], null, false, false, false, false, null);

        var lines = Lines(WorldMapControl.FormatSpellMobTooltip([mob], NoKnowledge));

        Assert.Equal("  zaklęcia: brak danych", PlainText(lines[2]));
    }

    [Fact]
    public void FormatSpellMobTooltip_DangerousBossLocked_AreTaggedInTheHeader()
    {
        var mob = new SpellMobEntry("100", "Wódz Ogrów", "Forteca", "Kleryk", [], null, false, true, true, true, null);

        var lines = Lines(WorldMapControl.FormatSpellMobTooltip([mob], NoKnowledge));

        Assert.Equal("Wódz Ogrów [boss, niebezpieczny, zamknięte/wymaga klucza]", lines[0].Text);
    }

    [Fact]
    public void FormatSpellMobTooltip_IncludesNotesWhenPresent()
    {
        var mob = new SpellMobEntry(
            "100", "Ktoś", "Region", "Mag", [], "Ukryty", false, false, false, false, null);

        var lines = Lines(WorldMapControl.FormatSpellMobTooltip([mob], NoKnowledge));

        Assert.Equal("  Ukryty", lines[3].Text);
    }

    [Fact]
    public void FormatSpellMobTooltip_MultipleMobsInSameRoom_ProduceOneBlockEach()
    {
        var first = new SpellMobEntry("100", "Pierwszy", "Region", "Mag", [], null, false, false, false, false, null);
        var second = new SpellMobEntry("100", "Drugi", "Region", "Mag", [], null, false, false, false, false, null);

        var root = Assert.IsType<StackPanel>(WorldMapControl.FormatSpellMobTooltip([first, second], NoKnowledge));

        Assert.Equal(2, root.Children.Count);
        Assert.Equal("Pierwszy", Assert.IsType<StackPanel>(root.Children[0]).Children.OfType<TextBlock>().First().Text);
        Assert.Equal("Drugi", Assert.IsType<StackPanel>(root.Children[1]).Children.OfType<TextBlock>().First().Text);
    }

    // ====================================================================
    // Spell coloring — see SpellKnowledgeClassifierTests for the underlying rule; these confirm
    // WorldMapControl wires each classification to the right run styling.
    // ====================================================================

    [Fact]
    public void CreateSpellRun_KnownSpell_IsColoredDifferentlyFromDefaultNoStrikethrough()
    {
        var knowledge = new Dictionary<string, bool> { ["armor"] = true };
        var defaultForeground = new Run("armor").Foreground;

        var run = WorldMapControl.CreateSpellRun("armor", knowledge);

        Assert.NotEqual(defaultForeground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }

    [Fact]
    public void CreateSpellRun_MissingSpell_IsColoredDifferentlyFromDefaultNoStrikethrough()
    {
        var knowledge = new Dictionary<string, bool> { ["armor"] = false };
        var defaultForeground = new Run("armor").Foreground;

        var run = WorldMapControl.CreateSpellRun("armor", knowledge);

        Assert.NotEqual(defaultForeground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }

    [Fact]
    public void CreateSpellRun_NotLearnableSpell_IsStruckThrough()
    {
        var knowledge = new Dictionary<string, bool> { ["shield"] = true };

        var run = WorldMapControl.CreateSpellRun("armor", knowledge);

        Assert.NotNull(run.TextDecorations);
    }

    [Fact]
    public void CreateSpellRun_KnownAndMissing_UseDifferentColors()
    {
        var knowledge = new Dictionary<string, bool> { ["armor"] = true, ["shield"] = false };

        var known = WorldMapControl.CreateSpellRun("armor", knowledge);
        var missing = WorldMapControl.CreateSpellRun("shield", knowledge);

        Assert.NotEqual(known.Foreground, missing.Foreground);
    }

    [Fact]
    public void CreateSpellRun_NoKnowledgeDataAtAll_LeavesDefaultStyling()
    {
        var run = WorldMapControl.CreateSpellRun("armor", NoKnowledge);
        var untouched = new Run("armor");

        Assert.Equal(untouched.Foreground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }
}
