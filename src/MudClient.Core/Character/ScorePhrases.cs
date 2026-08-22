using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.Core.Character;

/// <summary>
/// Maps this MUD's "score" command word-scale stat descriptions ("Twoja sila jest srednia.") to
/// their approximate numeric range. Tiers are checked in the order below — deliberately not
/// sorted or looked up by exact match — so a multi-word tier like "niezmiernie wysoka" wins over
/// the shorter "wysoka" it contains, the same ordering trick <see cref="Combat.DamagePhrases"/>
/// uses for its own multi-word verb tiers.
/// </summary>
public static class ScorePhrases
{
    private static readonly (string Phrase, string Range)[] Tiers =
    [
        ("polboska", "214+"),
        ("legendarna", "200-213"),
        ("niespotykana", "186-199"),
        ("niezmiernie wysoka", "172-185"),
        ("wysoka", "158-171"),
        ("niezla", "144-157"),
        ("nieprzecietna", "130-143"),
        ("srednia", "116-129"),
        ("ponizej przecietnej", "102-115"),
        ("bardzo niska", "88-101"),
        ("godna pozalowania", "74-87"),
    ];

    /// <summary>An untiered stat description below "godna pozalowania" — the game's own lowest
    /// named tier — falls back to this range, mirroring the "&lt;73" default the trigger this was
    /// ported from used for anything it didn't otherwise recognize.</summary>
    private const string UntieredRange = "<73";

    private static readonly Regex ScoreLinePattern = new(
        @"\bTwoja \S+ jest\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Finds a "score" stat line in <paramref name="line"/> (ANSI escape codes are
    /// stripped before matching) and returns its approximate numeric range. False for a line that
    /// isn't a recognized stat line at all — a stat line whose wording doesn't match any known
    /// tier still returns true with <see cref="UntieredRange"/>, since "Twoja X jest Y." with an
    /// unrecognized Y is still worth annotating.</summary>
    public static bool TryGetRange(string line, out string range)
    {
        var plain = AnsiText.StripAnsi(line);
        if (!ScoreLinePattern.IsMatch(plain))
        {
            range = string.Empty;
            return false;
        }

        foreach (var (phrase, tierRange) in Tiers)
        {
            if (plain.Contains(phrase, StringComparison.Ordinal))
            {
                range = tierRange;
                return true;
            }
        }

        range = UntieredRange;
        return true;
    }
}
