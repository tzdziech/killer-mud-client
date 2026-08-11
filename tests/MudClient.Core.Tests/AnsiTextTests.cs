using MudClient.Core.Text;

namespace MudClient.Core.Tests;

public sealed class AnsiTextTests
{
    private const string Esc = "\x1B";

    [Fact]
    public void StripAnsiWithMap_NoEscapeCodes_ReturnsInputWithIdentityMap()
    {
        var (plain, indexes) = AnsiText.StripAnsiWithMap("hello");

        Assert.Equal("hello", plain);
        Assert.Equal([0, 1, 2, 3, 4], indexes);
    }

    [Fact]
    public void StripAnsiWithMap_StripsColorCodes_KeepingVisibleTextOnly()
    {
        var (plain, _) = AnsiText.StripAnsiWithMap($"{Esc}[32mhello{Esc}[0m");

        Assert.Equal("hello", plain);
    }

    [Fact]
    public void StripAnsiWithMap_IndexesPointBackToOriginalPositionOfEachVisibleChar()
    {
        var input = $"ab{Esc}[32mcd{Esc}[0mef";
        var (plain, indexes) = AnsiText.StripAnsiWithMap(input);

        Assert.Equal("abcdef", plain);
        for (var i = 0; i < plain.Length; i++)
        {
            Assert.Equal(plain[i], input[indexes[i]]);
        }
    }

    [Fact]
    public void StripAnsiWithMap_ColorWrappingASingleDigit_SplitsItFromItsNeighborsInPlainText()
    {
        // The exact scenario that broke SkillTrainerAnnotator: a lone digit colored in the
        // middle of what otherwise reads as contiguous whitespace-delimited numbers.
        var input = $"10  {Esc}[32m3{Esc}[0m + 0";
        var (plain, _) = AnsiText.StripAnsiWithMap(input);

        Assert.Equal("10  3 + 0", plain);
    }
}
