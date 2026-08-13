using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>One class's full seed list — the skills/spells "/mapuj &lt;class&gt;" loops "help" over
/// (see <see cref="AbilityMappingCoordinator"/>).</summary>
public sealed record ClassAbilitySeed(
    string Class, IReadOnlyList<SkillSeedEntry> Skills, IReadOnlyList<SpellSeedEntry> Spells)
{
    /// <summary>All skill and spell names for this class, skills first then spells, in seed
    /// order — what a "/mapuj" run sends "help &lt;name&gt;" for, one at a time.</summary>
    public IReadOnlyList<string> AllNames =>
        Skills.Select(skill => skill.Name).Concat(Spells.Select(spell => spell.Name)).ToArray();
}

/// <summary>
/// Hand-curated per-class skill/spell lists — the starting point for the "database of everything
/// obtainable" the user asked for. Source: the user's own in-game "skills"/"spells" listings
/// pasted directly (Polish diacritics were mojibake'd in the paste and reconstructed by hand, the
/// same way the Killeropedia tricks wiki page was earlier). Currently seeded: Paladyn only — more
/// classes are added the same way as they're supplied.
///
/// This only carries what the class's own listing already states (name, level/circle gate, the
/// raw [P]/[T]/[A] tags). Everything else (alignment, school, Teacher, Wędrowiec specialization
/// notes) comes later from <see cref="AbilityCaptureStore"/>, once "/mapuj" has actually captured
/// real "help &lt;name&gt;" text to parse it out of.
/// </summary>
public static class AbilitySeedCatalog
{
    private const string Paladyn = "Paladyn";

    private static SkillSeedEntry Sk(string name, int minLevel, string[] tags, string? note = null) =>
        new(name, Paladyn, minLevel, tags, note);

    private static SpellSeedEntry Sp(string name, int circle) => new(name, Paladyn, circle);

    /// <summary>"Można nauczyć się tylko jednej umiejętności mistrzostwa broni; wymaga odpowiedniej
    /// podstawowej umiejętności broni na odpowiednio wysokim poziomie." — the level-20 "UWAGA!"
    /// note in the source listing, shared by the three weapon-mastery choices.</summary>
    private const string OneWeaponMasteryNote =
        "Można nauczyć się tylko jednej umiejętności mistrzostwa broni; wymaga odpowiedniej " +
        "podstawowej umiejętności broni na odpowiednio wysokim poziomie.";

    private static readonly IReadOnlyList<SkillSeedEntry> PaladynSkills =
    [
        // Poziom 1 — "podstawowe umiejętności broni" ([P] category); only the sub-types marked
        // [T] in the source carry that tag here, axe/polearm are listed with none.
        Sk("axe", 1, []),
        Sk("dagger", 1, ["T"]),
        Sk("flail", 1, ["T"]),
        Sk("mace", 1, ["T"]),
        Sk("polearm", 1, []),
        Sk("spear", 1, ["T"]),
        Sk("sword", 1, ["T"]),
        Sk("short-sword", 1, ["T"]),
        // Poziom 1 — "umiejętności zbroi" ([P] category), no sub-type tags in the source.
        Sk("light armor", 1, []),
        Sk("medium armor", 1, []),
        Sk("heavy armor", 1, []),
        Sk("riding", 1, ["P"]),
        Sk("lay", 1, ["A"]),
        Sk("twohanded weapon", 1, ["P", "T"]),
        Sk("bandage", 1, ["A"]),
        Sk("herbs knowledge", 1, ["A"]),
        Sk("trapper", 1, ["A"]),
        Sk("holy prayer", 1, ["A"]),

        Sk("turn undead", 3, ["A"]),

        Sk("kick", 4, ["A", "T"]),
        Sk("rescue", 4, ["A"]),
        Sk("smite evil", 4, ["A", "T"]),
        Sk("mining", 4, ["A"]),

        Sk("shield block", 8, ["P"]),
        Sk("bash", 8, ["A", "T"]),

        Sk("stun", 10, ["A", "T"]),

        Sk("twohanded weapon style", 11, ["P"]),

        Sk("disarm", 12, ["A"]),
        Sk("parry", 12, ["A", "T"]),
        Sk("track", 12, ["A"]),
        Sk("dualwield style", 12, ["P", "T"]),
        Sk("target", 12, ["A"]),

        Sk("meditation", 13, ["A"]),

        // Poziom 20 — "umiejętności mistrzostwa broni": only one of these three may be learned.
        Sk("mace mastery", 20, ["P", "T"], OneWeaponMasteryNote),
        Sk("flail mastery", 20, ["P"], OneWeaponMasteryNote),
        Sk("sword mastery", 20, ["P"], OneWeaponMasteryNote),

        // Poziom 31 — "umiejętności mistrzowskie".
        Sk("shield mastery", 31, ["P"]),
        Sk("parry mastery", 31, ["A"]),
        Sk("holy mastery", 31, ["A"]),
        Sk("turn mastery", 31, ["A"]),
    ];

    private static readonly IReadOnlyList<SpellSeedEntry> PaladynSpells =
    [
        // Krąg 1
        Sp("aura of protection", 1),
        Sp("bless", 1),
        Sp("create food", 1),
        Sp("create symbol", 1),
        Sp("create water", 1),
        Sp("cure disease", 1),
        Sp("cure light", 1),
        Sp("detect evil", 1),
        Sp("detect undead", 1),
        Sp("light", 1),
        Sp("lore undead", 1),
        Sp("protection evil", 1),

        // Krąg 2
        Sp("aid", 2),
        Sp("aura of battle lust", 2),
        Sp("aura of endurance", 2),
        Sp("aura of precision", 2),
        Sp("bull strength", 2),
        Sp("chant", 2),
        Sp("cure moderate", 2),
        Sp("divine favor", 2),
        Sp("eagle splendor", 2),
        Sp("lesser cure poison", 2),
        Sp("luck", 2),
        Sp("owl wisdom", 2),
        Sp("prayer", 2),
        Sp("spiritual armor", 2),
        Sp("spiritual weapon", 2),
        Sp("undead invisibility", 2),

        // Krąg 3
        Sp("aura of improved healing", 3),
        Sp("aura of vigor", 3),
        Sp("cure blindness", 3),
        Sp("cure poison", 3),
        Sp("cure serious", 3),
        Sp("divine shield", 3),
        Sp("hold evil", 3),
        Sp("remove curse", 3),
        Sp("remove fear", 3),

        // Krąg 4
        Sp("brave cloak", 4),
        Sp("calm", 4),
        Sp("dismiss undead", 4),
        Sp("dispel evil", 4),
        Sp("greater cure poison", 4),
        Sp("holy weapons", 4),
        Sp("lesser restoration", 4),

        // Krąg 5
        Sp("sanctuary", 5),
        Sp("divine power", 5),
        Sp("consecrate", 5),
    ];

    private static readonly IReadOnlyDictionary<string, ClassAbilitySeed> ByClass =
        new[] { new ClassAbilitySeed(Paladyn, PaladynSkills, PaladynSpells) }
            .ToDictionary(seed => seed.Class, seed => seed, StringComparer.OrdinalIgnoreCase);

    /// <summary>All seeded classes' names, for a usage message when "/mapuj" is given an
    /// unrecognized one.</summary>
    public static IReadOnlyList<string> KnownClasses => ByClass.Keys.ToArray();

    /// <summary>Case-insensitive lookup — null when no seed data exists yet for that class.</summary>
    public static ClassAbilitySeed? Find(string className) =>
        !string.IsNullOrWhiteSpace(className) && ByClass.TryGetValue(className.Trim(), out var seed)
            ? seed
            : null;
}
