using MudClient.App.Models;
using MudClient.App.Services;
using Xunit;

namespace MudClient.App.Tests;

public sealed class SkillTrainerAnnotatorTests
{
    private static TeacherEntry Teacher(string name, params TeacherSkillEntry[] skills) =>
        new("1", name, "Region", null, "100", [], skills, []);

    [Fact]
    public void Annotate_CurrentValueWithinTeacherRange_AppendsTeacherName()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 0, 50, 0, 100)) };
        var line = "[WW]  axe                 10   3 + 0  ";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("axe                 10   3 + 0 (Mistrz Moran)", result);
    }

    [Fact]
    public void Annotate_BelowTeachersRequiredThreshold_DoesNotAppendThatTeacher()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 30, 50, 30, 100)) };
        var line = "[WW]  axe                 10   3 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Equal(line, result);
    }

    [Fact]
    public void Annotate_AtOrAboveTeachersMax_DoesNotAppendThatTeacher()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 0, 50, 0, 100)) };
        var line = "[WW]  axe                 10  50 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Equal(line, result);
    }

    [Fact]
    public void Annotate_TeacherWithUnboundedMax_AlwaysEligibleOnceRequirementMet()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 45, null, 45, 100)) };
        var line = "[WW]  axe                 10  99 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("(Mistrz Moran)", result);
    }

    [Fact]
    public void Annotate_MultipleTeachersEligible_PicksTheOneReachingFurthest()
    {
        var teachers = new[]
        {
            Teacher("Zorro", new TeacherSkillEntry("axe", 0, 50, 0, 100)),
            Teacher("Anna", new TeacherSkillEntry("axe", 0, 90, 0, 100)),
        };
        var line = "[WW]  axe                 10   3 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("(Anna)", result);
        Assert.DoesNotContain("Zorro", result);
    }

    [Fact]
    public void Annotate_TiedMaxAmongEligibleTeachers_BreaksTieByName()
    {
        var teachers = new[]
        {
            Teacher("Zorro", new TeacherSkillEntry("axe", 0, 50, 0, 100)),
            Teacher("Anna", new TeacherSkillEntry("axe", 0, 50, 0, 100)),
        };
        var line = "[WW]  axe                 10   3 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("(Anna)", result);
    }

    [Fact]
    public void Annotate_TwoSkillEntriesOnOneLine_AnnotatesBothIndependently()
    {
        var teachers = new[]
        {
            Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 0, 50, 0, 100)),
            Teacher("Instruktor drow", new TeacherSkillEntry("dagger", 0, 50, 0, 100)),
        };
        var line = "[WW]  axe                 10   3 + 0  [WW]  dagger              11   2 + 0  ";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("axe                 10   3 + 0 (Mistrz Moran)", result);
        Assert.Contains("dagger              11   2 + 0 (Instruktor drow)", result);
    }

    [Fact]
    public void Annotate_MultiWordSkillName_IsCapturedInFull()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("twohanded weapon", 0, 80, 0, 40)) };
        var line = "[WW]  twohanded weapon     0  73 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Contains("(Mistrz Moran)", result);
    }

    [Fact]
    public void Annotate_NoMatchingTeacher_LeavesLineUnchanged()
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("sword", 0, 50, 0, 100)) };
        var line = "[WW]  axe                 10   3 + 0";

        var result = SkillTrainerAnnotator.Annotate(line, teachers);

        Assert.Equal(line, result);
    }

    [Theory]
    [InlineData("Witaj w krainie Killer.")]
    [InlineData("")]
    [InlineData("Widzisz tutaj strażnika miasta.")]
    public void Annotate_LineWithoutSkillRow_LeavesLineUnchanged(string line)
    {
        var teachers = new[] { Teacher("Mistrz Moran", new TeacherSkillEntry("axe", 0, 50, 0, 100)) };

        Assert.Equal(line, SkillTrainerAnnotator.Annotate(line, teachers));
    }

    [Fact]
    public void Annotate_NoTeachersLoaded_LeavesLineUnchanged()
    {
        var line = "[WW]  axe                 10   3 + 0";

        Assert.Equal(line, SkillTrainerAnnotator.Annotate(line, []));
    }

    [Fact]
    public void FindBestTrainer_TeacherOffersSameSkillTwice_StillReturnsThemOnce()
    {
        var teacher = new TeacherEntry(
            "1", "Mistrz Moran", "Region", null, "100", [],
            [
                new TeacherSkillEntry("axe", 0, 30, 0, 100),
                new TeacherSkillEntry("axe", 30, 60, 30, 100),
            ],
            []);

        var name = SkillTrainerAnnotator.FindBestTrainer("axe", 10, [teacher]);

        Assert.Equal("Mistrz Moran", name);
    }

    [Fact]
    public void FindBestTrainer_NoEligibleTeacher_ReturnsNull()
    {
        var teacher = new TeacherEntry(
            "1", "Mistrz Moran", "Region", null, "100", [],
            [new TeacherSkillEntry("axe", 30, 50, 30, 100)],
            []);

        Assert.Null(SkillTrainerAnnotator.FindBestTrainer("axe", 10, [teacher]));
    }
}
