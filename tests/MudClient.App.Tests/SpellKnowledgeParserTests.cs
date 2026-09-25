using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class SpellKnowledgeParserTests
{
    [Fact]
    public void Parse_KnownAndMissingRows_ReturnsBothWithCorrectFlag()
    {
        var chunk = "Krag 1: (29)[1] armor                    (  ) transmute staff";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Contains(results, r => r.Name == "armor" && r.Known);
        Assert.Contains(results, r => r.Name == "armor" && r.CastingLevel == 29 && r.Circle == 1);
        Assert.Contains(results, r => r.Name == "transmute staff" && !r.Known);
        Assert.Contains(results, r => r.Name == "transmute staff" && r.CastingLevel is null && r.Circle == 1);
    }

    [Fact]
    public void Parse_UsesTheCircleHeaderAndIgnoresTheBracketNumber()
    {
        var results = SpellKnowledgeParser.Parse("Krag 4: (33)[1] cure serious");

        Assert.Contains(results, r => r.Name == "cure serious" && r.Circle == 4);
    }

    [Fact]
    public void Parse_MultiLineSpellsAllOutput_CollectsRowsFromContinuationLines()
    {
        var chunk =
            "Krag 1: (29)[1] armor                    (29)[1] bless                    (29)[1] cause light              \n" +
            "        (29)[1] create food              (  ) transmute staff          (29)[1] create water             \n" +
            "        (29)[1] light                    (29)[1] cure light               (29)[1] detect magic             ";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Equal(9, results.Count);
        Assert.Contains(results, r => r.Name == "detect magic" && r.Known);
        Assert.Contains(results, r => r.Name == "transmute staff" && !r.Known);
    }

    [Fact]
    public void Parse_SpellsOutputWithNumericLevel_ContainsOnlyKnownSpells()
    {
        var results = SpellKnowledgeParser.Parse("Krag 2: (33)[1] blindness  (34)[2] cure moderate");

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.True(result.Known));
        Assert.All(results, result => Assert.NotNull(result.CastingLevel));
    }

    [Fact]
    public void TryFindSpellsListStart_AcceptsParenthesesButRejectsMemSquareBrackets()
    {
        Assert.True(SpellKnowledgeParser.TryFindSpellsListStart(
            "Krag 1: (26)[1] armor", out var spellsIndex));
        Assert.Equal(0, spellsIndex);
        Assert.False(SpellKnowledgeParser.TryFindSpellsListStart(
            "Krag 1: [ 2]armor             [ 2]bless", out _));
    }

    [Fact]
    public void ContainsSpellBookHeader_RecognizesAsciiAndPolishHeadersButNotMemHeader()
    {
        Assert.True(SpellKnowledgeParser.ContainsSpellBookHeader("== Ksiega Zaklec =="));
        Assert.True(SpellKnowledgeParser.ContainsSpellBookHeader("== Księga Zaklęć =="));
        Assert.False(SpellKnowledgeParser.ContainsSpellBookHeader("== Czary aktualnie zapamietane =="));
    }

    [Fact]
    public void Parse_TextWithoutCircleHeader_ReturnsEmpty()
    {
        var chunk = "(29)[1] armor  (  ) transmute staff";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Witaj w krainie Killer.")]
    [InlineData("")]
    public void Parse_UnrelatedText_ReturnsEmpty(string chunk)
    {
        Assert.Empty(SpellKnowledgeParser.Parse(chunk));
    }

    [Fact]
    public void Parse_PolishDiacriticHeader_IsRecognizedToo()
    {
        var chunk = "Krąg 2: (  ) fireball";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Contains(results, r => r.Name == "fireball" && !r.Known);
    }

    // ====================================================================
    // Regression guard: same ANSI-coloring hazard as SkillTrainerAnnotator/SpellSourceAnnotator —
    // this MUD colors the casting-level counts in its "spells" output.
    // ====================================================================

    private const string Esc = "\x1B";

    [Fact]
    public void Parse_ColoredCount_StillClassifiesCorrectly()
    {
        var chunk = $"Krag 1: ({Esc}[32m29{Esc}[0m)[1] armor";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Contains(results, r => r.Name == "armor" && r.Known);
    }

    [Fact]
    public void Parse_ColoredBlankCount_StillClassifiesAsMissing()
    {
        var chunk = $"Krag 1: ({Esc}[32m  {Esc}[0m) transmute staff";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Contains(results, r => r.Name == "transmute staff" && !r.Known);
    }

    // ====================================================================
    // Regression guard: a game update introduced "( + )" for a spell the player already
    // has but cannot yet cast (e.g. still below the required level). It's a non-blank
    // count, so the existing "known" check already covers it — this locks that in.
    // ====================================================================

    [Fact]
    public void Parse_PlusMarkerCount_IsClassifiedAsKnown()
    {
        var chunk = "Krag 1: ( + ) charm person";

        var results = SpellKnowledgeParser.Parse(chunk);

        Assert.Contains(results, r => r.Name == "charm person" && r.Known);
    }
}
