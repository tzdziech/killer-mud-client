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

/// <summary>Raw "help &lt;name&gt;" text captured live from the game for one seeded skill/spell —
/// see <see cref="Services.AbilityMappingCoordinator"/> (the "/mapuj &lt;class&gt;" command) and
/// <see cref="Services.AbilityCaptureStore"/>. Deliberately just a text blob for now: structured
/// fields (alignment/school/teacher/Wędrowiec specialization) would need to be parsed out of real
/// captured text, which doesn't exist yet.</summary>
public sealed class AbilityCaptureEntry
{
    public string Name { get; set; } = string.Empty;

    public string Class { get; set; } = string.Empty;

    public string RawHelpText { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; }
}

public sealed class AbilityCaptureDocument
{
    public List<AbilityCaptureEntry> Entries { get; set; } = [];
}
