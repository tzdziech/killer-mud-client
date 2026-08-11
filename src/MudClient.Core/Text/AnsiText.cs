using System.Text;
using System.Text.RegularExpressions;

namespace MudClient.Core.Text;

/// <summary>Strips ANSI/VT100 escape sequences from MUD text so it can be pattern-matched as
/// plain text (colors are still delivered to the UI separately for display).</summary>
public static partial class AnsiText
{
    public static string StripAnsi(string value) => AnsiRegex().Replace(value, string.Empty);

    /// <summary>Same stripping as <see cref="StripAnsi"/>, but also returns each kept
    /// character's index in <paramref name="value"/> (i.e. <c>OriginalIndexes[i]</c> is where
    /// <c>Plain[i]</c> came from). Lets a caller regex-match the returned plain text — immune to
    /// a color code splitting up what would otherwise be a contiguous whitespace-delimited token,
    /// e.g. a coloured skill percentage in the "skill" command's output — and then translate a
    /// match's start/end back into a splice position in the original, colored text.</summary>
    public static (string Plain, IReadOnlyList<int> OriginalIndexes) StripAnsiWithMap(string value)
    {
        var matches = AnsiRegex().Matches(value);
        var plain = new StringBuilder(value.Length);
        var indexes = new List<int>(value.Length);
        var cursor = 0;
        var matchIndex = 0;

        while (cursor < value.Length)
        {
            if (matchIndex < matches.Count && matches[matchIndex].Index == cursor)
            {
                cursor += matches[matchIndex].Length;
                matchIndex++;
                continue;
            }

            plain.Append(value[cursor]);
            indexes.Add(cursor);
            cursor++;
        }

        return (plain.ToString(), indexes);
    }

    [GeneratedRegex("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiRegex();
}
