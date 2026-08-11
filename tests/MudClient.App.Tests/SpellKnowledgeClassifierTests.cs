using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class SpellKnowledgeClassifierTests
{
    [Fact]
    public void Classify_NoKnowledgeCollected_ReturnsUnknown()
    {
        var state = SpellKnowledgeClassifier.Classify("armor", new Dictionary<string, bool>());

        Assert.Equal(SpellKnowledgeState.Unknown, state);
    }

    [Fact]
    public void Classify_NullKnowledge_ReturnsUnknown()
    {
        var state = SpellKnowledgeClassifier.Classify("armor", null);

        Assert.Equal(SpellKnowledgeState.Unknown, state);
    }

    [Fact]
    public void Classify_SpellPresentAndKnown_ReturnsKnown()
    {
        var knowledge = new Dictionary<string, bool> { ["armor"] = true };

        var state = SpellKnowledgeClassifier.Classify("armor", knowledge);

        Assert.Equal(SpellKnowledgeState.Known, state);
    }

    [Fact]
    public void Classify_SpellPresentButNotKnown_ReturnsMissing()
    {
        var knowledge = new Dictionary<string, bool> { ["transmute staff"] = false };

        var state = SpellKnowledgeClassifier.Classify("transmute staff", knowledge);

        Assert.Equal(SpellKnowledgeState.Missing, state);
    }

    [Fact]
    public void Classify_SpellAbsentFromKnowledge_ReturnsNotLearnable()
    {
        var knowledge = new Dictionary<string, bool> { ["armor"] = true };

        var state = SpellKnowledgeClassifier.Classify("cyclone", knowledge);

        Assert.Equal(SpellKnowledgeState.NotLearnable, state);
    }

    [Fact]
    public void Classify_CaseInsensitiveDictionary_MatchesRegardlessOfCase()
    {
        var knowledge = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { ["Armor"] = true };

        var state = SpellKnowledgeClassifier.Classify("armor", knowledge);

        Assert.Equal(SpellKnowledgeState.Known, state);
    }
}
