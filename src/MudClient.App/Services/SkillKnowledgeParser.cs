using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.App.Services;

/// <summary>
/// Extracts every skill row from a "skill" command's output chunk — e.g.
/// "[WW]  axe                 10   3 + 0" yields all three numeric columns. Used to build up a
/// persistent picture of the player's whole class skill list — see
/// <see cref="Models.ProfileSkillEntry"/> — so the map can color-code teacher tooltips.
/// </summary>
public static class SkillKnowledgeParser
{
    // "[WW]  <name, one or more words>  <learnable>  <current> + <bonus>" — the name is separated
    // from its numbers by 2+ spaces (the table's own column padding), which is what lets a
    // multi-word name like "twohanded weapon" be captured without swallowing the numbers after it.
    private static readonly Regex SkillRowPattern = new(
        @"\[WW\]\s+(?<name>\S(?:.*?\S)?)\s{2,}(?<learnable>\d+)\s+(?<current>\d+)\s*\+\s*(?<bonus>\d+)",
        RegexOptions.Compiled);

    private static readonly Regex LevelHeaderPattern = new(
        @"Poziom\s+(?<level>\d+):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<(string Name, int LearnableFromTeachers, int Current, int ItemBonus, int? Level)> Parse(string chunk)
    {
        var plain = AnsiText.StripAnsi(chunk);
        if (!plain.Contains("[WW]", StringComparison.Ordinal))
        {
            return [];
        }

        var results = new List<(string Name, int LearnableFromTeachers, int Current, int ItemBonus, int? Level)>();
        int? level = null;
        foreach (var line in plain.Split('\n'))
        {
            var header = LevelHeaderPattern.Match(line);
            if (header.Success)
            {
                level = int.Parse(header.Groups["level"].Value);
            }

            foreach (Match match in SkillRowPattern.Matches(line))
            {
                var name = match.Groups["name"].Value.Trim();
                results.Add((
                    name,
                    int.Parse(match.Groups["learnable"].Value),
                    int.Parse(match.Groups["current"].Value),
                    int.Parse(match.Groups["bonus"].Value),
                    level));
            }
        }

        return results;
    }
}
