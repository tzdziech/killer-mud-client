using System.Text.RegularExpressions;
using MudClient.Core.Gmcp;

namespace MudClient.Core.Automation;

/// <summary>Detects the current GMCP group leader's "snaps fingers" emote line — the trigger for
/// auto-recast (see AutomationPanelView's "Drużyna" tab).</summary>
public static class LeaderSnapPolicy
{
    private static readonly Regex SnapRegex = new(
        "^(?<name>[A-Za-z]+) pstryka palcami\\.?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>True if <paramref name="line"/> is the snap-fingers emote from whoever is
    /// currently the GMCP group's leader — not just any member (mirrors
    /// <see cref="GroupOrderPolicy.TryGetCommand"/>'s shape), and not <paramref name="selfName"/>,
    /// since the local player being the leader shouldn't trigger their own recast off their own
    /// emote.</summary>
    public static bool IsLeaderSnap(string line, string? selfName, CharacterGroupUpdate? group)
    {
        if (group is null)
        {
            return false;
        }

        var match = SnapRegex.Match(line);
        if (!match.Success)
        {
            return false;
        }

        var name = match.Groups["name"].Value;
        if (string.Equals(name, selfName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return group.Members.Any(member =>
            member.IsLeader && string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
