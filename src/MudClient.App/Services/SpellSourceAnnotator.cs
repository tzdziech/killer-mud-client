using System.Text;
using System.Text.RegularExpressions;
using MudClient.App.Models;
using MudClient.Core.Text;

namespace MudClient.App.Services;

/// <summary>
/// Splices " (Moby)" onto each row of the "spell"/known-spells list output (e.g.
/// "(  )[1] shield" -&gt; "(  )[1] shield (Rogaty demon)"), naming every currently known
/// spellbook-dropping mob whose book teaches a spell the player is still missing. A spell already
/// known shows a number inside the parentheses (e.g. "(29)[1] armor") — those are left untouched,
/// since there's nothing to source for a spell already learned; only an empty "(  )" is missing.
/// </summary>
public static class SpellSourceAnnotator
{
    // "(<mem count, blank when missing>)[<circle>] <spell name, one or more words>" — several
    // such entries typically share one line, column-padded with 2+ spaces before the next one
    // (or end of line for the last entry). The "[<circle>]" tag is only printed for spells the
    // player already knows something about — a fully missing spell prints just "(  ) name", so
    // the bracket group is optional.
    private static readonly Regex SpellRowPattern = new(
        @"\((?<count>[^)]*)\)(?:\[\d+\])?\s+(?<name>\S(?:.*?\S)?)(?=\s{2,}|\s*$)",
        RegexOptions.Compiled);

    /// <summary>Returns <paramref name="line"/> unchanged unless it contains at least one
    /// recognized, still-missing spell row. Matches against an ANSI-stripped copy of
    /// <paramref name="line"/> (see <see cref="AnsiText.StripAnsiWithMap"/>) — same reasoning as
    /// <see cref="SkillTrainerAnnotator"/>: this MUD's "known spells" listing colors entries, and
    /// those escape codes would otherwise sit inside what looks like plain column padding.</summary>
    public static string Annotate(string line, IReadOnlyList<SpellMobEntry> spellMobs)
    {
        if (spellMobs.Count == 0)
        {
            return line;
        }

        var (plain, originalIndexes) = AnsiText.StripAnsiWithMap(line);
        if (!plain.Contains('(', StringComparison.Ordinal) || !plain.Contains(')', StringComparison.Ordinal))
        {
            return line;
        }

        var matches = SpellRowPattern.Matches(plain);
        if (matches.Count == 0)
        {
            return line;
        }

        var builder = new StringBuilder(line.Length + matches.Count * 24);
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups["count"].Value))
            {
                // Already known — nothing to source, leave this entry untouched.
                continue;
            }

            var matchEndInPlain = match.Index + match.Length;
            var matchEndInLine = matchEndInPlain <= originalIndexes.Count
                ? originalIndexes[matchEndInPlain - 1] + 1
                : line.Length;

            builder.Append(line, lastIndex, matchEndInLine - lastIndex);
            lastIndex = matchEndInLine;

            var spellName = match.Groups["name"].Value.Trim();
            var sources = FindSpellSources(spellName, spellMobs);
            if (sources.Count > 0)
            {
                builder.Append(" (").Append(string.Join(", ", sources)).Append(')');
            }
        }

        builder.Append(line, lastIndex, line.Length - lastIndex);
        return builder.ToString();
    }

    /// <summary>Every known spellbook-dropping mob whose book teaches
    /// <paramref name="spellName"/>, case-insensitively matched.</summary>
    internal static IReadOnlyList<string> FindSpellSources(
        string spellName, IReadOnlyList<SpellMobEntry> spellMobs)
    {
        var names = new List<string>();
        foreach (var mob in spellMobs)
        {
            if (mob.Spells.Any(spell => string.Equals(spell, spellName, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(mob.Mob);
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
