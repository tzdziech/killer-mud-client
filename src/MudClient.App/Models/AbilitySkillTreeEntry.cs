using MudClient.App.Services;

namespace MudClient.App.Models;

/// <summary>
/// Wraps one captured <see cref="AbilityCaptureEntry"/> for display in the Wędrowiec skill tree,
/// evaluated against a chosen "browsing" class (see
/// <see cref="ViewModels.KilleropediaViewModel.SelectedAbilityClass"/>). A Wędrowiec's own class
/// list always includes every ability any class knows, but the game's "Specjalizacja wedrowca"
/// field decides whether a given specialization actually lets them use it:
/// <list type="bullet">
/// <item>"kazda specjalizacja" abilities are <see cref="IsOwned"/> — truly yours no matter what.</item>
/// <item>Abilities gated to one or more specific classes are only relevant while browsing one of
/// those classes, and even then are shown as an un-owned <b>preview</b> ("if I picked this
/// specialization, I'd gain this") rather than something already gained — see
/// <see cref="Create"/>, which returns <see langword="null"/> for anything irrelevant to the
/// class currently being browsed so callers can simply drop it from the tree.</item>
/// </list>
/// Not persisted; rebuilt whenever the browsing class or the underlying catalog changes.
/// </summary>
public sealed class AbilitySkillTreeEntry
{
    public required AbilityCaptureEntry Source { get; init; }

    public required string BrowsedClass { get; init; }

    public string Name => Source.Name;

    public string? Type => Source.Type;

    public string? Description => Source.Description;

    public string? Syntax => Source.Syntax;

    public string? Target => Source.Target;

    public string? School => Source.School;

    public string? Alignment => Source.Alignment;

    public string? SeeAlso => Source.SeeAlso;

    public IReadOnlyList<string> Teachers => Source.Teachers;

    public string? WandererSpecialization => Source.WandererSpecialization;

    /// <summary>Minimum level at which <see cref="BrowsedClass"/> itself learns this, from the
    /// game's own "Dostepne dla klas" line.</summary>
    public int? BrowsedClassLevel { get; init; }

    /// <summary>Minimum level at which a Wędrowiec learns this, from the game's own class list —
    /// always present, since <see cref="Create"/> excludes abilities Wędrowiec can never learn.</summary>
    public required int WandererLevel { get; init; }

    /// <summary>True when this is unconditionally yours ("kazda specjalizacja"); false means it's
    /// only being shown as a preview of what <see cref="BrowsedClass"/> would additionally grant.</summary>
    public required bool IsOwned { get; init; }

    public required string WandererAvailabilityText { get; init; }

    public string AvailableForClassesText => string.Join(", ",
        Source.AvailableForClasses.Select(entry => $"{entry.ClassName} ({entry.MinLevel} lvl)"));

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool HasSyntax => !string.IsNullOrWhiteSpace(Syntax);

    public bool HasSchool => !string.IsNullOrWhiteSpace(School);

    public bool HasTarget =>
        !string.IsNullOrWhiteSpace(Target) && !string.Equals(Target, "brak", StringComparison.OrdinalIgnoreCase);

    public bool HasAlignment =>
        !string.IsNullOrWhiteSpace(Alignment)
        && !string.Equals(Alignment, "brak ograniczen", StringComparison.OrdinalIgnoreCase);

    public bool HasSeeAlso => !string.IsNullOrWhiteSpace(SeeAlso);

    public bool HasTeachers => Teachers.Count > 0;

    /// <summary>The spell's "krąg" (circle) — the game's spell-tier concept, distinct from the
    /// character-level gate. Only known for classes with a hand-curated seed list (see
    /// <see cref="AbilitySeedCatalog"/>); <see langword="null"/> when nobody's supplied that data
    /// yet, in which case it's simply omitted from display rather than guessed at.</summary>
    public int? SpellCircle { get; init; }

    public bool HasSpellCircle => SpellCircle is not null;

    public string CircleText => $"Krąg {SpellCircle}";

    /// <summary>Dims the node/row for a preview (not-yet-owned) ability — the "wyszarzone"
    /// (grayed-out) requirement for abilities only shown because of the class being browsed.</summary>
    public double RowOpacity => IsOwned ? 1.0 : 0.55;

    public string LevelSummaryText
    {
        get
        {
            var browsedPart = BrowsedClassLevel is { } level
                ? $"{BrowsedClass}: {level} lvl"
                : $"{BrowsedClass}: brak danych";
            var levelText = string.Equals(BrowsedClass, "Wedrowiec", StringComparison.OrdinalIgnoreCase)
                ? browsedPart
                : $"{browsedPart} • Wędrowiec: {WandererLevel} lvl";
            return HasSpellCircle ? $"{levelText} • {CircleText}" : levelText;
        }
    }

    public string SearchableText => string.Join(' ',
        Name, Type, Description, Syntax, Target, School,
        string.Join(' ', Teachers), AvailableForClassesText, WandererSpecialization);

    /// <summary>
    /// Builds the tree entry for <paramref name="source"/> as seen while browsing
    /// <paramref name="browsedClass"/>, or <see langword="null"/> when it has nothing to do with
    /// a Wędrowiec browsing that class — either Wędrowiec can never learn it at all, or it's
    /// gated to some other specialization entirely (excluded rather than shown grayed, so
    /// browsing stays focused on "what I have" / "what this specialization would add").
    /// </summary>
    public static AbilitySkillTreeEntry? Create(AbilityCaptureEntry source, string browsedClass)
    {
        var wandererReq = source.AvailableForClasses.FirstOrDefault(
            requirement => string.Equals(requirement.ClassName, "Wedrowiec", StringComparison.OrdinalIgnoreCase));
        if (wandererReq is null)
        {
            return null;
        }

        var specialization = source.WandererSpecialization?.Trim();
        var isUniversal = string.Equals(specialization, "kazda specjalizacja", StringComparison.OrdinalIgnoreCase);
        var isBrowsingBaseWanderer = string.Equals(browsedClass, "Wedrowiec", StringComparison.OrdinalIgnoreCase);

        bool isOwned;
        bool include;
        string availabilityText;

        if (isUniversal)
        {
            isOwned = true;
            include = true;
            availabilityText = $"Zawsze dostępne dla Wędrowca (poziom {wandererReq.MinLevel}).";
        }
        else if (string.IsNullOrWhiteSpace(specialization) || isBrowsingBaseWanderer)
        {
            // No specialization chosen (browsing base Wędrowiec) — specialization-gated abilities
            // aren't yours yet, so they don't belong in the "what I have" view at all.
            isOwned = false;
            include = false;
            availabilityText = string.IsNullOrWhiteSpace(specialization)
                ? "Brak danych o wymaganej specjalizacji Wędrowca."
                : $"Wymaga specjalizacji Wędrowca: {specialization}.";
        }
        else
        {
            var requiredSpecializations = specialization
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            include = requiredSpecializations.Any(
                spec => string.Equals(spec, browsedClass, StringComparison.OrdinalIgnoreCase));
            isOwned = false;
            availabilityText = include
                ? $"Podgląd — zyskasz to wybierając specjalizację „{browsedClass}” (poziom {wandererReq.MinLevel})."
                : $"Wymaga specjalizacji Wędrowca: {specialization}.";
        }

        if (!include)
        {
            return null;
        }

        var browsedReq = source.AvailableForClasses.FirstOrDefault(
            requirement => string.Equals(requirement.ClassName, browsedClass, StringComparison.OrdinalIgnoreCase));

        return new AbilitySkillTreeEntry
        {
            Source = source,
            BrowsedClass = browsedClass,
            BrowsedClassLevel = browsedReq?.MinLevel,
            WandererLevel = wandererReq.MinLevel,
            IsOwned = isOwned,
            WandererAvailabilityText = availabilityText,
            SpellCircle = FindSpellCircle(source.Name),
        };
    }

    /// <summary>Searches every seeded class's spell list (not just <paramref name="browsedClass"/>
    /// — a spell's circle is a property of the spell itself, and we want it whenever any class's
    /// seed list happens to know it) for one matching <paramref name="abilityName"/> by name.</summary>
    private static int? FindSpellCircle(string abilityName) =>
        AbilitySeedCatalog.KnownClasses
            .Select(AbilitySeedCatalog.Find)
            .Where(seed => seed is not null)
            .SelectMany(seed => seed!.Spells)
            .FirstOrDefault(spell => string.Equals(spell.Name, abilityName, StringComparison.OrdinalIgnoreCase))
            ?.Circle;
}
