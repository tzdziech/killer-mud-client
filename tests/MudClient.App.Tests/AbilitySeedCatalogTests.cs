using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class AbilitySeedCatalogTests
{
    [Fact]
    public void Find_KnownClass_IsCaseInsensitiveAndTrimmed()
    {
        var lower = AbilitySeedCatalog.Find("paladyn");
        var upper = AbilitySeedCatalog.Find("PALADYN");
        var padded = AbilitySeedCatalog.Find("  Paladyn  ");

        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.NotNull(padded);
        Assert.Equal("Paladyn", lower!.Class);
    }

    [Fact]
    public void Find_UnknownClass_ReturnsNull()
    {
        Assert.Null(AbilitySeedCatalog.Find("wedrowiec"));
        Assert.Null(AbilitySeedCatalog.Find(""));
    }

    [Fact]
    public void Paladyn_HasNoDuplicateSkillOrSpellNames()
    {
        var seed = AbilitySeedCatalog.Find("Paladyn")!;

        var duplicateSkills = seed.Skills.GroupBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var duplicateSpells = seed.Spells.GroupBy(spell => spell.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

        Assert.Empty(duplicateSkills);
        Assert.Empty(duplicateSpells);
    }

    [Fact]
    public void Paladyn_EverySkillHasAPositiveMinLevel()
    {
        var seed = AbilitySeedCatalog.Find("Paladyn")!;

        Assert.All(seed.Skills, skill => Assert.True(skill.MinLevel > 0, skill.Name));
    }

    [Fact]
    public void Paladyn_EverySpellHasACircleBetweenOneAndFive()
    {
        var seed = AbilitySeedCatalog.Find("Paladyn")!;

        Assert.All(seed.Spells, spell => Assert.InRange(spell.Circle, 1, 5));
    }

    [Fact]
    public void Paladyn_AllNames_CombinesSkillsThenSpellsWithNoBlanks()
    {
        var seed = AbilitySeedCatalog.Find("Paladyn")!;

        var names = seed.AllNames;

        Assert.Equal(seed.Skills.Count + seed.Spells.Count, names.Count);
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(seed.Skills[0].Name, names[0]);
        Assert.Equal(seed.Spells[0].Name, names[seed.Skills.Count]);
    }

    [Fact]
    public void Paladyn_WeaponMasteryEntries_ShareTheSameNote()
    {
        var seed = AbilitySeedCatalog.Find("Paladyn")!;

        var masteries = seed.Skills.Where(skill => skill.Name.EndsWith("mastery", StringComparison.Ordinal)
            && skill.MinLevel == 20).ToArray();

        Assert.Equal(3, masteries.Length);
        Assert.All(masteries, skill => Assert.False(string.IsNullOrWhiteSpace(skill.Note)));
        Assert.Single(masteries.Select(skill => skill.Note).Distinct());
    }

    [Fact]
    public void KnownClasses_ContainsPaladyn()
    {
        Assert.Contains("Paladyn", AbilitySeedCatalog.KnownClasses);
    }
}
