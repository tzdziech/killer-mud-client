using MudClient.Core.Killeropedia;

namespace MudClient.App.Models;

/// <summary>One skill a class can learn, from the hand-curated seed list (see
/// <see cref="Services.AbilitySeedCatalog"/>) — not yet enriched with the game's own "help
/// &lt;name&gt;" text (see <see cref="AbilityCaptureEntry"/>).</summary>
/// <param name="Tags">Raw <c>[P]</c>/<c>[T]</c>/<c>[A]</c> prefix letters from the source list,
/// kept uninterpreted — their exact meaning (passive/trainable/active?) wasn't confirmed, so
/// guessing it wrong in a structured field would be worse than leaving it opaque for now.</param>
/// <param name="Note">Free-text caveat that doesn't fit any other field, e.g. "only one weapon
/// mastery may be learned."</param>
public sealed record SkillSeedEntry(
    string Name,
    string Class,
    int MinLevel,
    IReadOnlyList<string> Tags,
    string? Note = null);

/// <summary>One spell a class can learn, from the hand-curated seed list — grouped by "Krąg"
/// (circle), the game's spell-tier concept, distinct from a skill's character-level gate.</summary>
public sealed record SpellSeedEntry(string Name, string Class, int Circle);

/// <summary>"help &lt;name&gt;" captured live from the game for one seeded skill/spell — see
/// <see cref="Services.AbilityMappingCoordinator"/> (the "/mapuj &lt;class&gt;" command) and
/// <see cref="Services.AbilityCaptureStore"/>. <see cref="RawHelpText"/> is kept verbatim as the
/// archival source of truth; every other field below is parsed out of it by
/// <see cref="AbilityHelpParser"/> at capture time — null/empty when the game's response didn't
/// contain that field, or parsing didn't find a block matching this entry's own name.</summary>
public sealed class AbilityCaptureEntry
{
    public string Name { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public string RawHelpText { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; }

    public string? Type { get; set; }

    /// <summary>Every class (and level requirement) the game itself lists as able to learn this —
    /// often broader than just <see cref="Class"/>, since "help" reports it for every class at
    /// once regardless of which class's seed list triggered the capture.</summary>
    public List<ClassLevelRequirement> AvailableForClasses { get; set; } = [];

    public string? WandererSpecialization { get; set; }

    public string? Alignment { get; set; }

    public string? Target { get; set; }

    public string? Syntax { get; set; }

    public string? PolishEquivalent { get; set; }

    public string? School { get; set; }

    public string? MageSpecialization { get; set; }

    public string? SeeAlso { get; set; }

    public List<string> Teachers { get; set; } = [];

    public string? Description { get; set; }
}

public sealed class AbilityCaptureDocument
{
    public List<AbilityCaptureEntry> Entries { get; set; } = [];
}
