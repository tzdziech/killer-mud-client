using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class SkillKnowledgeParserTests
{
    [Fact]
    public void Parse_SingleRow_ReturnsNameAndCurrent()
    {
        var line = "[WW]  axe                 10   3 + 0";

        var results = SkillKnowledgeParser.Parse(line);

        Assert.Contains(results, r => r.Name == "axe" && r.Current == 3);
        Assert.Contains(results, r => r.Name == "axe" && r.LearnableFromTeachers == 10 && r.ItemBonus == 0);
    }

    [Fact]
    public void Parse_TwoRowsOnOneLine_ReturnsBoth()
    {
        var line = "[WW]  axe                 10   3 + 0  [WW]  dagger              11   2 + 0";

        var results = SkillKnowledgeParser.Parse(line);

        Assert.Contains(results, r => r.Name == "axe" && r.Current == 3);
        Assert.Contains(results, r => r.Name == "dagger" && r.Current == 2);
    }

    [Fact]
    public void Parse_MultiWordSkillName_IsCapturedInFull()
    {
        var line = "[WW]  twohanded weapon     0  73 + 0";

        var results = SkillKnowledgeParser.Parse(line);

        Assert.Contains(results, r => r.Name == "twohanded weapon" && r.Current == 73);
        Assert.Contains(results, r => r.Name == "twohanded weapon" && r.LearnableFromTeachers == 0 && r.ItemBonus == 0);
    }

    [Fact]
    public void Parse_UsesTheLevelHeaderInsteadOfTheRowValues()
    {
        var results = SkillKnowledgeParser.Parse("Poziom 12: [WW]  axe                 0  73 + 0");

        Assert.Contains(results, r => r.Name == "axe" && r.Level == 12);
    }

    [Fact]
    public void Parse_MindLimitedSkill_RemovesMarkerAndKeepsTheReportedLimit()
    {
        var results = SkillKnowledgeParser.Parse("""
            [WW]  #twohanded weapon     0  87 + 5
            Ograniczenia skilli ze wzgledu na mozliwosci umyslowe postaci do (86)
            """);

        Assert.Contains(results, r => r.Name == "twohanded weapon" && r.IsMindLimited && r.MindLimit == 86);
    }

    [Fact]
    public void Parse_TextWithoutSkillTag_ReturnsEmpty()
    {
        Assert.Empty(SkillKnowledgeParser.Parse("axe 10 3 + 0"));
    }

    [Theory]
    [InlineData("Witaj w krainie Killer.")]
    [InlineData("")]
    public void Parse_UnrelatedText_ReturnsEmpty(string chunk)
    {
        Assert.Empty(SkillKnowledgeParser.Parse(chunk));
    }

    // ====================================================================
    // Regression guard: same ANSI-coloring hazard as SkillTrainerAnnotator/SpellKnowledgeParser —
    // this MUD colors the skill numbers in its "skill" output.
    // ====================================================================

    private const string Esc = "\x1B";

    [Fact]
    public void Parse_ColoredCurrentValue_StillClassifiesCorrectly()
    {
        var line = $"[WW]  axe                 10  {Esc}[32m3{Esc}[0m + 0";

        var results = SkillKnowledgeParser.Parse(line);

        Assert.Contains(results, r => r.Name == "axe" && r.Current == 3);
    }

    [Fact]
    public void Parse_ColorCodesAroundEveryNumber_StillClassifiesCorrectly()
    {
        var line = $"[WW]  axe          {Esc}[36m10{Esc}[0m  {Esc}[32m3{Esc}[0m + {Esc}[33m0{Esc}[0m";

        var results = SkillKnowledgeParser.Parse(line);

        Assert.Contains(results, r => r.Name == "axe" && r.Current == 3);
    }
}
