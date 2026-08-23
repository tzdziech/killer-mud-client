using MudClient.Core.Automation;

namespace MudClient.Core.Tests;

public sealed class CombatStatusPolicyTests
{
    [Theory]
    [InlineData("lying", true)]
    [InlineData("LYING", true)]
    [InlineData("standing", false)]
    [InlineData(null, false)]
    public void IsLyingPosition_MatchesCaseInsensitively(string? position, bool expected)
    {
        Assert.Equal(expected, CombatStatusPolicy.IsLyingPosition(position));
    }

    [Theory]
    [InlineData("Ogr powala cię na ziemię!", true)]
    [InlineData("Ogr powala cie na ziemie!", true)]
    [InlineData("Przewracasz się!", true)]
    [InlineData("Przewracasz sie!", true)]
    [InlineData("Osuwasz się półprzytomny na ziemię.", true)]
    [InlineData("Osuwasz sie polprzytomny na ziemie.", true)]
    [InlineData("Z jękiem przewracasz się na ziemię.", true)]
    [InlineData("Z jeknieciem przewracasz sie na ziemie.", true)]
    [InlineData("Nic się nie dzieje.", false)]
    public void IsKnockedDownLine_FoldsDiacriticsBeforeMatching(string line, bool expected)
    {
        Assert.Equal(expected, CombatStatusPolicy.IsKnockedDownLine(line));
    }

    [Theory]
    [InlineData("Ogr rozbraja cię!", true)]
    [InlineData("Ogr rozbraja cie!", true)]
    [InlineData("Miecz wypada ci z rąk!", true)]
    [InlineData("Miecz wypada ci z rak!", true)]
    [InlineData("Nic się nie dzieje.", false)]
    public void IsDisarmedLine_FoldsDiacriticsBeforeMatching(string line, bool expected)
    {
        Assert.Equal(expected, CombatStatusPolicy.IsDisarmedLine(line));
    }
}
