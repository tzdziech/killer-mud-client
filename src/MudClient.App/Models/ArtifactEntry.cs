using MudClient.Core.Killeropedia;

namespace MudClient.App.Models;

/// <summary>
/// A "try &lt;n&gt;" capture (see <see cref="Services.ArtifactTryStore"/>) parsed into structured
/// fields by <see cref="ArtifactHelpParser"/>. Not persisted itself — rebuilt from
/// <c>ArtifactTryEntry.RawText</c> every time the catalog loads, the same way
/// <c>AbilitySkillTreeEntry</c> is rebuilt from <c>AbilityCaptureEntry</c>.
/// </summary>
public sealed class ArtifactEntry
{
    public required string Name { get; init; }

    public required string RawText { get; init; }

    public required DateTimeOffset CapturedAt { get; init; }

    public string? Condition { get; init; }

    public double? WeightKg { get; init; }

    public string? Material { get; init; }

    public bool IsIndestructible { get; init; }

    public bool IsCursed { get; init; }

    public IReadOnlyList<string> AllowedClassesOnly { get; init; } = [];

    public IReadOnlyList<string> ForbiddenClasses { get; init; } = [];

    public IReadOnlyList<string> AllowedRacesOnly { get; init; } = [];

    public IReadOnlyList<string> ForbiddenRaces { get; init; } = [];

    public IReadOnlyList<string> ForbiddenAlignments { get; init; } = [];

    public string? MageSchoolRestriction { get; init; }

    public string? WeaponType { get; init; }

    public int? HitBonus { get; init; }

    public string? DamageText { get; init; }

    public string? ArmorType { get; init; }

    public string? ArmorClassText { get; init; }

    public IReadOnlyList<ArtifactStatBonus> StatBonuses { get; init; } = [];

    public IReadOnlyList<string> GrantedAbilities { get; init; } = [];

    public int SocketCount { get; init; }

    public bool IsPartOfSet { get; init; }

    public IReadOnlyList<string> SetMembers { get; init; } = [];

    public IReadOnlyList<string> SetBonuses { get; init; } = [];

    public bool HasWeapon => WeaponType is not null;

    public bool HasArmor => ArmorType is not null;

    public bool HasStatBonuses => StatBonuses.Count > 0;

    public bool HasGrantedAbilities => GrantedAbilities.Count > 0;

    public bool HasClassRestriction => AllowedClassesOnly.Count > 0 || ForbiddenClasses.Count > 0;

    public bool HasRaceRestriction => AllowedRacesOnly.Count > 0 || ForbiddenRaces.Count > 0;

    public bool HasAlignmentRestriction => ForbiddenAlignments.Count > 0;

    public bool HasSockets => SocketCount > 0;

    public string ClassRestrictionText => AllowedClassesOnly.Count > 0
        ? $"Tylko: {string.Join(", ", AllowedClassesOnly)}"
        : ForbiddenClasses.Count > 0
            ? $"Nie mogą używać: {string.Join(", ", ForbiddenClasses)}"
            : "Brak ograniczeń klasowych";

    public string RaceRestrictionText => AllowedRacesOnly.Count > 0
        ? $"Tylko: {string.Join(", ", AllowedRacesOnly)}"
        : $"Nie mogą używać: {string.Join(", ", ForbiddenRaces)}";

    public string AlignmentRestrictionText => $"Nie mogą używać: {string.Join(", ", ForbiddenAlignments)}";

    public string WeaponSummaryText => $"{WeaponType}, trafienie +{HitBonus}, {DamageText}";

    public string ArmorSummaryText => $"{ArmorType}, {ArmorClassText}";

    public string StatBonusesText => string.Join(
        "\n", StatBonuses.Select(bonus => $"{bonus.Stat}: {(bonus.Amount >= 0 ? "+" : string.Empty)}{bonus.Amount}"));

    public string GrantedAbilitiesText => string.Join(", ", GrantedAbilities);

    public string SetMembersText => string.Join("\n", SetMembers);

    public string SetBonusesText => string.Join("\n", SetBonuses);

    public string WeightMaterialText => (WeightKg, Material) switch
    {
        (not null, not null) => $"{WeightKg:0.##} kg, {Material}",
        (not null, null) => $"{WeightKg:0.##} kg",
        (null, not null) => Material!,
        (null, null) => "brak danych",
    };

    public string SearchableText => string.Join(
        ' ',
        Name, ClassRestrictionText, RaceRestrictionText, WeaponType, ArmorType,
        StatBonusesText, GrantedAbilitiesText, Material);

    /// <summary>True when a character of <paramref name="className"/> could actually equip/wield
    /// this — not on a forbidden list, and (when the item is gated to specific classes at all)
    /// on the allowed list.</summary>
    public bool FitsClass(string className) =>
        !ForbiddenClasses.Contains(className, StringComparer.OrdinalIgnoreCase)
        && (AllowedClassesOnly.Count == 0 || AllowedClassesOnly.Contains(className, StringComparer.OrdinalIgnoreCase));

    /// <summary>Every class named in this entry's restrictions, allowed or forbidden — used to
    /// build the class-filter checklist without needing a hardcoded class roster.</summary>
    public IEnumerable<string> ReferencedClasses => AllowedClassesOnly.Concat(ForbiddenClasses);

    private static ArtifactEntry FromParsed(string rawText, DateTimeOffset capturedAt, ParsedArtifact parsed) => new()
    {
        Name = parsed.Name,
        RawText = rawText,
        CapturedAt = capturedAt,
        Condition = parsed.Condition,
        WeightKg = parsed.WeightKg,
        Material = parsed.Material,
        IsIndestructible = parsed.IsIndestructible,
        IsCursed = parsed.IsCursed,
        AllowedClassesOnly = parsed.AllowedClassesOnly,
        ForbiddenClasses = parsed.ForbiddenClasses,
        AllowedRacesOnly = parsed.AllowedRacesOnly,
        ForbiddenRaces = parsed.ForbiddenRaces,
        ForbiddenAlignments = parsed.ForbiddenAlignments,
        MageSchoolRestriction = parsed.MageSchoolRestriction,
        WeaponType = parsed.WeaponType,
        HitBonus = parsed.HitBonus,
        DamageText = parsed.DamageText,
        ArmorType = parsed.ArmorType,
        ArmorClassText = parsed.ArmorClassText,
        StatBonuses = parsed.StatBonuses,
        GrantedAbilities = parsed.GrantedAbilities,
        SocketCount = parsed.SocketCount,
        IsPartOfSet = parsed.IsPartOfSet,
        SetMembers = parsed.SetMembers,
        SetBonuses = parsed.SetBonuses,
    };

    /// <summary>Parses every raw capture and collapses same-name duplicates (case-insensitive) —
    /// captures can repeat across separate "/mapuj" runs — keeping whichever parse recognized the
    /// most fields (<see cref="ParsedArtifact.Completeness"/>), with raw text length as a
    /// tiebreaker for genuinely equal captures.</summary>
    public static IReadOnlyList<ArtifactEntry> MergeByName(IEnumerable<ArtifactTryEntry> rawEntries)
    {
        return rawEntries
            .Select(entry => (Entry: entry, Parsed: ArtifactHelpParser.Parse(entry.RawText)))
            .Where(pair => pair.Parsed is not null)
            .Select(pair => (pair.Entry, Parsed: pair.Parsed!))
            .GroupBy(pair => pair.Parsed.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(pair => pair.Parsed.Completeness)
                .ThenByDescending(pair => pair.Entry.RawText.Length)
                .First())
            .Select(pair => FromParsed(pair.Entry.RawText, pair.Entry.CapturedAt, pair.Parsed))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
