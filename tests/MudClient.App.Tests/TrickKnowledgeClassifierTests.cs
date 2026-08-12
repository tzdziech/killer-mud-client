using MudClient.App.Models;
using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class TrickKnowledgeClassifierTests
{
    private static TeacherTrickEntry Trick(
        (string Skill, int MinPercent)[] requirements, bool requiresAll = true) =>
        new(
            "test trick", 20, 1000,
            Requirements: requirements.Select(r => new TrickRequirement(r.Skill, r.MinPercent)).ToArray(),
            RequiresAllRequirements: requiresAll);

    [Fact]
    public void MeetsRequirements_NoRequirementsTranscribed_ReturnsFalse()
    {
        var trick = new TeacherTrickEntry("thigh jab", 25, 6666);

        Assert.False(TrickKnowledgeClassifier.MeetsRequirements(trick, new Dictionary<string, int> { ["dagger"] = 99 }));
    }

    [Fact]
    public void MeetsRequirements_SingleRequirementMet_ReturnsTrue()
    {
        var trick = Trick([("kick", 85)]);

        Assert.True(TrickKnowledgeClassifier.MeetsRequirements(trick, new Dictionary<string, int> { ["kick"] = 85 }));
    }

    [Fact]
    public void MeetsRequirements_SingleRequirementAboveThreshold_ReturnsTrue()
    {
        var trick = Trick([("kick", 85)]);

        Assert.True(TrickKnowledgeClassifier.MeetsRequirements(trick, new Dictionary<string, int> { ["kick"] = 99 }));
    }

    [Fact]
    public void MeetsRequirements_SingleRequirementBelowThreshold_ReturnsFalse()
    {
        var trick = Trick([("kick", 85)]);

        Assert.False(TrickKnowledgeClassifier.MeetsRequirements(trick, new Dictionary<string, int> { ["kick"] = 50 }));
    }

    [Fact]
    public void MeetsRequirements_SkillNeverSeen_ReturnsFalse()
    {
        var trick = Trick([("kick", 85)]);

        Assert.False(TrickKnowledgeClassifier.MeetsRequirements(trick, new Dictionary<string, int>()));
    }

    [Fact]
    public void MeetsRequirements_AndRequirement_NeedsBothMet()
    {
        var trick = Trick([("spear", 91), ("twohanded weapon", 91)], requiresAll: true);

        Assert.False(TrickKnowledgeClassifier.MeetsRequirements(
            trick, new Dictionary<string, int> { ["spear"] = 91, ["twohanded weapon"] = 50 }));
        Assert.True(TrickKnowledgeClassifier.MeetsRequirements(
            trick, new Dictionary<string, int> { ["spear"] = 91, ["twohanded weapon"] = 91 }));
    }

    [Fact]
    public void MeetsRequirements_OrRequirement_NeedsOnlyOneMet()
    {
        var trick = Trick([("sword", 80), ("short-sword", 80)], requiresAll: false);

        Assert.True(TrickKnowledgeClassifier.MeetsRequirements(
            trick, new Dictionary<string, int> { ["short-sword"] = 80 }));
        Assert.False(TrickKnowledgeClassifier.MeetsRequirements(
            trick, new Dictionary<string, int> { ["sword"] = 10, ["short-sword"] = 10 }));
    }
}
