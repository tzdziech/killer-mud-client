namespace MudClient.App.Services;

/// <summary>How a single spell relates to what this character has reported via "spell"/"spell
/// all" — see <see cref="SpellKnowledgeClassifier.Classify"/>.</summary>
public enum SpellKnowledgeState
{
    /// <summary>No "spell"/"spell all" data collected yet for this character — nothing can be
    /// said about the spell either way.</summary>
    Unknown,

    /// <summary>Reported with a non-blank memorization count — the player already has it.</summary>
    Known,

    /// <summary>Reported with a blank count ("(  )") — learnable by this character's class but
    /// not yet obtained.</summary>
    Missing,

    /// <summary>Never appeared in this character's "spell"/"spell all" output at all, despite
    /// having collected data — inferred to be outside this character's class spell list.</summary>
    NotLearnable,
}

public static class SpellKnowledgeClassifier
{
    public static SpellKnowledgeState Classify(string spellName, IReadOnlyDictionary<string, bool>? knowledge)
    {
        if (knowledge is null || knowledge.Count == 0)
        {
            return SpellKnowledgeState.Unknown;
        }

        if (knowledge.TryGetValue(spellName, out var known))
        {
            return known ? SpellKnowledgeState.Known : SpellKnowledgeState.Missing;
        }

        return SpellKnowledgeState.NotLearnable;
    }
}
