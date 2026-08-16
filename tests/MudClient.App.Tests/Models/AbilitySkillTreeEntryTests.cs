using MudClient.App.Models;
using MudClient.Core.Killeropedia;

namespace MudClient.App.Tests.Models;

public sealed class AbilitySkillTreeEntryTests
{
    private static AbilityCaptureEntry MakeEntry(
        string? wandererSpecialization,
        params (string ClassName, int MinLevel)[] classLevels) => new()
    {
        Name = "smite evil",
        AvailableForClasses = classLevels
            .Select(entry => new ClassLevelRequirement(entry.ClassName, entry.MinLevel))
            .ToList(),
        WandererSpecialization = wandererSpecialization,
    };

    // ====================================================================
    // "kazda specjalizacja" — owned regardless of browsed class
    // ====================================================================

    [Fact]
    public void Create_AnySpecialization_OwnedWhenBrowsingBaseWanderer()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 4), ("Wedrowiec", 4));

        var item = AbilitySkillTreeEntry.Create(entry, "Wedrowiec");

        Assert.NotNull(item);
        Assert.True(item!.IsOwned);
        Assert.Equal(1.0, item.RowOpacity);
    }

    [Fact]
    public void Create_AnySpecialization_OwnedWhenBrowsingOtherClass()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 4), ("Wedrowiec", 4));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn");

        Assert.NotNull(item);
        Assert.True(item!.IsOwned);
    }

    // ====================================================================
    // Specialization-gated abilities — excluded from the base Wędrowiec view, shown as an
    // un-owned preview only while browsing the matching class
    // ====================================================================

    [Fact]
    public void Create_SpecificSpecialization_ExcludedWhenBrowsingBaseWanderer()
    {
        // "aura of protection"-style: Paladyn (13 lvl), Wedrowiec (1 lvl), Specjalizacja: Paladyn —
        // a Wędrowiec who hasn't chosen a specialization yet doesn't have this, so it shouldn't
        // clutter the base "what I own" view at all.
        var entry = MakeEntry("Paladyn", ("Paladyn", 13), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Wedrowiec");

        Assert.Null(item);
    }

    [Fact]
    public void Create_SpecificSpecialization_PreviewedAsUnownedWhenBrowsedClassMatches()
    {
        var entry = MakeEntry("Paladyn", ("Paladyn", 13), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn");

        Assert.NotNull(item);
        Assert.False(item!.IsOwned);
        Assert.Equal(0.55, item.RowOpacity);
        Assert.Contains("Paladyn", item.WandererAvailabilityText);
    }

    [Fact]
    public void Create_SpecificSpecialization_ExcludedWhenBrowsedClassDoesNotMatch()
    {
        var entry = MakeEntry("Paladyn", ("Paladyn", 13), ("Wojownik", 13), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Wojownik");

        Assert.Null(item);
    }

    [Fact]
    public void Create_CommaSeparatedSpecializationList_MatchesAnyListedClass()
    {
        var entry = MakeEntry("Wojownik, Paladyn", ("Paladyn", 20), ("Wojownik", 20), ("Wedrowiec", 20));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn");

        Assert.NotNull(item);
        Assert.False(item!.IsOwned);
    }

    // ====================================================================
    // Wędrowiec entirely absent from the game's class list
    // ====================================================================

    [Fact]
    public void Create_WandererNotInClassList_AlwaysExcluded()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 4));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn");

        Assert.Null(item);
    }

    // ====================================================================
    // Level lookups per browsed class
    // ====================================================================

    [Fact]
    public void Create_BrowsedClassLevel_ReadFromMatchingClassEntry()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 8), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn")!;

        Assert.Equal(8, item.BrowsedClassLevel);
        Assert.Equal(1, item.WandererLevel);
    }

    [Fact]
    public void Create_LevelSummaryText_OmitsWandererPartWhenBrowsingBaseWanderer()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 8), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Wedrowiec")!;

        Assert.Equal("Wedrowiec: 1 lvl", item.LevelSummaryText);
    }

    [Fact]
    public void Create_LevelSummaryText_IncludesBothLevelsWhenBrowsingOtherClass()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Paladyn", 8), ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Paladyn")!;

        Assert.Equal("Paladyn: 8 lvl • Wędrowiec: 1 lvl", item.LevelSummaryText);
    }

    // ====================================================================
    // SpellCircle — looked up from the hand-curated seed catalog, by name, regardless of
    // browsed class
    // ====================================================================

    [Fact]
    public void Create_SpellNameInSeedCatalog_PicksUpItsCircle()
    {
        var source = new AbilityCaptureEntry
        {
            Name = "sanctuary",
            WandererSpecialization = "kazda specjalizacja",
            AvailableForClasses = [new ClassLevelRequirement("Wedrowiec", 19)],
        };

        var item = AbilitySkillTreeEntry.Create(source, "Wedrowiec")!;

        Assert.Equal(5, item.SpellCircle);
        Assert.Contains("Krąg 5", item.LevelSummaryText);
    }

    [Fact]
    public void Create_NameNotInSeedCatalog_LeavesCircleNull()
    {
        var entry = MakeEntry("kazda specjalizacja", ("Wedrowiec", 1));

        var item = AbilitySkillTreeEntry.Create(entry, "Wedrowiec")!;

        Assert.Null(item.SpellCircle);
        Assert.False(item.HasSpellCircle);
        Assert.DoesNotContain("Krąg", item.LevelSummaryText);
    }
}
