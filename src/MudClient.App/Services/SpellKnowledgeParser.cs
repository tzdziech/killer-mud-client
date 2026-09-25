using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.App.Services;

/// <summary>
/// Extracts every spell row from a "spells"/"spells all" command's output chunk — e.g.
/// "Krag 1: (29)[1] armor  (  ) transmute staff" yields the known state, casting level and circle.
/// Unlike <see cref="SpellSourceAnnotator"/> (which only
/// splices annotations onto already-missing entries for display), this captures every row,
/// known or not, so the caller can build up a persistent picture of the player's whole class
/// spell list — see <see cref="Models.ProfileSpellEntry"/>. The caller decides whether to show
/// blank rows as missing spells; the ordinary <c>spells</c> response contains only known rows.
/// </summary>
public static class SpellKnowledgeParser
{
    // Gates parsing on the chunk actually being spell-list output — without this, the row
    // pattern below (a fairly generic "(x) name" shape) could false-positive on unrelated text.
    private static readonly Regex CircleHeaderPattern = new(
        @"Kr[ąa]g\s+(?<circle>\d+):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SpellBookHeaderPattern = new(
        @"Ksi[eę]ga\s+Zakl[eę][cć]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // A <c>spells</c> row starts with a casting level in parentheses. The <c>mem</c> response
    // also has "Krąg N:" headers, but its rows start with square brackets ([ 2]armor), so it
    // must not be buffered as an unfinished spells response.
    private static readonly Regex SpellsListStartPattern = new(
        @"Kr[ąa]g\s+\d+:\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Same shape as SpellSourceAnnotator.SpellRowPattern — the "[<circle>]" tag is only printed
    // once a spell has some count (even blank shows just "(  ) name" with no bracket).
    private static readonly Regex SpellRowPattern = new(
        @"\((?<count>[^)]*)\)(?:\[\d+\])?\s+(?<name>\S(?:.*?\S)?)(?=\s{2,}|\s*$)",
        RegexOptions.Compiled);

    public static IReadOnlyList<(string Name, bool Known, int? CastingLevel, int? Circle)> Parse(string chunk)
    {
        var plain = AnsiText.StripAnsi(chunk);
        if (!CircleHeaderPattern.IsMatch(plain))
        {
            return [];
        }

        var results = new List<(string Name, bool Known, int? CastingLevel, int? Circle)>();
        int? circle = null;
        foreach (var line in plain.Split('\n'))
        {
            var header = CircleHeaderPattern.Match(line);
            if (header.Success)
            {
                circle = int.Parse(header.Groups["circle"].Value);
            }

            foreach (Match match in SpellRowPattern.Matches(line))
            {
                var name = match.Groups["name"].Value.Trim();
                var count = match.Groups["count"].Value.Trim();
                var known = count.Length > 0;
                results.Add((
                    name,
                    known,
                    int.TryParse(count, out var castingLevel) ? castingLevel : null,
                    circle));
            }
        }

        return results;
    }

    /// <summary>Returns the stripped-text position of a real <c>spells</c> list start.</summary>
    public static bool TryFindSpellsListStart(string text, out int index)
    {
        var match = SpellsListStartPattern.Match(AnsiText.StripAnsi(text));
        index = match.Index;
        return match.Success;
    }

    /// <summary>Whether the text contains the server's <c>Księga Zaklęć</c> response header.</summary>
    public static bool ContainsSpellBookHeader(string text) =>
        SpellBookHeaderPattern.IsMatch(AnsiText.StripAnsi(text));
}
