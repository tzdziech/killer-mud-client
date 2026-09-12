using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.Core.Combat;

/// <summary>
/// Maps this MUD's combat damage verbs to their approximate numeric tier, for lines that mean
/// "you dealt this damage".
///
/// Two message shapes both count as your own damage:
///  - 2nd person: "Ranisz golema mieczem." — you're literally the grammatical subject. Ends in
///    "sz"/"SZ" and is unambiguous on its own.
///  - 3rd person via a named technique: "Twoje miażdżące walnięcie dewastuje sędziwego
///    krasnoluda." — the subject is the technique noun ("Twoje ... walnięcie"), not "you", so the
///    verb conjugates in 3rd person ("dewastuje") even though it's your own hit. On its own this
///    form is ambiguous (the same verb describes a mob hitting you, or bystander-visible combat
///    between others) — it only counts here when the line also contains "Twoj*" ("Twoje"/"Twój"/
///    "Twoja"/"Twoim"/...), confirming the technique is yours.
///
/// A couple of encoding-mangled variants (e.g. the "Å" mojibake) are kept too, in case a client
/// encoding misdetection ever produces them — they cost nothing to keep around.
/// </summary>
public static class DamagePhrases
{
    private const int MinimumInflectedNamePrefixLength = 3;
    private static readonly IReadOnlyDictionary<string, int> SelfVerbValues = new Dictionary<string, int>
    {
        ["Chybiasz"] = 0,
        ["chybiasz"] = 0,
        ["chybiajÄc"] = 0,
        ["chybiając"] = 0,
        ["chybiajac"] = 0,
        ["Siniaczysz"] = 2,
        ["siniaczysz"] = 2,
        ["Muskasz"] = 6,
        ["muskasz"] = 6,
        ["Ledwie ranisz"] = 10,
        ["ledwie ranisz"] = 10,
        ["Lekko ranisz"] = 14,
        ["lekko ranisz"] = 14,
        // The source table only had "Eanisz" (an R→E misread/typo) for this tier's capitalized
        // form, never the correctly spelled "Ranisz" — added here so a hit landing at the start
        // of a sentence is still recognized; "Eanisz" is kept too in case the server really does
        // send it.
        ["Ranisz"] = 18,
        ["Eanisz"] = 18,
        ["ranisz"] = 18,
        ["Mocno ranisz"] = 22,
        ["mocno ranisz"] = 22,
        ["Dotkliwie ranisz"] = 26,
        ["dotkliwie ranisz"] = 26,
        ["Powaznie ranisz"] = 30,
        ["powaznie ranisz"] = 30,
        ["PowaÅ¼nie ranisz"] = 30,
        ["powaÅ¼nie ranisz"] = 30,
        ["Poważnie ranisz"] = 30,
        ["poważnie ranisz"] = 30,
        ["Masakrujesz"] = 34,
        ["masakrujesz"] = 34,
        ["Rozpruwasz"] = 38,
        ["rozpruwasz"] = 38,
        ["Dewastujesz"] = 44,
        ["dewastujesz"] = 44,
        ["Grzmocisz"] = 50,
        ["grzmocisz"] = 50,
        ["Niszczysz"] = 55,
        ["niszczysz"] = 55,
        ["NISZCZYSZ"] = 60,
        ["DRUZGOCZESZ"] = 67,
        ["ROZPRUWASZ"] = 75,
        ["ROZRYWASZ"] = 84,
        ["ROZBEBESZASZ"] = 100,
        ["DEKAPITUJESZ"] = 115,
        ["EKSTYRPUJESZ"] = 130,
        ["ANIHILUJESZ"] = 145,
        ["USMIERCASZ"] = 200,
        ["UÅMIERCASZ"] = 200,
        ["UŚMIERCASZ"] = 200,
        ["UNICESTWIASZ"] = 201,
    };

    private static readonly IReadOnlyDictionary<string, int> TechniqueVerbValues = new Dictionary<string, int>
    {
        ["chybia"] = 0,
        ["siniaczy"] = 2,
        ["muska"] = 6,
        ["ledwie rani"] = 10,
        ["lekko rani"] = 14,
        ["rani"] = 18,
        ["mocno rani"] = 22,
        ["dotkliwie rani"] = 26,
        ["powaznie rani"] = 30,
        ["powaÅ¼nie rani"] = 30,
        ["poważnie rani"] = 30,
        ["masakruje"] = 34,
        ["rozpruwa"] = 38,
        ["dewastuje"] = 44,
        ["grzmoci"] = 50,
        ["niszczy"] = 55,
        ["NISZCZY"] = 60,
        ["DRUZGOCZE"] = 67,
        ["ROZPRUWA"] = 75,
        ["ROZRYWA"] = 84,
        ["ROZBEBESZA"] = 100,
        ["DEKAPITUJE"] = 115,
        ["EKSTYRPUJE"] = 130,
        ["ANIHILUJE"] = 145,
        ["USMIERCA"] = 200,
        ["UÅMIERCA"] = 200,
        ["UŚMIERCA"] = 200,
        ["UNICESTWIA"] = 201,
    };

    private static readonly Regex SelfVerbPattern = BuildPattern(SelfVerbValues.Keys);
    private static readonly Regex TechniqueVerbPattern = BuildPattern(TechniqueVerbValues.Keys);
    private static readonly Regex OwnTechniquePattern = new(
        @"\bTwoj\w*\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex WordPattern = new(
        @"\p{L}+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static Regex BuildPattern(IEnumerable<string> phrases)
    {
        var alternation = string.Join(
            '|', phrases.OrderByDescending(phrase => phrase.Length).Select(Regex.Escape));
        return new Regex($@"\b(?:{alternation})\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    /// <summary>Finds a recognized "you dealt damage" phrase in <paramref name="line"/> (ANSI
    /// escape codes are stripped before matching) and returns its numeric tier.</summary>
    public static bool TryGetDamage(string line, out int damage) =>
        TryGetDamage(line, out damage, out _);

    /// <summary>As above, additionally returns the attack type written between the possessive
    /// pronoun and the damage verb, for example <c>ciecie</c> in <c>Twoje ciecie ROZPRUWA</c>.</summary>
    public static bool TryGetDamage(string line, out int damage, out string damageType)
    {
        var plain = AnsiText.StripKillerColors(AnsiText.StripAnsi(line));

        var selfMatch = SelfVerbPattern.Match(plain);
        if (selfMatch.Success)
        {
            damage = SelfVerbValues[selfMatch.Value];
            damageType = ReadOwnDamageType(plain, selfMatch.Index);
            return true;
        }

        var ownTechnique = OwnTechniquePattern.Match(plain);
        if (ownTechnique.Success)
        {
            var techniqueMatch = TechniqueVerbPattern.Match(plain);
            if (techniqueMatch.Success)
            {
                damage = TechniqueVerbValues[techniqueMatch.Value];
                damageType = ReadOwnDamageType(plain, techniqueMatch.Index);
                return true;
            }
        }

        damage = 0;
        damageType = string.Empty;
        return false;
    }

    /// <summary>Recognizes a third-person damage phrase only when the inflected attacker name
    /// immediately before the verb has one unambiguous word-by-word prefix match with a supplied
    /// canonical group-member name (for example Agrona → Agron or oswojonego wilka → Oswojony
    /// wilk). Callers supply only group members currently present in Room.People, so unrelated
    /// mob attacks are not added.</summary>
    public static bool TryGetGroupMemberDamage(
        string line,
        IEnumerable<string> groupMemberNames,
        out string attackerName,
        out int damage) =>
        TryGetGroupMemberDamage(line, groupMemberNames, out attackerName, out damage, out _);

    /// <summary>As above, additionally returns the attack description before the matched attacker
    /// name, for example <c>Wyssanie zycia</c> in <c>Wyssanie zycia cienia muska</c>.</summary>
    public static bool TryGetGroupMemberDamage(
        string line,
        IEnumerable<string> groupMemberNames,
        out string attackerName,
        out int damage,
        out string damageType)
    {
        var plain = AnsiText.StripKillerColors(AnsiText.StripAnsi(line));
        var verb = TechniqueVerbPattern.Match(plain);
        if (!verb.Success)
        {
            attackerName = string.Empty;
            damage = 0;
            damageType = string.Empty;
            return false;
        }

        var wordsBeforeVerb = WordPattern.Matches(plain[..verb.Index])
            .Select(match => match.Value)
            .ToArray();
        if (wordsBeforeVerb.Length == 0)
        {
            attackerName = string.Empty;
            damage = 0;
            damageType = string.Empty;
            return false;
        }

        var match = groupMemberNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name =>
            {
                var nameMatch = FindInflectedNameMatch(wordsBeforeVerb, name);
                return (Name: name, NameMatch: nameMatch);
            })
            .Where(candidate => candidate.NameMatch is not null)
            .Select(candidate => new GroupMemberDamageMatch(candidate.Name,
                candidate.NameMatch!.Value.WordCount, candidate.NameMatch.Value.PrefixLength,
                candidate.NameMatch.Value.IsShortName))
            .OrderBy(candidate => candidate.IsShortName)
            .ThenByDescending(candidate => candidate.PrefixLength)
            .ThenByDescending(candidate => candidate.Name.Length)
            .ToList();
        if (match.Count > 0 &&
            (match.Count == 1 ||
             match[0].IsShortName != match[1].IsShortName ||
             match[0].PrefixLength > match[1].PrefixLength))
        {
            attackerName = match[0].Name;
            damage = TechniqueVerbValues[verb.Value];
            var attackerStart = wordsBeforeVerb.Length - match[0].WordCount;
            damageType = attackerStart > 0
                ? NormalizeDamageType(string.Join(' ', wordsBeforeVerb[..attackerStart]))
                : "Inne";
            return true;
        }

        attackerName = string.Empty;
        damage = 0;
        damageType = string.Empty;
        return false;
    }

    private static string ReadOwnDamageType(string plain, int verbIndex)
    {
        var own = OwnTechniquePattern.Match(plain[..verbIndex]);
        if (!own.Success) return "Inne";

        var words = WordPattern.Matches(plain[(own.Index + own.Length)..verbIndex])
            .Select(match => match.Value)
            .ToArray();
        return words.Length > 0 ? NormalizeDamageType(string.Join(' ', words)) : "Inne";
    }

    private static string NormalizeDamageType(string value) => value.Length switch
    {
        0 => "Inne",
        1 => value.ToUpperInvariant(),
        _ => char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant(),
    };

    private static InflectedNameMatch? FindInflectedNameMatch(IReadOnlyList<string> wordsBeforeVerb,
        string canonicalName)
    {
        var canonicalWords = WordPattern.Matches(canonicalName)
            .Select(match => match.Value)
            .ToArray();
        if (canonicalWords.Length == 0)
        {
            return null;
        }

        var fullNameMatch = MatchTrailingNameWords(wordsBeforeVerb, canonicalWords);
        if (fullNameMatch >= 0)
        {
            return new InflectedNameMatch(canonicalWords.Length, fullNameMatch, IsShortName: false);
        }

        // Some companion actions use only the inflected first word of a multi-word GMCP name,
        // e.g. "cienia" for "Cienisty lord". It is still safe only when this is the unique
        // best visible group-member match selected by the caller.
        if (canonicalWords.Length > 1)
        {
            var firstWordMatch = MatchTrailingNameWords(wordsBeforeVerb, canonicalWords[..1]);
            if (firstWordMatch >= 0)
            {
                return new InflectedNameMatch(1, firstWordMatch, IsShortName: true);
            }
        }

        return null;
    }

    private static int MatchTrailingNameWords(IReadOnlyList<string> wordsBeforeVerb,
        IReadOnlyList<string> canonicalWords)
    {
        if (canonicalWords.Count > wordsBeforeVerb.Count) return -1;

        var offset = wordsBeforeVerb.Count - canonicalWords.Count;
        var totalPrefixLength = 0;
        for (var index = 0; index < canonicalWords.Count; index++)
        {
            var prefixLength = CommonPrefixLength(wordsBeforeVerb[offset + index], canonicalWords[index]);
            if (prefixLength < Math.Min(MinimumInflectedNamePrefixLength, canonicalWords[index].Length))
            {
                return -1;
            }

            totalPrefixLength += prefixLength;
        }

        return totalPrefixLength;
    }

    private static int CommonPrefixLength(string left, string right)
    {
        left = PolishText.Fold(left);
        right = PolishText.Fold(right);
        var maximum = Math.Min(left.Length, right.Length);
        var length = 0;
        while (length < maximum && char.ToUpperInvariant(left[length]) == char.ToUpperInvariant(right[length]))
        {
            length++;
        }

        return length;
    }

    private readonly record struct InflectedNameMatch(int WordCount, int PrefixLength, bool IsShortName);
    private readonly record struct GroupMemberDamageMatch(
        string Name, int WordCount, int PrefixLength, bool IsShortName);

}
