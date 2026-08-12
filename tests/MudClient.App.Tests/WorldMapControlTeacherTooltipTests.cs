using Avalonia.Controls;
using Avalonia.Controls.Documents;
using MudClient.App.Controls;
using MudClient.App.Models;
using Xunit;

namespace MudClient.App.Tests;

public sealed class WorldMapControlTeacherTooltipTests
{
    private static readonly IReadOnlyDictionary<string, int> NoKnowledge = new Dictionary<string, int>();

    private static string PlainText(TextBlock line) =>
        line.Inlines is null
            ? line.Text ?? string.Empty
            : string.Concat(line.Inlines.OfType<Run>().Select(run => run.Text));

    [Fact]
    public void FormatTeacherTooltip_ListsSkillsAndTricksWithTheirTerms()
    {
        var teacher = new TeacherEntry(
            "100", "Mistrz Moran", "Carrallak", null, "500",
            ["Wojownik"],
            [new TeacherSkillEntry("dragon strike", 65, 95, 65, 90)],
            [new TeacherTrickEntry("vertical kick", 25, 5000)]);

        var root = Assert.IsType<StackPanel>(WorldMapControl.FormatTeacherTooltip([teacher], NoKnowledge));
        var block = Assert.IsType<StackPanel>(root.Children[0]);
        var lines = block.Children.Cast<TextBlock>().ToArray();

        Assert.Equal("Mistrz Moran", lines[0].Text);
        Assert.Equal("  dragon strike — zakres 65–95, wymaga od 65, cena 90%", PlainText(lines[1]));
        Assert.Equal("  vertical kick — szansa nauki 25%, cena 5000 $", PlainText(lines[2]));
    }

    [Fact]
    public void FormatTeacherTooltip_NoOfferings_SaysSo()
    {
        var teacher = new TeacherEntry("100", "Ktoś", "Region", null, "500", [], [], []);

        var root = Assert.IsType<StackPanel>(WorldMapControl.FormatTeacherTooltip([teacher], NoKnowledge));
        var block = Assert.IsType<StackPanel>(root.Children[0]);

        Assert.Equal("  brak danych o szkoleniu", block.Children.Cast<TextBlock>().Last().Text);
    }

    [Fact]
    public void FormatTeacherTooltip_MultipleTeachersInSameRoom_ProduceOneBlockEach()
    {
        var first = new TeacherEntry("100", "Pierwszy", "Region", null, "500", [], [], []);
        var second = new TeacherEntry("200", "Drugi", "Region", null, "500", [], [], []);

        var root = Assert.IsType<StackPanel>(WorldMapControl.FormatTeacherTooltip([first, second], NoKnowledge));

        Assert.Equal(2, root.Children.Count);
        Assert.Equal("Pierwszy", Assert.IsType<StackPanel>(root.Children[0]).Children.OfType<TextBlock>().First().Text);
        Assert.Equal("Drugi", Assert.IsType<StackPanel>(root.Children[1]).Children.OfType<TextBlock>().First().Text);
    }

    // ====================================================================
    // Skill coloring — see SkillKnowledgeClassifierTests for the underlying rule; these confirm
    // WorldMapControl wires each classification to the right run styling.
    // ====================================================================

    [Fact]
    public void CreateSkillRun_CurrentAtOrAboveTeacherMax_IsColoredDifferentlyFromDefaultNoStrikethrough()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var knowledge = new Dictionary<string, int> { ["axe"] = 50 };
        var defaultForeground = new Run("axe").Foreground;

        var run = WorldMapControl.CreateSkillRun(skill, knowledge);

        Assert.NotEqual(defaultForeground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }

    [Fact]
    public void CreateSkillRun_CurrentBelowTeacherMax_IsColoredDifferentlyFromDefaultNoStrikethrough()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var knowledge = new Dictionary<string, int> { ["axe"] = 10 };
        var defaultForeground = new Run("axe").Foreground;

        var run = WorldMapControl.CreateSkillRun(skill, knowledge);

        Assert.NotEqual(defaultForeground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }

    [Fact]
    public void CreateSkillRun_SkillNeverSeen_IsStruckThrough()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var knowledge = new Dictionary<string, int> { ["dagger"] = 10 };

        var run = WorldMapControl.CreateSkillRun(skill, knowledge);

        Assert.NotNull(run.TextDecorations);
    }

    [Fact]
    public void CreateSkillRun_KnownAndLearnable_UseDifferentColors()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var known = WorldMapControl.CreateSkillRun(skill, new Dictionary<string, int> { ["axe"] = 50 });
        var learnable = WorldMapControl.CreateSkillRun(skill, new Dictionary<string, int> { ["axe"] = 10 });

        Assert.NotEqual(known.Foreground, learnable.Foreground);
    }

    [Fact]
    public void CreateSkillRun_NoKnowledgeDataAtAll_LeavesDefaultStyling()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var defaultForeground = new Run("axe").Foreground;

        var run = WorldMapControl.CreateSkillRun(skill, NoKnowledge);

        Assert.Equal(defaultForeground, run.Foreground);
        Assert.Null(run.TextDecorations);
    }

    // ====================================================================
    // Trick coloring — see TrickKnowledgeClassifierTests for the underlying rule; these confirm
    // WorldMapControl wires "requirement met" to the same "learnable" gold skills use.
    // ====================================================================

    [Fact]
    public void CreateTrickRun_RequirementMet_IsColoredDifferentlyFromDefault()
    {
        var trick = new TeacherTrickEntry(
            "vertical kick", 25, 5000, Requirements: [new TrickRequirement("kick", 85)]);
        var knowledge = new Dictionary<string, int> { ["kick"] = 85 };
        var defaultForeground = new Run("vertical kick").Foreground;

        var run = WorldMapControl.CreateTrickRun(trick, knowledge);

        Assert.NotEqual(defaultForeground, run.Foreground);
    }

    [Fact]
    public void CreateTrickRun_RequirementNotMet_LeavesDefaultStyling()
    {
        var trick = new TeacherTrickEntry(
            "vertical kick", 25, 5000, Requirements: [new TrickRequirement("kick", 85)]);
        var knowledge = new Dictionary<string, int> { ["kick"] = 50 };
        var defaultForeground = new Run("vertical kick").Foreground;

        var run = WorldMapControl.CreateTrickRun(trick, knowledge);

        Assert.Equal(defaultForeground, run.Foreground);
    }

    [Fact]
    public void CreateTrickRun_NoRequirementDataTranscribed_LeavesDefaultStyling()
    {
        var trick = new TeacherTrickEntry("thigh jab", 25, 6666);
        var defaultForeground = new Run("thigh jab").Foreground;

        var run = WorldMapControl.CreateTrickRun(trick, new Dictionary<string, int> { ["dagger"] = 99 });

        Assert.Equal(defaultForeground, run.Foreground);
    }

    [Fact]
    public void CreateTrickRun_SameGoldAsLearnableSkill()
    {
        var skill = new TeacherSkillEntry("axe", 0, 50, 0, 100);
        var learnableSkillRun = WorldMapControl.CreateSkillRun(skill, new Dictionary<string, int> { ["axe"] = 10 });

        var trick = new TeacherTrickEntry(
            "vertical kick", 25, 5000, Requirements: [new TrickRequirement("kick", 85)]);
        var metTrickRun = WorldMapControl.CreateTrickRun(trick, new Dictionary<string, int> { ["kick"] = 85 });

        Assert.Equal(learnableSkillRun.Foreground, metTrickRun.Foreground);
    }
}
