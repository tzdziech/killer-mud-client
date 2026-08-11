using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.App.Services;

/// <summary>
/// Extracts every spell row from a "spell"/"spell all" command's output chunk — e.g.
/// "Krag 1: (29)[1] armor  (  ) transmute staff" yields ("armor", Known: true) and
/// ("transmute staff", Known: false). Unlike <see cref="SpellSourceAnnotator"/> (which only
/// splices annotations onto already-missing entries for display), this captures every row,
/// known or not, so the caller can build up a persistent picture of the player's whole class
/// spell list — see <see cref="Models.ProfileSpellEntry"/>.
/// </summary>
public static class SpellKnowledgeParser
{
    // Gates parsing on the chunk actually being spell-list output — without this, the row
    // pattern below (a fairly generic "(x) name" shape) could false-positive on unrelated text.
    private static readonly Regex CircleHeaderPattern = new(
        @"Kr[ąa]g\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Same shape as SpellSourceAnnotator.SpellRowPattern — the "[<circle>]" tag is only printed
    // once a spell has some count (even blank shows just "(  ) name" with no bracket).
    private static readonly Regex SpellRowPattern = new(
        @"\((?<count>[^)]*)\)(?:\[\d+\])?\s+(?<name>\S(?:.*?\S)?)(?=\s{2,}|\s*$)",
        RegexOptions.Compiled);

    public static IReadOnlyList<(string Name, bool Known)> Parse(string chunk)
    {
        var plain = AnsiText.StripAnsi(chunk);
        if (!CircleHeaderPattern.IsMatch(plain))
        {
            return [];
        }

        var results = new List<(string Name, bool Known)>();
        foreach (Match match in SpellRowPattern.Matches(plain))
        {
            var name = match.Groups["name"].Value.Trim();
            var known = !string.IsNullOrWhiteSpace(match.Groups["count"].Value);
            results.Add((name, known));
        }

        return results;
    }
}
