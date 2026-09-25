using MudClient.App.Services;

namespace MudClient.App.Tests;

public sealed class AbilityLearningMessageParserTests
{
    [Theory]
    [InlineData("Mlody druid uczy cie umiejetnosci 'herbs knowledge'.", AbilityLearningKind.SkillTrainingSucceeded, "herbs knowledge")]
    [InlineData("Nauczyciel uczy cie umiejetnosci 'riding'.", AbilityLearningKind.SkillTrainingSucceeded, "riding")]
    [InlineData("Nie udalo ci sie nauczyc zaklecia endure fire.", AbilityLearningKind.SpellTrainingFailed, "endure fire")]
    [InlineData("Mlody druid uczy cie zaklecia 'nature ally I'.", AbilityLearningKind.SpellTrainingSucceeded, "nature ally I")]
    [InlineData("Przepisujesz zaklecie 'bless' do swojego modlitewnika.", AbilityLearningKind.SpellTrainingSucceeded, "bless")]
    [InlineData("Przepisujesz zaklecie 'fireball' do swojej ksiegi czarow.", AbilityLearningKind.SpellTrainingSucceeded, "fireball")]
    [InlineData("Przepisujesz zaklecie 'nature ally I' do swojej ksiegi zaklec.", AbilityLearningKind.SpellTrainingSucceeded, "nature ally I")]
    [InlineData("Mlody druid uczy cie paru wskazowek do umiejetnosci 'twohanded weapon'.", AbilityLearningKind.SkillTeacherHints, "twohanded weapon")]
    [InlineData("Stajesz sie lepsza w umiejetnosci 'staff'.", AbilityLearningKind.SkillImproved, "staff")]
    public void Parse_ObservedLearningMessage_ReturnsItsMeaning(string line, AbilityLearningKind expectedKind, string expectedName)
    {
        var observation = AbilityLearningMessageParser.Parse(line);

        Assert.NotNull(observation);
        Assert.Equal(expectedKind, observation!.Kind);
        Assert.Equal(expectedName, observation.AbilityName);
    }
}
