using MudClient.Core.Character;

namespace MudClient.Core.Tests;

public sealed class ScorePhrasesTests
{
    [Theory]
    [InlineData("Twoja sila jest polboska.", "214+")]
    [InlineData("Twoja sila jest legendarna.", "200-213")]
    [InlineData("Twoja sila jest niespotykana.", "186-199")]
    [InlineData("Twoja sila jest niezmiernie wysoka.", "172-185")]
    [InlineData("Twoja sila jest wysoka.", "158-171")]
    [InlineData("Twoja sila jest niezla.", "144-157")]
    [InlineData("Twoja sila jest nieprzecietna.", "130-143")]
    [InlineData("Twoja sila jest srednia.", "116-129")]
    [InlineData("Twoja sila jest ponizej przecietnej.", "102-115")]
    [InlineData("Twoja sila jest bardzo niska.", "88-101")]
    [InlineData("Twoja sila jest godna pozalowania.", "74-87")]
    public void TryGetRange_RecognizesEveryTier(string line, string expected)
    {
        Assert.True(ScorePhrases.TryGetRange(line, out var range));
        Assert.Equal(expected, range);
    }

    [Theory]
    [InlineData("Twoja zrecznosc jest niezla.", "144-157")]
    [InlineData("Twoja kondycja jest srednia.", "116-129")]
    [InlineData("Twoja inteligencja jest nieprzecietna.", "130-143")]
    [InlineData("Twoja wiedza jest srednia.", "116-129")]
    [InlineData("Twoja charyzma jest nieprzecietna.", "130-143")]
    public void TryGetRange_RecognizesEveryStatName(string line, string expected)
    {
        Assert.True(ScorePhrases.TryGetRange(line, out var range));
        Assert.Equal(expected, range);
    }

    [Fact]
    public void TryGetRange_LongerTier_WinsOverShorterTierInsideIt()
    {
        // "niezmiernie wysoka" must win over the bare "wysoka" (158-171) it contains.
        Assert.True(ScorePhrases.TryGetRange("Twoja sila jest niezmiernie wysoka.", out var range));
        Assert.Equal("172-185", range);
    }

    [Fact]
    public void TryGetRange_UnrecognizedTierWording_FallsBackToLowestRange()
    {
        Assert.True(ScorePhrases.TryGetRange("Twoja sila jest jakas dziwna.", out var range));
        Assert.Equal("<73", range);
    }

    [Fact]
    public void TryGetRange_NotAScoreLine_ReturnsFalse()
    {
        Assert.False(ScorePhrases.TryGetRange("Rozglądasz się dookoła.", out _));
    }

    [Fact]
    public void TryGetRange_StripsAnsiBeforeMatching()
    {
        var esc = (char)0x1B;
        var line = $"{esc}[32mTwoja sila jest srednia.{esc}[0m";

        Assert.True(ScorePhrases.TryGetRange(line, out var range));
        Assert.Equal("116-129", range);
    }
}
