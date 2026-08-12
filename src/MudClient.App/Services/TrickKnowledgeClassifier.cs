using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>
/// Whether the local character currently meets a trick's skill-percent requirement(s) — e.g.
/// "vertical kick" needs "kick na min. 85%", checked against the same skill knowledge (from the
/// "skill" command) that colors the map's skill lines — see <see cref="MudClient.Core.Automation"/>-
/// adjacent <c>SkillKnowledgeClassifier</c>. A trick with no known requirements (not yet
/// transcribed from the wiki) never reports as met, since there's nothing to check.
/// </summary>
public static class TrickKnowledgeClassifier
{
    public static bool MeetsRequirements(TeacherTrickEntry trick, IReadOnlyDictionary<string, int> skillKnowledge)
    {
        if (trick.Requirements is not { Count: > 0 } requirements)
        {
            return false;
        }

        bool Meets(TrickRequirement requirement) =>
            skillKnowledge.TryGetValue(requirement.SkillName, out var current) && current >= requirement.MinPercent;

        return trick.RequiresAllRequirements ? requirements.All(Meets) : requirements.Any(Meets);
    }
}
