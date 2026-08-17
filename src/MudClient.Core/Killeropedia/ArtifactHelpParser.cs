using System.Globalization;
using System.Text.RegularExpressions;

namespace MudClient.Core.Killeropedia;

public sealed record ArtifactStatBonus(string Stat, int Amount);

/// <summary>Everything <see cref="ArtifactHelpParser"/> can pull out of a single "try &lt;n&gt;"
/// response's free-form narrative text.</summary>
public sealed record ParsedArtifact(
    string Name,
    string? Condition,
    double? WeightKg,
    string? Material,
    bool IsIndestructible,
    bool IsCursed,
    IReadOnlyList<string> AllowedClassesOnly,
    IReadOnlyList<string> ForbiddenClasses,
    IReadOnlyList<string> AllowedRacesOnly,
    IReadOnlyList<string> ForbiddenRaces,
    IReadOnlyList<string> ForbiddenAlignments,
    string? MageSchoolRestriction,
    string? WeaponType,
    int? HitBonus,
    string? DamageText,
    string? ArmorType,
    string? ArmorClassText,
    IReadOnlyList<ArtifactStatBonus> StatBonuses,
    IReadOnlyList<string> GrantedAbilities,
    int SocketCount,
    bool IsPartOfSet,
    IReadOnlyList<string> SetMembers,
    IReadOnlyList<string> SetBonuses)
{
    /// <summary>How much of the free-form text this parse actually recognized — used to pick the
    /// "richest" capture when the same item name was captured more than once (see
    /// <c>ArtifactCatalogMerger</c> in the App project). A plain field/note count, not a
    /// weighted score — good enough to tell "this capture has stats and a set" from "this one is
    /// just the flavor text and a weight line."</summary>
    public int Completeness =>
        (Condition is null ? 0 : 1) + (WeightKg is null && Material is null ? 0 : 1)
        + AllowedClassesOnly.Count + ForbiddenClasses.Count + AllowedRacesOnly.Count + ForbiddenRaces.Count
        + ForbiddenAlignments.Count + (WeaponType is null ? 0 : 1) + (ArmorType is null ? 0 : 1)
        + StatBonuses.Count + GrantedAbilities.Count + (SocketCount > 0 ? 1 : 0)
        + (IsPartOfSet ? 1 + SetMembers.Count + SetBonuses.Count : 0);
}

/// <summary>
/// Parses the game's own "try &lt;n&gt;" output (captured verbatim by
/// <c>ArtifactTryMappingCoordinator</c>/"/mapuj &lt;liczba&gt;") into structured fields. Unlike
/// <see cref="AbilityHelpParser"/>'s labeled "Label: value" header, this response is free-form
/// narrative prose — the item's name, restrictions and stats are each embedded in one of a small
/// number of recurring sentence templates the MUD always uses, so this scans line by line for
/// those templates rather than a positional header block.
/// </summary>
public static partial class ArtifactHelpParser
{
    /// <summary>Plural class nouns → canonical singular class name, as they appear in "Przedmiotu
    /// tego nie moga uzywac &lt;X&gt;."/"Przedmiot ten moga uzywac tylko &lt;X&gt;." sentences.
    /// Discovered by enumerating every distinct restriction sentence across a real capture set —
    /// extend this table if the game ever reports a class not listed here.</summary>
    private static readonly IReadOnlyDictionary<string, string> ClassNouns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["barbarzyncy"] = "Barbarzynca",
        ["bardowie"] = "Bard",
        ["czarni rycerze"] = "Czarny Rycerz",
        ["druidzi"] = "Druid",
        ["klerycy"] = "Kleryk",
        ["magowie"] = "Mag",
        ["nomadzi"] = "Nomad",
        ["paladyni"] = "Paladyn",
        ["szamani"] = "Szaman",
        ["wojownicy"] = "Wojownik",
        ["zlodzieje"] = "Zlodziej",
    };

    private static readonly IReadOnlyDictionary<string, string> RaceNouns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["elfy"] = "Elf",
        ["gnomy"] = "Gnom",
        ["krasnoludy"] = "Krasnolud",
        ["ludzie"] = "Czlowiek",
        ["niziolki"] = "Niziolek",
        ["polelfy"] = "Polelf",
        ["polorki"] = "Polork",
    };

    private static readonly IReadOnlyDictionary<string, string> AlignmentNouns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["istoty dobre"] = "dobry",
        ["istoty neutralne"] = "neutralny",
        ["istoty zle"] = "zly",
    };

    public static ParsedArtifact? Parse(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var name = ExtractName(rawText);
        if (name is null)
        {
            return null;
        }

        var condition = JestWStanieRegex().Match(rawText) is { Success: true } conditionMatch
            ? conditionMatch.Groups["condition"].Value.Trim()
            : null;

        var (weightKg, material) = ExtractWeightAndMaterial(rawText);

        var allowedClasses = new List<string>();
        var forbiddenClasses = new List<string>();
        var allowedRaces = new List<string>();
        var forbiddenRaces = new List<string>();
        var forbiddenAlignments = new List<string>();
        string? mageSchool = null;

        foreach (Match match in RestrictionRegex().Matches(rawText))
        {
            var isAllowOnly = match.Groups["allow"].Success;
            var subject = match.Groups["subject"].Value.Trim();

            var schoolMatch = MageSchoolRegex().Match(subject);
            if (schoolMatch.Success)
            {
                mageSchool = schoolMatch.Groups["school"].Value.Trim();
                subject = subject[..schoolMatch.Index].Trim();
            }

            if (AlignmentNouns.TryGetValue(subject, out var alignment))
            {
                forbiddenAlignments.Add(alignment);
            }
            else if (ClassNouns.TryGetValue(subject, out var className))
            {
                (isAllowOnly ? allowedClasses : forbiddenClasses).Add(className);
            }
            else if (RaceNouns.TryGetValue(subject, out var raceName))
            {
                (isAllowOnly ? allowedRaces : forbiddenRaces).Add(raceName);
            }
        }

        var statBonuses = StatBonusRegex().Matches(rawText)
            .Select(match => new ArtifactStatBonus(
                match.Groups["stat"].Value.Trim(),
                int.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture)))
            .ToArray();

        var grantedAbilities = GrantedAbilityRegex().Matches(rawText)
            .Select(match => match.Groups["ability"].Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var socketCount = SocketRegex().Matches(rawText).Count;

        var weaponType = WeaponTypeRegex().Match(rawText) is { Success: true } weaponMatch
            ? weaponMatch.Groups["type"].Value.Trim()
            : null;
        var hitBonus = HitBonusRegex().Match(rawText) is { Success: true } hitMatch
            ? int.Parse(hitMatch.Groups["bonus"].Value, CultureInfo.InvariantCulture)
            : (int?)null;
        var damageText = DamageRegex().Match(rawText) is { Success: true } damageMatch
            ? damageMatch.Groups["damage"].Value.Trim()
            : null;

        var armorType = ArmorTypeRegex().Match(rawText) is { Success: true } armorMatch
            ? armorMatch.Groups["type"].Value.Trim()
            : null;
        var armorClassText = ArmorClassRegex().Match(rawText) is { Success: true } armorClassMatch
            ? armorClassMatch.Groups["ac"].Value.Trim()
            : null;

        var (isPartOfSet, setMembers, setBonuses) = ExtractSetInfo(rawText);

        return new ParsedArtifact(
            Name: name,
            Condition: condition,
            WeightKg: weightKg,
            Material: material,
            IsIndestructible: rawText.Contains(
                "zadna moc nie bylaby w stanie zniszczyc tego przedmiotu", StringComparison.OrdinalIgnoreCase),
            IsCursed: rawText.Contains("wieczna klatwa", StringComparison.OrdinalIgnoreCase),
            AllowedClassesOnly: allowedClasses,
            ForbiddenClasses: forbiddenClasses,
            AllowedRacesOnly: allowedRaces,
            ForbiddenRaces: forbiddenRaces,
            ForbiddenAlignments: forbiddenAlignments,
            MageSchoolRestriction: mageSchool,
            WeaponType: weaponType,
            HitBonus: hitBonus,
            DamageText: damageText,
            ArmorType: armorType,
            ArmorClassText: armorClassText,
            StatBonuses: statBonuses,
            GrantedAbilities: grantedAbilities,
            SocketCount: socketCount,
            IsPartOfSet: isPartOfSet,
            SetMembers: setMembers,
            SetBonuses: setBonuses);
    }

    /// <summary>The item's name always appears — in nominative case, with any quoted proper name
    /// intact — as the subject of one of a handful of recurring sentences ("X jest w ... stanie.",
    /// "X to niezwykle cenny przedmiot.", "X prawie nic nie wazy, ...", "X emanuje niezwykla
    /// magia, ..."). The longest match across those is preferred, since a longer subject usually
    /// means it kept the quoted special name rather than just the base noun. Falls back to the
    /// declined ("Postanawiasz dokladniej obejrzec X.") name — always present, but occasionally
    /// missing the quoted special name — when none of the nominative anchors are found.</summary>
    private static string? ExtractName(string rawText)
    {
        var candidates = NameAnchorRegexes()
            .Select(regex => regex.Match(rawText))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(candidate => candidate.Length > 0)
            .ToList();

        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(candidate => candidate.Length).First();
        }

        var fallback = FallbackNameRegex().Match(rawText);
        return fallback.Success ? fallback.Groups["name"].Value.Trim() : null;
    }

    private static IEnumerable<Regex> NameAnchorRegexes() =>
    [
        JestWStanieRegex(),
        NiezwykleCennyRegex(),
        PrawieNicNieWazyRegex(),
        EmanujeMagiaRegex(),
    ];

    private static (double? WeightKg, string? Material) ExtractWeightAndMaterial(string rawText)
    {
        var match = WeightRegex().Match(rawText);
        if (match.Success)
        {
            var weight = double.Parse(match.Groups["weight"].Value, CultureInfo.InvariantCulture);
            return (weight, match.Groups["material"].Value.Trim());
        }

        var lightMatch = LightWeightRegex().Match(rawText);
        return lightMatch.Success ? (null, lightMatch.Groups["material"].Value.Trim()) : (null, null);
    }

    private static (bool IsPartOfSet, IReadOnlyList<string> Members, IReadOnlyList<string> Bonuses) ExtractSetInfo(string rawText)
    {
        var setIndex = rawText.IndexOf("stanowi czesc wiekszej calosci", StringComparison.OrdinalIgnoreCase);
        if (setIndex < 0)
        {
            return (false, [], []);
        }

        var membersMatch = SetMembersRegex().Match(rawText);
        var members = membersMatch.Success
            ? membersMatch.Groups["members"].Value
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Length > 0)
                .ToArray()
            : [];

        var bonusesMatch = SetBonusesRegex().Match(rawText);
        var bonuses = bonusesMatch.Success
            ? bonusesMatch.Groups["bonuses"].Value
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.Length > 0)
                .ToArray()
            : [];

        return (true, members, bonuses);
    }

    [GeneratedRegex(@"^(?<name>.+?) jest w (?<condition>[\p{L} ]+?) stanie\.", RegexOptions.Multiline)]
    private static partial Regex JestWStanieRegex();

    [GeneratedRegex(@"^(?<name>.+?) to niezwykle cenny przedmiot\.", RegexOptions.Multiline)]
    private static partial Regex NiezwykleCennyRegex();

    [GeneratedRegex(@"^(?<name>.+?) prawie nic nie wazy,", RegexOptions.Multiline)]
    private static partial Regex PrawieNicNieWazyRegex();

    [GeneratedRegex(@"^(?<name>.+?) emanuje niezwykla magia,", RegexOptions.Multiline)]
    private static partial Regex EmanujeMagiaRegex();

    [GeneratedRegex(@"Postanawiasz dokladniej obejrzec (?<name>[^.\r\n]+)\.")]
    private static partial Regex FallbackNameRegex();

    [GeneratedRegex(@"wynosi okolo (?<weight>[\d.,]+) kg, przedmiot ten wykonano z materialu '(?<material>[^']+)'")]
    private static partial Regex WeightRegex();

    [GeneratedRegex(@"prawie nic nie wazy, przedmiot ten wykonano z materialu '(?<material>[^']+)'")]
    private static partial Regex LightWeightRegex();

    [GeneratedRegex(
        @"^(?:(?<allow>Przedmiot ten moga uzywac tylko)|Przedmiotu tego nie moga uzywac) (?<subject>[^.\r\n]+)\.",
        RegexOptions.Multiline)]
    private static partial Regex RestrictionRegex();

    [GeneratedRegex(@"^ze szkoly (?<school>[\p{L}]+)$", RegexOptions.IgnoreCase)]
    private static partial Regex MageSchoolRegex();

    [GeneratedRegex(@"^Wplywa na (?<stat>[^.\r\n]+?) o (?<amount>-?\d+)\.", RegexOptions.Multiline)]
    private static partial Regex StatBonusRegex();

    [GeneratedRegex(@"^[Dd]odaje (?<ability>[a-z_]+)\.", RegexOptions.Multiline)]
    private static partial Regex GrantedAbilityRegex();

    [GeneratedRegex(@"^Gniazdo \d+: ", RegexOptions.Multiline)]
    private static partial Regex SocketRegex();

    [GeneratedRegex(@"Typ broni: '(?<type>[^']+)'\.")]
    private static partial Regex WeaponTypeRegex();

    [GeneratedRegex(@"Bonus do trafienia: (?<bonus>-?\d+)\.")]
    private static partial Regex HitBonusRegex();

    [GeneratedRegex(@"Obrazenia zadawane (?<damage>[^.\r\n]+\([^)]*\))\.")]
    private static partial Regex DamageRegex();

    [GeneratedRegex(@"Rodzaj pancerza: (?<type>[^\r\n]+)")]
    private static partial Regex ArmorTypeRegex();

    [GeneratedRegex(@"Klasa pancerza: (?<ac>[^\r\n]+)")]
    private static partial Regex ArmorClassRegex();

    [GeneratedRegex(
        @"Pozostale przedmioty nalezace do kompletu:\n(?<members>.*?)\n\s*\nPo zbadaniu",
        RegexOptions.Singleline)]
    private static partial Regex SetMembersRegex();

    [GeneratedRegex(
        @"Po zbadaniu tego przedmiotu odkrywasz magiczne wlasciwosci kompletu:\n(?<bonuses>.*)$",
        RegexOptions.Singleline)]
    private static partial Regex SetBonusesRegex();
}
