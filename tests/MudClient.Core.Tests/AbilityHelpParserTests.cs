using MudClient.Core.Killeropedia;

namespace MudClient.Core.Tests;

public sealed class AbilityHelpParserTests
{
    // Real "/mapuj paladyn" capture for "axe" — two entries (AXE, AXE MASTERY) separated by a
    // "====" divider, multi-line "Nauczyciele:" continuation.
    private const string AxeRawText =
        "Nazwa:                   AXE\n" +
        "Typ:                     skill bierny\n" +
        "Dostepne dla klas:       Wojownik (1 lvl), Paladyn (1 lvl), Barbarzynca (1 lvl), Bard (1 lvl), Czarny Rycerz (1 lvl), Wedrowiec (1 lvl)\n" +
        "Specjalizacja wedrowca:  kazda specjalizacja\n" +
        "Alignment:               brak ograniczen\n" +
        "Cel:                     brak\n" +
        "Nauczyciele:             [52] barbarzynca Y'ergiz, [534] barbarzynca Brahadhan\n" +
        "                         [24933] barbarzynca, [955] rumiany krasnoludzki drwal\n" +
        "                         [38] Mistrz Gharion.\n" +
        "\n" +
        "Umiejetnosc, dzieki ktorej mozna poslugiwac sie roznej masci toporkami\n" +
        "i toporzyskami.\n" +
        "\n" +
        "============================================================\n" +
        "\n" +
        "Nazwa:                   AXE MASTERY\n" +
        "Typ:                     skill bierny\n" +
        "Dostepne dla klas:       Wojownik (20 lvl), Czarny Rycerz (20 lvl), Wedrowiec (20 lvl)\n" +
        "Specjalizacja wedrowca:  kazda specjalizacja\n" +
        "Alignment:               brak ograniczen\n" +
        "Cel:                     brak\n" +
        "Nauczyciele:             [38] Mistrz Gharion, [6199] zbrojmistrz garnizonu.\n" +
        "\n" +
        "Specjalizacja wladania okreslonym typem broni.";

    [Fact]
    public void Parse_MatchesTheBlockWithTheExactName_NotJustTheFirstBlock()
    {
        var parsed = AbilityHelpParser.Parse("axe", AxeRawText);

        Assert.NotNull(parsed);
        Assert.Equal("AXE", parsed!.Name);
        Assert.Equal("skill bierny", parsed.Type);
    }

    [Fact]
    public void Parse_ExtractsClassLevelRequirements()
    {
        var parsed = AbilityHelpParser.Parse("axe", AxeRawText)!;

        Assert.Equal(6, parsed.AvailableForClasses.Count);
        Assert.Contains(parsed.AvailableForClasses, c => c.ClassName == "Wojownik" && c.MinLevel == 1);
        Assert.Contains(parsed.AvailableForClasses, c => c.ClassName == "Wedrowiec" && c.MinLevel == 1);
    }

    [Fact]
    public void Parse_ExtractsWandererSpecializationAndAlignment()
    {
        var parsed = AbilityHelpParser.Parse("axe", AxeRawText)!;

        Assert.Equal("kazda specjalizacja", parsed.WandererSpecialization);
        Assert.Equal("brak ograniczen", parsed.Alignment);
    }

    [Fact]
    public void Parse_JoinsMultilineTeachersAcrossContinuationLines()
    {
        var parsed = AbilityHelpParser.Parse("axe", AxeRawText)!;

        Assert.Contains("[52] barbarzynca Y'ergiz", parsed.Teachers);
        Assert.Contains("[534] barbarzynca Brahadhan", parsed.Teachers);
        Assert.Contains("[24933] barbarzynca", parsed.Teachers);
        Assert.Contains("[38] Mistrz Gharion", parsed.Teachers);
    }

    [Fact]
    public void Parse_ExtractsDescriptionBelowTheHeaderFields()
    {
        var parsed = AbilityHelpParser.Parse("axe", AxeRawText)!;

        Assert.Contains("poslugiwac sie roznej masci toporkami", parsed.Description);
    }

    [Fact]
    public void Parse_DifferentNameInSameRawText_MatchesTheOtherBlock()
    {
        var parsed = AbilityHelpParser.Parse("axe mastery", AxeRawText)!;

        Assert.Equal("AXE MASTERY", parsed.Name);
        Assert.Equal(3, parsed.AvailableForClasses.Count);
    }

    [Fact]
    public void Parse_UnrecognizedNoiseLineBeforeTheHeaderBlock_IsIgnored()
    {
        // Real capture for "parry" — an unrelated system message ("Zapamietales czar...") arrived
        // during the capture window, before the actual PARRY block even starts.
        const string rawText =
            "Zapamietales czar 'cure serious'.\n" +
            "Zaczynasz zapamietywac czar 'create food'.\n" +
            "Czas zapamietywania obliczony na 4s.\n" +
            "\n" +
            "Nazwa:                   PARRY\n" +
            "Typ:                     skill aktywny\n" +
            "Dostepne dla klas:       Wojownik (12 lvl), Paladyn (12 lvl)\n" +
            "Alignment:               brak ograniczen\n" +
            "\n" +
            "Wyszkoleni wojownicy moga uzyc wlasnych broni.";

        var parsed = AbilityHelpParser.Parse("parry", rawText);

        Assert.NotNull(parsed);
        Assert.Equal("PARRY", parsed!.Name);
        Assert.DoesNotContain("Zapamietales", parsed.Description);
    }

    [Fact]
    public void Parse_MultipleUnrelatedEntriesSharingAPrefix_PicksTheOneMatchingTheSearchedName()
    {
        // Real capture for "stun" — the game's fuzzy help match bundled STUN, POWER WORD STUN and
        // STUNNING FIST (which has no "Dostepne dla klas" data at all) into one response.
        const string rawText =
            "Nazwa:                   STUN\n" +
            "Typ:                     skill aktywny\n" +
            "Dostepne dla klas:       Kleryk (12 lvl), Wojownik (10 lvl), Paladyn (10 lvl)\n" +
            "\n" +
            "Wojownik wyposazony w odpowiednia bron obuchowa.\n" +
            "\n" +
            "============================================================\n" +
            "\n" +
            "Nazwa:                   POWER WORD STUN\n" +
            "Typ:                     czar ofensywny\n" +
            "Dostepne dla klas:       Czarodziej (19 lvl), Wedrowiec (23 lvl)\n" +
            "Szkola:                  Przemiany\n" +
            "\n" +
            "Jest to niezwykle potezne zaklecie.\n" +
            "\n" +
            "============================================================\n" +
            "\n" +
            "Nazwa:                   STUNNING FIST\n" +
            "Typ:                     skill bierny\n" +
            "Dostepne dla klas:       brak\n" +
            "Alignment:               brak ograniczen\n" +
            "Cel:                     brak\n" +
            "Nauczyciele:             Brak.";

        var stun = AbilityHelpParser.Parse("stun", rawText)!;
        var powerWordStun = AbilityHelpParser.Parse("power word stun", rawText)!;
        var stunningFist = AbilityHelpParser.Parse("stunning fist", rawText)!;

        Assert.Equal("STUN", stun.Name);
        Assert.Equal(3, stun.AvailableForClasses.Count);

        Assert.Equal("POWER WORD STUN", powerWordStun.Name);
        Assert.Equal("Przemiany", powerWordStun.School);

        Assert.Equal("STUNNING FIST", stunningFist.Name);
        Assert.Empty(stunningFist.AvailableForClasses);
        Assert.Equal(["Brak"], stunningFist.Teachers);
    }

    [Fact]
    public void Parse_BlockWithoutANazwaField_IsSkippedEntirely()
    {
        const string rawText =
            "Ta magiczna inkantacja tymczasowo hartuje cialo bez zadnego naglowka.\n" +
            "\n" +
            "============================================================\n" +
            "\n" +
            "Nazwa:                   REAL ENTRY\n" +
            "Typ:                     skill bierny\n" +
            "\n" +
            "Opis prawdziwego wpisu.";

        var parsed = AbilityHelpParser.Parse("real entry", rawText);

        Assert.NotNull(parsed);
        Assert.Equal("REAL ENTRY", parsed!.Name);
    }

    [Fact]
    public void Parse_NameNotFoundAnywhere_FallsBackToFirstBlock()
    {
        var parsed = AbilityHelpParser.Parse("nieznana nazwa", AxeRawText)!;

        Assert.Equal("AXE", parsed.Name);
    }

    [Fact]
    public void Parse_EmptyRawText_ReturnsNull()
    {
        Assert.Null(AbilityHelpParser.Parse("axe", ""));
        Assert.Null(AbilityHelpParser.Parse("axe", "   "));
    }

    [Fact]
    public void Parse_RealRefreshCapture_JoinsMultilineSkladniaAndNauczyciele()
    {
        // Real "/mapuj" capture pasted directly by the user for "refresh".
        const string rawText =
            "Nazwa:                   REFRESH\n" +
            "Typ:                     czar wspomagajacy\n" +
            "Dostepne dla klas:       Druid (4 lvl), Wedrowiec (3 lvl)\n" +
            "Specjalizacja wedrowca:  kazda specjalizacja\n" +
            "Alignment:               brak ograniczen\n" +
            "Cel:                     [postac]\n" +
            "Skladnia:                cast 'refresh' [postac]\n" +
            "                         czaruj 'refresh' [postac]\n" +
            "Szkola:                  Przemiany\n" +
            "Nauczyciele:             [5321] druid Merak, [2508] druid Iverl\n" +
            "                         [34501] mlody druid\n" +
            "                         [1666] pobrudzona ksiega.";

        var parsed = AbilityHelpParser.Parse("refresh", rawText)!;

        Assert.Equal("REFRESH", parsed.Name);
        Assert.Equal("czar wspomagajacy", parsed.Type);
        Assert.Equal(2, parsed.AvailableForClasses.Count);
        Assert.Equal("kazda specjalizacja", parsed.WandererSpecialization);
        Assert.Equal("[postac]", parsed.Target);
        Assert.Equal("cast 'refresh' [postac], czaruj 'refresh' [postac]", parsed.Syntax);
        Assert.Equal("Przemiany", parsed.School);
        Assert.Equal(
            ["[5321] druid Merak", "[2508] druid Iverl", "[34501] mlody druid", "[1666] pobrudzona ksiega"],
            parsed.Teachers);
    }

    [Fact]
    public void Parse_SpellFields_SchoolAndSyntaxAndPolishEquivalent()
    {
        const string rawText =
            "Nazwa:                   KICK\n" +
            "Typ:                     skill aktywny\n" +
            "Cel:                     <postac>\n" +
            "Skladnia:                kick\n" +
            "Polski odpowiednik:      kopnij\n" +
            "Nauczyciele:             [530] paladyn Dyne.\n" +
            "\n" +
            "Opis.";

        var parsed = AbilityHelpParser.Parse("kick", rawText)!;

        Assert.Equal("<postac>", parsed.Target);
        Assert.Equal("kick", parsed.Syntax);
        Assert.Equal("kopnij", parsed.PolishEquivalent);
    }
}
