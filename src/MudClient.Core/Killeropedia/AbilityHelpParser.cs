using System.Text;
using System.Text.RegularExpressions;

namespace MudClient.Core.Killeropedia;

/// <summary>One "ClassName (N lvl)" entry from a "Dostepne dla klas:" line.</summary>
public sealed record ClassLevelRequirement(string ClassName, int MinLevel);

/// <summary>Everything <see cref="AbilityHelpParser"/> can pull out of a single "help &lt;name&gt;"
/// entry's header fields, plus the free-text description below them.</summary>
public sealed record ParsedAbilityHelp(
    string Name,
    string? Type,
    IReadOnlyList<ClassLevelRequirement> AvailableForClasses,
    string? WandererSpecialization,
    string? Alignment,
    string? Target,
    string? Syntax,
    string? PolishEquivalent,
    string? School,
    string? MageSpecialization,
    string? SeeAlso,
    IReadOnlyList<string> Teachers,
    string Description);

/// <summary>
/// Parses the game's own "help &lt;name&gt;" output (captured verbatim by
/// <c>AbilityMappingCoordinator</c>/"/mapuj") into structured fields. A single response can bundle
/// several related entries separated by a "====...====" divider (e.g. "help axe" returns both AXE
/// and AXE MASTERY, and "help stun" returns three unrelated entries that merely share a prefix) —
/// <see cref="Parse"/> picks the block whose own "Nazwa:" matches the name that was actually
/// searched for. Captures can also carry unrelated noise from other game activity that happened to
/// arrive during the capture window (e.g. a stray "Zapamietales czar '...'." line) — the line-based
/// header parsing here tolerates that by simply not recognizing it as one of the known field
/// labels, so it falls into the free-text description instead of corrupting a field.
/// </summary>
public static partial class AbilityHelpParser
{
    /// <summary>Recognized header labels, exactly as the game prints them (this MUD's output is
    /// diacritic-stripped ASCII throughout, so no Polish-letter variants are needed) — in the order
    /// checked, though matching is by exact label rather than position.</summary>
    private static readonly string[] FieldLabels =
    [
        "Nazwa",
        "Typ",
        "Dostepne dla klas",
        "Specjalizacja wedrowca",
        "Alignment",
        "Cel",
        "Skladnia",
        "Polski odpowiednik",
        "Szkola",
        "Specjalizacja maga",
        "Zobacz tez",
        "Nauczyciele",
    ];

    public static ParsedAbilityHelp? Parse(string abilityName, string rawHelpText)
    {
        if (string.IsNullOrWhiteSpace(rawHelpText))
        {
            return null;
        }

        var blocks = SplitIntoBlocks(rawHelpText)
            .Select(ParseBlock)
            .Where(block => block is not null)
            .Select(block => block!)
            .ToArray();

        if (blocks.Length == 0)
        {
            return null;
        }

        return blocks.FirstOrDefault(block =>
            string.Equals(block.Name, abilityName, StringComparison.OrdinalIgnoreCase)) ?? blocks[0];
    }

    private static IEnumerable<IReadOnlyList<string>> SplitIntoBlocks(string rawHelpText)
    {
        var lines = rawHelpText.Replace("\r\n", "\n").Split('\n');
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (SeparatorLineRegex().IsMatch(line.Trim()))
            {
                if (current.Count > 0)
                {
                    yield return current;
                    current = [];
                }

                continue;
            }

            current.Add(line);
        }

        if (current.Count > 0)
        {
            yield return current;
        }
    }

    private static ParsedAbilityHelp? ParseBlock(IReadOnlyList<string> lines)
    {
        var fields = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
        string? currentLabel = null;
        var headerDone = false;
        var description = new StringBuilder();

        foreach (var line in lines)
        {
            if (!headerDone)
            {
                var label = MatchLabel(line, out var value);
                if (label is not null)
                {
                    currentLabel = label;
                    fields[label] = new StringBuilder(value);
                    continue;
                }

                if (currentLabel is not null && line.Length > 0 && char.IsWhiteSpace(line[0]))
                {
                    // Continuation of the previous field's (possibly multi-line) value. The game
                    // wraps comma-separated lists (Nauczyciele, Dostepne dla klas) at whatever
                    // column the line hits, not necessarily right after a comma — so the wrap
                    // point itself never carries one. Joining with ", " (not just a space)
                    // reconstructs a single valid comma list regardless of where it wrapped.
                    fields[currentLabel].Append(", ").Append(line.Trim());
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (fields.ContainsKey("Nazwa"))
                    {
                        headerDone = true;
                    }

                    continue;
                }

                // An unrecognized non-blank, non-indented line before any blank line has been
                // seen — noise from unrelated game activity. Skip it rather than mis-attributing
                // it to whatever field came before.
                continue;
            }

            description.AppendLine(line);
        }

        if (!fields.TryGetValue("Nazwa", out var nameBuilder) || nameBuilder.Length == 0)
        {
            return null;
        }

        string? Get(string label) => fields.TryGetValue(label, out var value)
            ? value.ToString().Trim()
            : null;

        return new ParsedAbilityHelp(
            Name: nameBuilder.ToString().Trim(),
            Type: Get("Typ"),
            AvailableForClasses: ParseClassLevels(Get("Dostepne dla klas")),
            WandererSpecialization: Get("Specjalizacja wedrowca"),
            Alignment: Get("Alignment"),
            Target: Get("Cel"),
            Syntax: Get("Skladnia"),
            PolishEquivalent: Get("Polski odpowiednik"),
            School: Get("Szkola"),
            MageSpecialization: Get("Specjalizacja maga"),
            SeeAlso: Get("Zobacz tez"),
            Teachers: SplitCommaList(Get("Nauczyciele")),
            Description: TrimBlankEdges(description.ToString()));
    }

    private static string? MatchLabel(string line, out string value)
    {
        value = string.Empty;
        if (line.Length == 0 || char.IsWhiteSpace(line[0]))
        {
            return null;
        }

        foreach (var label in FieldLabels)
        {
            var prefix = label + ":";
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                value = line[prefix.Length..].Trim();
                return label;
            }
        }

        return null;
    }

    /// <summary>"Wojownik (1 lvl), Paladyn (1 lvl), ..." → one entry per class. Silently yields
    /// nothing for values like "brak" (no class currently has access).</summary>
    private static IReadOnlyList<ClassLevelRequirement> ParseClassLevels(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return ClassLevelRegex().Matches(value)
            .Select(match => new ClassLevelRequirement(
                match.Groups["class"].Value.Trim(),
                int.Parse(match.Groups["level"].Value)))
            .ToArray();
    }

    /// <summary>Standalone (bracket-less) teacher entries that occasionally close out a
    /// "Nauczyciele" list, e.g. "... [55887] widmo podroznika, Skillbook.".</summary>
    private static readonly string[] StandaloneTeacherLabels = ["Skillbook", "Spellbook"];

    /// <summary>Teacher entries are normally "[id] name", comma-separated; but a name/title itself
    /// can contain a comma (e.g. "[16603] Sarvin, syn Tankarteza", "[3601] Dae'raira, Roza
    /// Pustyni") which a naive comma split would wrongly turn into two teachers. A fragment that
    /// doesn't start a new bracketed id (and isn't one of the few bracket-less standalone entries
    /// like "Skillbook") is instead folded back into the teacher before it.</summary>
    private static IReadOnlyList<string> SplitCommaList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var rawParts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.TrimEnd('.'))
            .Where(entry => entry.Length > 0);

        var result = new List<string>();
        foreach (var part in rawParts)
        {
            if (result.Count > 0 && !part.StartsWith('[') && !StandaloneTeacherLabels.Contains(part))
            {
                result[^1] = $"{result[^1]}, {part}";
            }
            else
            {
                result.Add(part);
            }
        }

        return result;
    }

    private static string TrimBlankEdges(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        while (lines.Count > 0 && lines[0].Trim().Length == 0)
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && lines[^1].Trim().Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"^=+$")]
    private static partial Regex SeparatorLineRegex();

    [GeneratedRegex(@"(?<class>[^,()]+?)\s*\((?<level>\d+)\s*lvl\)")]
    private static partial Regex ClassLevelRegex();
}
