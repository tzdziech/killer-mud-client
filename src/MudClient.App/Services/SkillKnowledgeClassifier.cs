namespace MudClient.App.Services;

/// <summary>How a single teacher-offered skill relates to what this character has reported via
/// the "skill" command — see <see cref="SkillKnowledgeClassifier.Classify"/>.</summary>
public enum SkillKnowledgeState
{
    /// <summary>No "skill" data collected yet for this character — nothing can be said about the
    /// skill either way.</summary>
    Unknown,

    /// <summary>The player's current level already reaches or exceeds this teacher's range — this
    /// teacher has nothing left to offer.</summary>
    Known,

    /// <summary>The player's current level is still below this teacher's range — still worth
    /// training here.</summary>
    Learnable,

    /// <summary>Never appeared in this character's "skill" output at all, despite having
    /// collected data — inferred to be outside this character's class skill list.</summary>
    NotLearnable,
}

public static class SkillKnowledgeClassifier
{
    /// <param name="skillName">The skill this teacher offers.</param>
    /// <param name="teacherMax">This teacher's upper training bound for the skill (their
    /// <c>TeacherSkillEntry.Max</c>), or null when unbounded.</param>
    /// <param name="knowledge">This character's skill name -&gt; current level map.</param>
    public static SkillKnowledgeState Classify(
        string skillName, int? teacherMax, IReadOnlyDictionary<string, int>? knowledge)
    {
        if (knowledge is null || knowledge.Count == 0)
        {
            return SkillKnowledgeState.Unknown;
        }

        if (!knowledge.TryGetValue(skillName, out var current))
        {
            return SkillKnowledgeState.NotLearnable;
        }

        var effectiveMax = teacherMax ?? int.MaxValue;
        return current >= effectiveMax ? SkillKnowledgeState.Known : SkillKnowledgeState.Learnable;
    }
}
