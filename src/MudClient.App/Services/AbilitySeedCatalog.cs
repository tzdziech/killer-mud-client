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
/// same way the Killeropedia tricks wiki page was earlier). Seeded so far: Paladyn, Czarny
/// Rycerz, Złodziej, Druid, Nomad, Kleryk, Wojownik, Barbarzyńca, Mag — more classes/schools are
/// added the same way as they're supplied (e.g. Mag's spells here are only its "Odrzucanie"
/// school so far; the other schools listed in <c>AbilitySkillTreeCanvas.BranchColors</c> arrive
/// as separate supplied lists later).
///
/// This only carries what the class's own listing already states (name, level/circle gate, the
/// raw [P]/[T]/[A] tags — kept exactly as given, including apparent oddities like Druid listing
/// "light armor" twice, since second-guessing the source risks losing real signal). Skill/spell
/// *names* are never "corrected" even when they look like a typo (e.g. Mag's "lighting mastery"),
/// since "/mapuj" sends them verbatim as "help &lt;name&gt;" and a fixed spelling just wouldn't
/// match what the game actually knows. Everything else (alignment, school, Teacher, Wędrowiec
/// specialization notes) comes later from <see cref="AbilityCaptureStore"/>, once "/mapuj" has
/// actually captured real "help &lt;name&gt;" text to parse it out of.
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

    // ========================================================================
    // Czarny Rycerz (Black Knight)
    // ========================================================================

    private const string CzarnyRycerz = "Czarny Rycerz";

    private static readonly IReadOnlyList<SkillSeedEntry> CzarnyRycerzSkills = BuildCzarnyRycerzSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildCzarnyRycerzSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, CzarnyRycerz, minLevel, tags, note);

        return
        [
            // Poziom 1 — "podstawowe umiejętności broni".
            S("axe", 1, []),
            S("dagger", 1, ["T"]),
            S("flail", 1, ["T"]),
            S("mace", 1, []),
            S("polearm", 1, []),
            S("spear", 1, ["T"]),
            S("sword", 1, ["T"]),
            S("whip", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            // Poziom 1 — "umiejętności zbroi".
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("heavy armor", 1, []),
            S("kick", 1, ["A", "T"]),
            S("riding", 1, ["P"]),
            S("twohanded weapon", 1, ["P", "T"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),
            S("torment", 1, ["A"]),
            S("undead resemblance", 1, ["P"]),

            S("call avatar", 2, ["A"]),

            S("control undead", 3, ["A"]),

            S("smite good", 4, ["A", "T"]),
            S("mining", 4, ["A"]),

            S("bash", 5, ["A", "T"]),
            S("devour", 5, ["A"]),
            S("hustle", 5, ["A"]),

            S("twohanded weapon style", 7, ["P"]),

            S("track", 8, ["A"]),
            S("cleave", 8, ["A", "T"]),

            S("demon aura", 9, ["A"]),

            S("damn weapon", 11, ["A"]),
            S("damn armor", 11, ["A"]),

            S("parry", 13, ["A", "T"]),

            S("disarm", 14, ["A", "T"]),
            S("envenom", 14, ["A"]),

            S("vertical slash", 15, ["A"]),

            S("overwhelming strike", 19, ["A", "T"]),

            // Poziom 20 — "umiejętności mistrzostwa broni": only one of these may be learned.
            S("sword mastery", 20, ["P"], OneWeaponMasteryNote),
            S("axe mastery", 20, ["P"], OneWeaponMasteryNote),
            S("flail mastery", 20, ["P", "T"], OneWeaponMasteryNote),
            S("polearm mastery", 20, ["P"], OneWeaponMasteryNote),

            // Poziom 31 — "umiejętności mistrzowskie".
            S("envenom mastery", 31, ["A"]),
            S("unholy mastery", 31, ["A"]),
            S("avatar mastery", 31, ["A"]),
        ];
    }

    // ========================================================================
    // Złodziej (Thief)
    // ========================================================================

    private const string Zlodziej = "Złodziej";

    private static readonly IReadOnlyList<SkillSeedEntry> ZlodziejSkills = BuildZlodziejSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildZlodziejSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Zlodziej, minLevel, tags, note);

        return
        [
            S("dagger", 1, ["T"]),
            S("flail", 1, []),
            S("mace", 1, []),
            S("sword", 1, ["T"]),
            S("whip", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("backstab", 1, ["A"]),
            S("riding", 1, ["P"]),
            S("hide", 1, ["A"]),
            S("peek", 1, ["A"]),
            S("pick lock", 1, ["A"]),
            S("sneak", 1, ["A"]),
            S("steal", 1, ["A"]),
            S("bandage", 1, ["A"]),
            S("detect traps", 1, ["A"]),
            S("disarm traps", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),

            S("kick", 4, ["A"]),
            S("mining", 4, ["A"]),

            S("trip", 8, ["A", "T"]),

            S("dodge", 10, ["A", "T"]),

            S("envenom", 12, ["A"]),

            S("circle", 15, ["A"]),

            S("dagger mastery", 18, ["P"]),

            S("short-sword mastery", 20, ["P"]),

            S("backstab mastery", 31, ["A"]),
            S("steal mastery", 31, ["A"]),
            S("envenom mastery", 31, ["A"]),
        ];
    }

    // ========================================================================
    // Druid / Nomad — share the same nature spell list (see NatureSpellSeeds below);
    // Nomad additionally has "sand storm" at Krąg 8.
    // ========================================================================

    private const string Druid = "Druid";
    private const string Nomad = "Nomad";

    private static readonly IReadOnlyList<SkillSeedEntry> DruidSkills = BuildDruidSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildDruidSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Druid, minLevel, tags, note);

        return
        [
            S("dagger", 1, ["T"]),
            S("flail", 1, ["T"]),
            S("mace", 1, []),
            S("staff", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("riding", 1, ["P"]),
            S("twohanded weapon", 1, ["P", "T"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),
            // "light armor" really is listed twice in the source — once inside "umiejętności
            // zbroi" above, once as its own standalone [P] bullet. Kept as given.
            S("light armor", 1, ["P"]),

            S("mining", 4, ["A"]),

            S("shield block", 10, ["P"]),

            S("nature ally I mastery", 31, ["A"]),
            S("nature ally II mastery", 31, ["A"]),
            S("nature ally III mastery", 31, ["A"]),
            S("nature ally IV mastery", 31, ["A"]),
        ];
    }

    /// <summary>Krąg-tagged nature spell names shared verbatim by Druid and Nomad (see the class
    /// header comment above) — kept as one list materialized twice with each class's own name, so
    /// the ~90-entry list isn't transcribed (and risks drifting) twice over.</summary>
    private static readonly (string Name, int Circle)[] NatureSpellSeeds =
    [
        // Krąg 1
        ("burst of flame", 1), ("cure animal", 1), ("cure plant", 1), ("dismiss animal", 1),
        ("dismiss insect", 1), ("dismiss plant", 1), ("endure acid", 1), ("endure cold", 1),
        ("endure fire", 1), ("endure lightning", 1), ("faerie fire", 1), ("firefly swarm", 1),
        ("frost rift", 1), ("nature ally I", 1), ("purify food", 1), ("shillelagh", 1),
        ("spray of thorns", 1), ("summon animal", 1),

        // Krąg 2
        ("alicorn lance", 2), ("animal invisibility", 2), ("bear endurance", 2), ("burst of fire", 2),
        ("cat grace", 2), ("create food", 2), ("create water", 2), ("flare", 2), ("goodbarry", 2),
        ("hold animal", 2), ("immolate", 2), ("luck", 2), ("owl wisdom", 2), ("produce fire", 2),
        ("refresh", 2), ("sense fatigue", 2), ("slow rot", 2), ("waterwalk", 2),

        // Krąg 3
        ("bark guardian", 3), ("bark skin", 3), ("cause light", 3), ("charm animal", 3), ("corrode", 3),
        ("create spring", 3), ("create tree", 3), ("cure light", 3), ("entangle", 3), ("flame blade", 3),
        ("float", 3), ("hold plant", 3), ("lava bolt", 3), ("magic fang", 3), ("nature ally II", 3),
        ("poison", 3), ("ring of vanion", 3), ("slow poison", 3), ("sunscorch", 3),

        // Krąg 4
        ("beast claws", 4), ("call lightning", 4), ("chill metal", 4), ("control weather", 4),
        ("create healing water", 4), ("earthquake", 4), ("heat metal", 4), ("hellfire", 4),
        ("ice bolt", 4), ("reinvigore animal", 4), ("reinvigore plant", 4), ("resist acid", 4),
        ("resist cold", 4), ("resist fire", 4), ("resist lightning", 4), ("silence", 4),
        ("water breathing", 4), ("wind shield", 4),

        // Krąg 5
        ("animal rage", 5), ("cause moderate", 5), ("cure moderate", 5), ("dismiss monster", 5),
        ("dismiss outsider", 5), ("flamestrike", 5), ("hold person", 5), ("hold undead", 5),
        ("liveoak", 5), ("nature ally III", 5), ("neutralize poison", 5), ("smashing wave", 5),
        ("storm shell", 5), ("weaken", 5), ("wind charger", 5),

        // Krąg 6
        ("blade barrier", 6), ("cause serious", 6), ("cure serious", 6), ("fly", 6),
        ("freezing rain", 6), ("heal animal", 6), ("heal plant", 6), ("nature curse", 6),
        ("stone skin", 6),

        // Krąg 7
        ("cause critical", 7), ("circle of vanion", 7), ("cure critical", 7), ("mass luck", 7),
        ("nature ally IV", 7), ("resist poison", 7), ("shield of nature", 7), ("wildthorn", 7),

        // Krąg 8
        ("healing ring", 8), ("mass refresh", 8),
    ];

    private static readonly IReadOnlyList<SpellSeedEntry> DruidSpells =
        NatureSpellSeeds.Select(seed => new SpellSeedEntry(seed.Name, Druid, seed.Circle)).ToArray();

    private static readonly IReadOnlyList<SkillSeedEntry> NomadSkills = BuildNomadSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildNomadSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Nomad, minLevel, tags, note);

        return
        [
            S("dagger", 1, ["T"]),
            S("spear", 1, []),
            S("sword", 1, ["T"]),
            S("whip", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("riding", 1, ["P"]),
            S("peek", 1, ["A"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),

            S("dualwield style", 3, ["P", "T"]),

            S("steal", 4, ["A"]),
            S("mining", 4, ["A"]),

            S("kick", 6, ["A", "T"]),
            S("hide", 6, ["A"]),

            S("sneak", 8, ["A"]),
            S("sharpen", 8, ["A"]),
            S("recuperate", 8, ["A"]),
            S("dodge", 8, ["A", "T"]),

            S("hustle", 9, ["A"]),
            S("bladedance", 9, ["A"]),

            S("slam", 10, ["A"]),

            S("first aid", 11, ["A"]),
            S("call avatar", 11, ["A"]),

            S("trip", 12, ["A", "T"]),
            S("target", 12, ["A"]),

            S("parry", 16, ["A", "T"]),
            S("envenom", 16, ["A"]),
            S("damage reduction", 16, ["A"]),

            S("disarm", 18, ["A"]),

            S("pick lock", 20, ["A"]),
            S("weapon mastery", 20, ["P"]),

            S("whirlwind", 25, ["A"]),
            S("bladefury", 25, ["A"]),

            S("parry mastery", 31, ["A"]),
            S("avatar mastery", 31, ["A"]),
            S("whirlwind mastery", 31, ["A"]),
            S("hustle mastery", 31, ["A"]),
        ];
    }

    private static readonly IReadOnlyList<SpellSeedEntry> NomadSpells =
        NatureSpellSeeds.Select(seed => new SpellSeedEntry(seed.Name, Nomad, seed.Circle))
            .Append(new SpellSeedEntry("sand storm", Nomad, 8))
            .ToArray();

    // ========================================================================
    // Kleryk (Cleric)
    // ========================================================================

    private const string Kleryk = "Kleryk";

    private static readonly IReadOnlyList<SkillSeedEntry> KlerykSkills = BuildKlerykSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildKlerykSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Kleryk, minLevel, tags, note);

        return
        [
            S("dagger", 1, []),
            S("flail", 1, ["T"]),
            S("mace", 1, []),
            S("staff", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("riding", 1, ["P"]),
            S("meditation", 1, ["A"]),
            S("first aid", 1, ["A"]),
            S("twohanded weapon", 1, ["P", "T"]),
            S("bandage", 1, ["A"]),
            S("turn undead", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),

            S("mining", 4, ["A"]),

            S("shield block", 10, ["P"]),

            S("stun", 12, ["A", "T"]),

            S("healing touch", 18, ["A"]),

            S("turn mastery", 31, ["A"]),
            S("alacrity mastery", 31, ["A"]),
        ];
    }

    private static readonly IReadOnlyList<SpellSeedEntry> KlerykSpells =
    [
        // Krąg 1
        new("bane", Kleryk, 1), new("bless", Kleryk, 1), new("cause light", Kleryk, 1),
        new("change liquid", Kleryk, 1), new("command", Kleryk, 1), new("comprehend languages", Kleryk, 1),
        new("create food", Kleryk, 1), new("create symbol", Kleryk, 1), new("create water", Kleryk, 1),
        new("cure light", Kleryk, 1), new("detect undead", Kleryk, 1), new("dismiss undead", Kleryk, 1),
        new("lore undead", Kleryk, 1), new("luck", Kleryk, 1), new("remove fear", Kleryk, 1),
        new("spiritual light", Kleryk, 1), new("transmute staff", Kleryk, 1),

        // Krąg 2
        new("aid", Kleryk, 2), new("blindness", Kleryk, 2), new("bull strength", Kleryk, 2),
        new("cause moderate", Kleryk, 2), new("cure blindness", Kleryk, 2), new("cure moderate", Kleryk, 2),
        new("curse", Kleryk, 2), new("detect evil", Kleryk, 2), new("detect good", Kleryk, 2),
        new("detect poison", Kleryk, 2), new("divine favor", Kleryk, 2), new("owl wisdom", Kleryk, 2),
        new("poison", Kleryk, 2), new("protection evil", Kleryk, 2), new("protection good", Kleryk, 2),
        new("ray of light", Kleryk, 2), new("silence", Kleryk, 2), new("spiritual armor", Kleryk, 2),
        new("spiritual hammer", Kleryk, 2), new("spiritual weapon", Kleryk, 2),

        // Krąg 3
        new("cause serious", Kleryk, 3), new("chant", Kleryk, 3), new("consecrate", Kleryk, 3),
        new("cure disease", Kleryk, 3), new("cure serious", Kleryk, 3), new("desecrate", Kleryk, 3),
        new("detect invis", Kleryk, 3), new("endure acid", Kleryk, 3), new("endure cold", Kleryk, 3),
        new("endure fire", Kleryk, 3), new("endure lightning", Kleryk, 3), new("fear", Kleryk, 3),
        new("holy bolt", Kleryk, 3), new("know alignment", Kleryk, 3), new("lesser cure poison", Kleryk, 3),
        new("prayer", Kleryk, 3), new("pyrotechnics", Kleryk, 3), new("remove curse", Kleryk, 3),
        new("sense life", Kleryk, 3), new("undead invisibility", Kleryk, 3),

        // Krąg 4
        new("animate dead", Kleryk, 4), new("brave cloak", Kleryk, 4), new("calm", Kleryk, 4),
        new("cause critical", Kleryk, 4), new("confusion", Kleryk, 4), new("create healing water", Kleryk, 4),
        new("cure critical", Kleryk, 4), new("detect hidden", Kleryk, 4), new("dispel magic", Kleryk, 4),
        new("free action", Kleryk, 4), new("hold undead", Kleryk, 4), new("lesser restoration", Kleryk, 4),
        new("remove paralysis", Kleryk, 4), new("resist acid", Kleryk, 4), new("resist cold", Kleryk, 4),
        new("resist fire", Kleryk, 4), new("resist lightning", Kleryk, 4), new("resist negative", Kleryk, 4),
        new("weaken", Kleryk, 4),

        // Krąg 5
        new("champion strength", Kleryk, 5), new("chill metal", Kleryk, 5), new("cure poison", Kleryk, 5),
        new("dispel evil", Kleryk, 5), new("dispel good", Kleryk, 5), new("harm", Kleryk, 5),
        new("heal", Kleryk, 5), new("heat metal", Kleryk, 5), new("hold person", Kleryk, 5),
        new("mass aid", Kleryk, 5), new("mass cure light", Kleryk, 5), new("restoration", Kleryk, 5),

        // Krąg 6
        new("deathward", Kleryk, 6), new("energy shield", Kleryk, 6), new("life transfer", Kleryk, 6),
        new("mass cure moderate", Kleryk, 6), new("resist magic", Kleryk, 6), new("sanctuary", Kleryk, 6),

        // Krąg 7
        new("divine power", Kleryk, 7), new("greater cure poison", Kleryk, 7), new("mass bless", Kleryk, 7),
        new("mass cure serious", Kleryk, 7), new("mass luck", Kleryk, 7), new("mass protection evil", Kleryk, 7),
        new("mass protection good", Kleryk, 7),

        // Krąg 8
        new("mass cure critical", Kleryk, 8), new("mass resist negative", Kleryk, 8),
    ];

    // ========================================================================
    // Wojownik (Warrior)
    // ========================================================================

    private const string Wojownik = "Wojownik";

    private static readonly IReadOnlyList<SkillSeedEntry> WojownikSkills = BuildWojownikSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildWojownikSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Wojownik, minLevel, tags, note);

        return
        [
            S("axe", 1, []),
            S("dagger", 1, ["T"]),
            S("flail", 1, ["T"]),
            S("mace", 1, ["T"]),
            S("polearm", 1, []),
            S("staff", 1, ["T"]),
            S("spear", 1, ["T"]),
            S("sword", 1, ["T"]),
            S("whip", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("heavy armor", 1, []),
            S("twohanded weapon", 1, ["P", "T"]),
            S("bash", 1, ["A", "T"]),
            S("kick", 1, ["A", "T"]),
            S("rescue", 1, ["A"]),
            S("riding", 1, ["P"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),

            S("shield block", 4, ["P"]),
            S("mining", 4, ["A"]),

            S("track", 8, ["A"]),
            S("wardance", 8, ["A"]),
            S("target", 8, ["A"]),

            S("cleave", 9, ["A", "T"]),

            S("stun", 10, ["A", "T"]),
            S("twohanded weapon style", 10, ["P"]),

            S("disarm", 12, ["A", "T"]),
            S("parry", 12, ["A", "T"]),
            S("dualwield style", 12, ["P", "T"]),

            S("vertical slash", 14, ["A"]),

            S("overwhelming strike", 16, ["A", "T"]),

            // Poziom 20 — "umiejętności mistrzostwa broni": only one of these may be learned.
            S("dagger mastery", 20, ["P"], OneWeaponMasteryNote),
            S("mace mastery", 20, ["P", "T"], OneWeaponMasteryNote),
            S("flail mastery", 20, ["P"], OneWeaponMasteryNote),
            S("sword mastery", 20, ["P"], OneWeaponMasteryNote),
            S("axe mastery", 20, ["P"], OneWeaponMasteryNote),
            S("whip mastery", 20, ["P", "T"], OneWeaponMasteryNote),
            S("short-sword mastery", 20, ["P"], OneWeaponMasteryNote),
            S("polearm mastery", 20, ["P"], OneWeaponMasteryNote),
            S("staff mastery", 20, ["P"], OneWeaponMasteryNote),
            S("spear mastery", 20, ["P"], OneWeaponMasteryNote),

            // Poziom 31 — "umiejętności mistrzowskie".
            S("wardance mastery", 31, ["A"]),
            S("shield mastery", 31, ["P"]),
            S("parry mastery", 31, ["A"]),
            S("weapon mastery", 31, ["P"]),
        ];
    }

    // ========================================================================
    // Barbarzyńca (Barbarian)
    // ========================================================================

    private const string Barbarzynca = "Barbarzyńca";

    private static readonly IReadOnlyList<SkillSeedEntry> BarbarzyncaSkills = BuildBarbarzyncaSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildBarbarzyncaSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Barbarzynca, minLevel, tags, note);

        return
        [
            S("axe", 1, ["T"]),
            S("dagger", 1, ["T"]),
            S("flail", 1, ["T"]),
            S("mace", 1, []),
            S("polearm", 1, []),
            S("staff", 1, ["T"]),
            S("spear", 1, ["T"]),
            S("sword", 1, ["T"]),
            S("whip", 1, ["T"]),
            S("short-sword", 1, ["T"]),
            S("light armor", 1, []),
            S("medium armor", 1, []),
            S("riding", 1, ["P"]),
            S("twohanded weapon", 1, ["P", "T"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),
            S("claw-weapons", 1, ["P"]),

            S("rescue", 4, ["A"]),
            S("mighty blow", 4, ["A", "T"]),
            S("mining", 4, ["A"]),

            S("charge", 8, ["A", "T"]),
            S("sharpen", 8, ["A"]),
            S("recuperate", 8, ["A"]),

            S("track", 9, ["A"]),

            S("berserk", 10, ["A"]),
            S("lore item", 10, ["A"]),

            S("twohanded weapon style", 12, ["P"]),
            S("power strike", 12, ["A"]),
            S("damage reduction", 12, ["A"]),

            S("dualwield style", 16, ["P", "T"]),

            S("critical strike", 20, ["A", "T"]),
            S("whirlwind", 20, ["A"]),

            S("berserk mastery", 31, ["A"]),
            S("damage reduction mastery", 31, ["A"]),
            S("recuperate mastery", 31, ["A"]),
        ];
    }

    // ========================================================================
    // Mag (Mage) — skills, plus the "Odrzucanie" (Abjuration) school's spells only so far; the
    // rest of Mag's schools (Przemiany, Nekromancja, Przywołania, Zauroczenie, Inwokacje,
    // Poznanie — see AbilitySkillTreeCanvas.BranchColors) arrive as separate supplied lists.
    // ========================================================================

    private const string Mag = "Mag";

    private static readonly IReadOnlyList<SkillSeedEntry> MagSkills = BuildMagSkills();

    private static IReadOnlyList<SkillSeedEntry> BuildMagSkills()
    {
        SkillSeedEntry S(string name, int minLevel, string[] tags, string? note = null) =>
            new(name, Mag, minLevel, tags, note);

        return
        [
            S("dagger", 1, ["T"]),
            S("staff", 1, ["T"]),
            S("light armor", 1, []),
            S("twohanded weapon", 1, ["P", "T"]),
            S("riding", 1, ["P"]),
            S("bandage", 1, ["A"]),
            S("herbs knowledge", 1, ["A"]),
            S("trapper", 1, ["A"]),
            S("control of magic", 1, ["P"]),

            S("mining", 4, ["A"]),

            S("fire mastery", 31, ["P"]),
            S("cold mastery", 31, ["P"]),
            S("lighting mastery", 31, ["P"]),
            S("acid mastery", 31, ["P"]),
            S("entropy mastery", 31, ["P"]),
        ];
    }

    private static readonly IReadOnlyList<SpellSeedEntry> MagSpells =
    [
        // Krąg 1
        new("armor", Mag, 1), new("bonelace", Mag, 1), new("comprehend languages", Mag, 1),
        new("detect magic", Mag, 1), new("dismiss animal", Mag, 1), new("dismiss insect", Mag, 1),
        new("dismiss plant", Mag, 1), new("fire darts", Mag, 1), new("lore undead", Mag, 1),
        new("magic missile", Mag, 1), new("shield", Mag, 1),

        // Krąg 2
        new("bladethirst", Mag, 2), new("chill touch", Mag, 2), new("cold snap", Mag, 2),
        new("darkvision", Mag, 2), new("deafness", Mag, 2), new("defense curl", Mag, 2),
        new("detect invis", Mag, 2), new("dismiss person", Mag, 2), new("eagle splendor", Mag, 2),
        new("endure acid", Mag, 2), new("endure cold", Mag, 2), new("endure fire", Mag, 2),
        new("endure lightning", Mag, 2), new("fortitude", Mag, 2), new("identify", Mag, 2),
        new("ray of enfeeblement", Mag, 2), new("shocking grasp", Mag, 2), new("silence", Mag, 2),
        new("sleep", Mag, 2), new("weaken", Mag, 2),

        // Krąg 3
        new("dismiss monster", Mag, 3), new("dismiss outsider", Mag, 3), new("dispel magic", Mag, 3),
        new("farsight", Mag, 3), new("flame arrow", Mag, 3), new("healing sleep", Mag, 3),
        new("increase wounds", Mag, 3), new("lightning bolt", Mag, 3), new("mind fortess", Mag, 3),
        new("perfect senses", Mag, 3), new("web", Mag, 3),

        // Krąg 4
        new("acid blast", Mag, 4), new("animate dead", Mag, 4), new("detect hidden", Mag, 4),
        new("dismiss undead", Mag, 4), new("ethereal armor", Mag, 4), new("floating disc", Mag, 4),
        new("force field", Mag, 4), new("free action", Mag, 4), new("hold person", Mag, 4),
        new("mental barrier", Mag, 4), new("remove paralysis", Mag, 4), new("repayment", Mag, 4),
        new("stability", Mag, 4), new("vampiric touch", Mag, 4),

        // Krąg 5
        new("chaotic shock", Mag, 5), new("charm person", Mag, 5), new("confusion", Mag, 5),
        new("fireshield", Mag, 5), new("force bolt", Mag, 5), new("hold animal", Mag, 5),
        new("hold undead", Mag, 5), new("iceshield", Mag, 5), new("lesser magic resist", Mag, 5),
        new("minor globe of invulnerability", Mag, 5), new("raise zombie", Mag, 5), new("resist acid", Mag, 5),
        new("resist cold", Mag, 5), new("resist fire", Mag, 5), new("resist lightning", Mag, 5),
        new("resist normal weapon", Mag, 5), new("resist poison", Mag, 5),

        // Krąg 6
        new("antimagic manacles", Mag, 6), new("chain lightning", Mag, 6), new("charm monster", Mag, 6),
        new("feeblemind", Mag, 6), new("fireball", Mag, 6), new("hold monster", Mag, 6),
        new("hold plant", Mag, 6), new("light nova", Mag, 6), new("locate object", Mag, 6),
        new("protection from summon", Mag, 6), new("reflect spell I", Mag, 6), new("resist elements", Mag, 6),
        new("shadow weapon", Mag, 6), new("summon distortion", Mag, 6), new("summon", Mag, 6),

        // Krąg 7
        new("cone of cold", Mag, 7), new("energy shield", Mag, 7), new("exile", Mag, 7),
        new("globe of invulnerability", Mag, 7), new("mantle", Mag, 7), new("portal", Mag, 7),
        new("reflect spell II", Mag, 7),

        // Krąg 8
        new("brainwash", Mag, 8), new("enchant armor", Mag, 8), new("enchant weapon", Mag, 8),
        new("energy drain", Mag, 8), new("great dispel magic", Mag, 8), new("lightingshield", Mag, 8),
        new("major globe of invulnerability", Mag, 8), new("reflect spell III", Mag, 8),
        new("resist magic weapon", Mag, 8),

        // Krąg 9
        new("absolute magic protection", Mag, 9), new("deflect wounds", Mag, 9), new("nexus", Mag, 9),
        new("resist weapon", Mag, 9),
    ];

    private static readonly IReadOnlyDictionary<string, ClassAbilitySeed> ByClass = new[]
    {
        new ClassAbilitySeed(Paladyn, PaladynSkills, PaladynSpells),
        new ClassAbilitySeed(CzarnyRycerz, CzarnyRycerzSkills, []),
        new ClassAbilitySeed(Zlodziej, ZlodziejSkills, []),
        new ClassAbilitySeed(Druid, DruidSkills, DruidSpells),
        new ClassAbilitySeed(Nomad, NomadSkills, NomadSpells),
        new ClassAbilitySeed(Kleryk, KlerykSkills, KlerykSpells),
        new ClassAbilitySeed(Wojownik, WojownikSkills, []),
        new ClassAbilitySeed(Barbarzynca, BarbarzyncaSkills, []),
        new ClassAbilitySeed(Mag, MagSkills, MagSpells),
    }.ToDictionary(seed => seed.Class, seed => seed, StringComparer.OrdinalIgnoreCase);

    /// <summary>All seeded classes' names, for a usage message when "/mapuj" is given an
    /// unrecognized one.</summary>
    public static IReadOnlyList<string> KnownClasses => ByClass.Keys.ToArray();

    /// <summary>Case-insensitive lookup — null when no seed data exists yet for that class.</summary>
    public static ClassAbilitySeed? Find(string className) =>
        !string.IsNullOrWhiteSpace(className) && ByClass.TryGetValue(className.Trim(), out var seed)
            ? seed
            : null;
}
