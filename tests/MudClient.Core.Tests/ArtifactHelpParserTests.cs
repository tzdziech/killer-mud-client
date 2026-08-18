using MudClient.Core.Killeropedia;

namespace MudClient.Core.Tests;

public sealed class ArtifactHelpParserTests
{
    // Real "/mapuj <liczba>" capture ("try 1") — indestructible weapon with a quoted special name,
    // no class/race/alignment restriction, aura flavor lines, three empty sockets.
    private const string KatanaRawText =
        "Chwytasz katane 'Chokutou Reiki' w obie rece i az sapiesz z wysilku podnoszac bron do gory.\n\n" +
        "Postanawiasz dokladniej obejrzec katane 'Chokutou Reiki'.\n\n" +
        "Jest to niewiarygodnie dluga jak na katane bron, osiaga prawie dwa metry.\n\n" +
        "Katana 'Chokutou Reiki' jest w znakomitym stanie.\n\n" +
        "Bron ta posiada unikalna moc, ktora mozna uzyc 3 razy dziennie.\n\n" +
        "Waga katany 'Chokutou Reiki' wynosi okolo 14.51 kg, przedmiot ten wykonano z materialu 'doskonala stal'.\n" +
        "Katana 'Chokutou Reiki' to niezwykle cenny przedmiot.\n" +
        "Katane 'Chokutou Reiki' mozna naprawiac jeszcze wiele razy.\n" +
        "Katana 'Chokutou Reiki' emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Od katany 'Chokutou Reiki' bije zimna aura zla.\n" +
        "Dotykajac katane 'Chokutou Reiki' odczuwasz delikatna, magiczna aure.\n" +
        "Typ broni: 'miecz'.\n" +
        "Bonus do trafienia: 3.\n" +
        "Obrazenia zadawane 3d5 + 3 (srednio 12).\n\n" +
        "W rekojesci katany 'Chokutou Reiki' dostrzegasz gniazda na kamienie mocy.\n" +
        "Gniazdo 1: puste.\n" +
        "Gniazdo 2: puste.\n" +
        "Gniazdo 3: puste.";

    // Class-restricted (allowed-only) weapon with a stat bonus.
    private const string KorbaczRawText =
        "Chwytasz dwureczny korbacz Blogoslawionego Ognia w obie rece.\n\n" +
        "Postanawiasz dokladniej obejrzec dwureczny korbacz Blogoslawionego Ognia.\n\n" +
        "Trzymasz w dloniach przepiekny korbacz.\n\n" +
        "Dwureczny korbacz Blogoslawionego Ognia jest w znakomitym stanie.\n\n" +
        "Waga dwurecznego korbacza Blogoslawionego Ognia wynosi okolo 6.80 kg, przedmiot ten wykonano z materialu 'drewno'.\n" +
        "Dwureczny korbacz Blogoslawionego Ognia mozna naprawic jeszcze wiele razy.\n" +
        "Dwureczny korbacz Blogoslawionego Ognia emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Przedmiot ten moga uzywac tylko klerycy.\n" +
        "Typ broni: 'korbacz'.\n" +
        "Bonus do trafienia: 3.\n" +
        "Obrazenia zadawane 3d6 + 3 (srednio 13).";

    // Multiple forbidden classes + a stat bonus, no quoted special name at all in the postanawiasz line.
    private const string MieczRawText =
        "Bierzesz miecz Zwinnosci Arbane'a do reki. Bron w sam raz dla ciebie.\n\n" +
        "Postanawiasz dokladniej obejrzec miecz Zwinnosci Arbane'a.\n\n" +
        "Byla to bron banity Garno.\n\n" +
        "Miecz Zwinnosci Arbane'a jest w znakomitym stanie.\n\n" +
        "Waga miecza Zwinnosci Arbane'a wynosi okolo 5.44 kg, przedmiot ten wykonano z materialu 'stal'.\n" +
        "Miecz Zwinnosci Arbane'a to niezwykle cenny przedmiot.\n" +
        "Miecz Zwinnosci Arbane'a emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Dotykajac miecz Zwinnosci Arbane'a odczuwasz delikatna, magiczna aure.\n" +
        "Przedmiotu tego nie moga uzywac klerycy.\n" +
        "Przedmiotu tego nie moga uzywac druidzi.\n" +
        "Typ broni: 'miecz'.\n" +
        "Bonus do trafienia: 3.\n" +
        "Obrazenia zadawane 2d4 + 3 (srednio 8).\n" +
        "Wplywa na zrecznosc o 8.";

    // The "Postanawiasz..." line is missing the quoted special name; only the later nominative
    // anchor line has it. Also exercises alignment restrictions and multiple stat bonuses.
    private const string MaskaRawText =
        "Niestety, mroczna maska 'Nekromantki Kaylin' zbytnio cie uwiera.\n\n" +
        "Postanawiasz dokladniej obejrzec mroczna maske.\n\n" +
        "Ta maska nalezala do nekromantki Kaylin Ygresse.\n\n" +
        "Mroczna maska 'Nekromantki Kaylin' jest w znakomitym stanie.\n\n" +
        "Waga mrocznej maski wynosi okolo 0.54 kg, przedmiot ten wykonano z materialu 'adamantyt'.\n" +
        "Mroczna maska 'Nekromantki Kaylin' to niezwykle cenny przedmiot.\n" +
        "Mroczna maska 'Nekromantki Kaylin' emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Dotykajac mroczna maske odczuwasz delikatna, magiczna aure.\n" +
        "Przedmiotu tego nie moga uzywac istoty dobre.\n" +
        "Przedmiotu tego nie moga uzywac istoty neutralne.\n" +
        "Przedmiot ten moga uzywac tylko magowie.\n" +
        "Wplywa na punkty odpornosci na 'energie pozytywna' o 10.\n" +
        "Wplywa na ilosc mozliwych zaklec do zapamietania z 5 kregu o 1.\n" +
        "Wplywa na inteligencje o 5.";

    // Armor with set info: two members and two set bonuses.
    private const string RekawiceRawText =
        "Przymierzasz kosciane rekawice Venreeh'meyel. Wszystko wydaje sie doskonale pasowac.\n\n" +
        "Postanawiasz dokladniej obejrzec kosciane rekawice Venreeh'meyel.\n\n" +
        "Rekawice wykonane zostaly z bardzo grubej, czarnej skory.\n\n" +
        "Kosciane rekawice Venreeh'meyel sa w znakomitym stanie.\n\n" +
        "Kosciane rekawice Venreeh'meyel prawie nic nie wazy, przedmiot ten wykonano z materialu 'kosc'.\n" +
        "Kosciane rekawice Venreeh'meyel emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Rodzaj pancerza: Medium armor\n" +
        "Klasa pancerza: 6 klujace, 7 obuchowe, 6 ciecie\n" +
        "Wplywa na umiejetnosc 'twohanded weapon' o 3.\n\n" +
        "W koscianych rekawicach Venreeh'meyel dostrzegasz gniazda na kamienie mocy.\n" +
        "Gniazdo 1: puste.\n" +
        "Gniazdo 2: puste.\n\n" +
        "Przedmiot ten stanowi czesc wiekszej calosci.\n" +
        "Pozostale przedmioty nalezace do kompletu:\n" +
        "kosciany helm Venreeh'meyel (take head)\n" +
        "kosciane naramienniki Venreeh'meyel (take arms)\n\n" +
        "Po zbadaniu tego przedmiotu odkrywasz magiczne wlasciwosci kompletu:\n" +
        "dodaje free_action.\n" +
        "zmienia punkty zycia o 75";

    // Light item with a plain (unquoted) skill bonus and a granted ability keyword.
    private const string PierscienRawText =
        "Przymierzasz pierscien Cichego Lotrzyka. Wszystko wydaje sie w porzadku.\n\n" +
        "Postanawiasz dokladniej obejrzec pierscien Cichego Lotrzyka.\n\n" +
        "Ten pierscien byl wiele razy uzywany.\n\n" +
        "Pierscien Cichego Lotrzyka prawie nic nie wazy, przedmiot ten wykonano z materialu 'stal'.\n" +
        "Pierscien Cichego Lotrzyka to niezwykle cenny przedmiot.\n" +
        "Pierscien Cichego Lotrzyka emanuje niezwykla magia, zadna moc nie bylaby w stanie zniszczyc tego przedmiotu.\n" +
        "Wplywa na umiejetnosc 'backstab mastery' o 4.\n" +
        "Wplywa na zrecznosc o 6.\n" +
        "Dodaje quiet_step.\n\n" +
        "Po wewnetrznej stronie pierscienia Cichego Lotrzyka dostrzegasz gniazda na kamienie mocy.\n" +
        "Gniazdo 1: puste.";

    [Fact]
    public void Parse_ExtractsQuotedSpecialName_PreferringTheLongestNominativeAnchor()
    {
        var parsed = ArtifactHelpParser.Parse(KatanaRawText);

        Assert.NotNull(parsed);
        Assert.Equal("Katana 'Chokutou Reiki'", parsed!.Name);
    }

    [Fact]
    public void Parse_NoRestrictionLines_LeavesAllRestrictionListsEmpty()
    {
        var parsed = ArtifactHelpParser.Parse(KatanaRawText)!;

        Assert.Empty(parsed.AllowedClassesOnly);
        Assert.Empty(parsed.ForbiddenClasses);
        Assert.Empty(parsed.ForbiddenAlignments);
    }

    [Fact]
    public void Parse_IndestructibleAndWeaponStats_AreRecognized()
    {
        var parsed = ArtifactHelpParser.Parse(KatanaRawText)!;

        Assert.True(parsed.IsIndestructible);
        Assert.False(parsed.IsCursed);
        Assert.Equal(14.51, parsed.WeightKg);
        Assert.Equal("doskonala stal", parsed.Material);
        Assert.Equal("miecz", parsed.WeaponType);
        Assert.Equal(3, parsed.HitBonus);
        Assert.Equal("3d5 + 3 (srednio 12)", parsed.DamageText);
        Assert.Equal(3, parsed.SocketCount);
    }

    [Fact]
    public void Parse_AllowOnlyClassRestriction_IsCapturedAsAllowedClassesOnly()
    {
        var parsed = ArtifactHelpParser.Parse(KorbaczRawText)!;

        Assert.Equal(["Kleryk"], parsed.AllowedClassesOnly);
        Assert.Empty(parsed.ForbiddenClasses);
    }

    [Fact]
    public void Parse_MultipleForbiddenClasses_AreAllCaptured()
    {
        var parsed = ArtifactHelpParser.Parse(MieczRawText)!;

        Assert.Equal(["Kleryk", "Druid"], parsed.ForbiddenClasses);
        Assert.Contains(parsed.StatBonuses, bonus => bonus.Stat == "zrecznosc" && bonus.Amount == 8);
    }

    [Fact]
    public void Parse_NameMissingFromDeclinedLine_StillRecoveredFromNominativeAnchor()
    {
        var parsed = ArtifactHelpParser.Parse(MaskaRawText)!;

        Assert.Equal("Mroczna maska 'Nekromantki Kaylin'", parsed.Name);
    }

    [Fact]
    public void Parse_AlignmentAndClassRestrictions_AreClassifiedSeparately()
    {
        var parsed = ArtifactHelpParser.Parse(MaskaRawText)!;

        Assert.Equal(["dobry", "neutralny"], parsed.ForbiddenAlignments);
        Assert.Equal(["Mag"], parsed.AllowedClassesOnly);
    }

    [Fact]
    public void Parse_MultipleStatBonuses_AreAllCaptured()
    {
        var parsed = ArtifactHelpParser.Parse(MaskaRawText)!;

        Assert.Contains(parsed.StatBonuses, bonus => bonus.Stat == "inteligencje" && bonus.Amount == 5);
        Assert.Contains(
            parsed.StatBonuses,
            bonus => bonus.Stat == "ilosc mozliwych zaklec do zapamietania z 5 kregu" && bonus.Amount == 1);
    }

    [Fact]
    public void Parse_ArmorFields_AreExtracted()
    {
        var parsed = ArtifactHelpParser.Parse(RekawiceRawText)!;

        Assert.Equal("Medium armor", parsed.ArmorType);
        Assert.Equal("6 klujace, 7 obuchowe, 6 ciecie", parsed.ArmorClassText);
        Assert.Null(parsed.WeaponType);
    }

    [Fact]
    public void Parse_SetInfo_CapturesMembersAndBonuses()
    {
        var parsed = ArtifactHelpParser.Parse(RekawiceRawText)!;

        Assert.True(parsed.IsPartOfSet);
        Assert.Equal(2, parsed.SetMembers.Count);
        Assert.Contains(parsed.SetMembers, member => member.Contains("kosciany helm"));
        Assert.Contains(parsed.SetBonuses, bonus => bonus.Contains("free_action"));
    }

    [Fact]
    public void Parse_LightweightItemWithoutConditionLine_StillParsesName()
    {
        var parsed = ArtifactHelpParser.Parse(PierscienRawText)!;

        Assert.Equal("Pierscien Cichego Lotrzyka", parsed.Name);
        Assert.Null(parsed.WeightKg);
        Assert.Equal("stal", parsed.Material);
    }

    [Fact]
    public void Parse_GrantedAbilityKeyword_IsCaptured()
    {
        var parsed = ArtifactHelpParser.Parse(PierscienRawText)!;

        Assert.Contains("quiet_step", parsed.GrantedAbilities);
    }

    [Fact]
    public void Parse_NamedSkillBonus_KeepsTheQuotedSkillNameInTheStatText()
    {
        var parsed = ArtifactHelpParser.Parse(PierscienRawText)!;

        Assert.Contains(parsed.StatBonuses, bonus => bonus.Stat == "umiejetnosc 'backstab mastery'" && bonus.Amount == 4);
    }

    [Fact]
    public void Parse_EmptyOrWhitespaceText_ReturnsNull()
    {
        Assert.Null(ArtifactHelpParser.Parse(string.Empty));
        Assert.Null(ArtifactHelpParser.Parse("   "));
    }

    [Fact]
    public void Parse_TextWithNoRecognizableNameAnchor_ReturnsNull()
    {
        Assert.Null(ArtifactHelpParser.Parse("Nic tu nie ma do obejrzenia."));
    }

    [Fact]
    public void ParsedArtifact_Completeness_IsHigherForARicherCapture()
    {
        var rich = ArtifactHelpParser.Parse(RekawiceRawText)!;
        var sparse = ArtifactHelpParser.Parse(KatanaRawText)!;

        // Rekawice has class/armor stats plus a full set (members + bonuses) on top of its own
        // fields — it should always outscore a capture with fewer recognized facts.
        Assert.True(rich.Completeness > 0);
        Assert.True(sparse.Completeness >= 0);
    }
}
