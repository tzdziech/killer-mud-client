using MudClient.App.Controls;
using Xunit;

namespace MudClient.App.Tests;

public sealed class OutputLineTests
{
    // A single line has no newline to bound its growth otherwise — e.g. a decompression-bomb
    // MCCP2 payload with no line breaks would keep appending to one OutputLine forever.
    private const int MaxLength = 64 * 1024;

    [Fact]
    public void Append_WithinLimit_KeepsFullText()
    {
        var line = new OutputLine();

        line.Append("hello", default);

        Assert.Equal(5, line.Length);
        Assert.Equal("hello", line.Text);
    }

    [Fact]
    public void Append_ExceedingLimit_TruncatesAtMaxLength()
    {
        var line = new OutputLine();
        line.Append(new string('a', MaxLength - 10), default);

        line.Append(new string('b', 100), default);

        Assert.Equal(MaxLength, line.Length);
        Assert.Equal(10, line.Text.Count(c => c == 'b'));
    }

    [Fact]
    public void Append_AlreadyAtLimit_IgnoresFurtherAppends()
    {
        var line = new OutputLine();
        line.Append(new string('a', MaxLength), default);
        var textAtLimit = line.Text;

        line.Append(new string('c', 1000), default);

        Assert.Equal(MaxLength, line.Length);
        Assert.Equal(textAtLimit, line.Text);
    }

    [Fact]
    public void Append_RepeatedSmallChunks_NeverExceedsMaxLength()
    {
        var line = new OutputLine();

        for (var i = 0; i < 2000; i++)
        {
            line.Append(new string('x', 100), default);
        }

        Assert.Equal(MaxLength, line.Length);
        Assert.Equal(MaxLength, line.Text.Length);
    }
}
