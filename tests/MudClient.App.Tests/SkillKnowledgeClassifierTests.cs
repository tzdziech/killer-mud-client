using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class SkillKnowledgeClassifierTests
{
    [Fact]
    public void Classify_NoKnowledgeCollected_ReturnsUnknown()
    {
        var state = SkillKnowledgeClassifier.Classify("axe", 50, new Dictionary<string, int>());

        Assert.Equal(SkillKnowledgeState.Unknown, state);
    }

    [Fact]
    public void Classify_NullKnowledge_ReturnsUnknown()
    {
        var state = SkillKnowledgeClassifier.Classify("axe", 50, null);

        Assert.Equal(SkillKnowledgeState.Unknown, state);
    }

    [Fact]
    public void Classify_CurrentAtTeacherMax_ReturnsKnown()
    {
        var knowledge = new Dictionary<string, int> { ["axe"] = 50 };

        var state = SkillKnowledgeClassifier.Classify("axe", 50, knowledge);

        Assert.Equal(SkillKnowledgeState.Known, state);
    }

    [Fact]
    public void Classify_CurrentAboveTeacherMax_ReturnsKnown()
    {
        var knowledge = new Dictionary<string, int> { ["axe"] = 90 };

        var state = SkillKnowledgeClassifier.Classify("axe", 50, knowledge);

        Assert.Equal(SkillKnowledgeState.Known, state);
    }

    [Fact]
    public void Classify_CurrentBelowTeacherMax_ReturnsLearnable()
    {
        var knowledge = new Dictionary<string, int> { ["axe"] = 10 };

        var state = SkillKnowledgeClassifier.Classify("axe", 50, knowledge);

        Assert.Equal(SkillKnowledgeState.Learnable, state);
    }

    [Fact]
    public void Classify_UnboundedTeacherMax_AlwaysLearnable()
    {
        var knowledge = new Dictionary<string, int> { ["axe"] = 999 };

        var state = SkillKnowledgeClassifier.Classify("axe", null, knowledge);

        Assert.Equal(SkillKnowledgeState.Learnable, state);
    }

    [Fact]
    public void Classify_SkillAbsentFromKnowledge_ReturnsNotLearnable()
    {
        var knowledge = new Dictionary<string, int> { ["axe"] = 50 };

        var state = SkillKnowledgeClassifier.Classify("dagger", 50, knowledge);

        Assert.Equal(SkillKnowledgeState.NotLearnable, state);
    }

    [Fact]
    public void Classify_CaseInsensitiveDictionary_MatchesRegardlessOfCase()
    {
        var knowledge = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Axe"] = 50 };

        var state = SkillKnowledgeClassifier.Classify("axe", 50, knowledge);

        Assert.Equal(SkillKnowledgeState.Known, state);
    }
}
