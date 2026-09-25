using System.Text.RegularExpressions;
using MudClient.Core.Text;

namespace MudClient.App.Services;

/// <summary>Recognizes observed KillerMUD learning-result lines.</summary>
public static partial class AbilityLearningMessageParser
{
    public static AbilityLearningObservation? Parse(string line)
    {
        var plain = AnsiText.StripAnsi(line).Trim();

        if (SpellTrainingFailed().Match(plain) is { Success: true } failedSpell)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SpellTrainingFailed, failedSpell.Groups["name"].Value.Trim());
        }

        if (SpellTrainingSucceeded().Match(plain) is { Success: true } learnedSpell)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SpellTrainingSucceeded, learnedSpell.Groups["name"].Value.Trim());
        }

        if (SpellCopiedSucceeded().Match(plain) is { Success: true } copiedSpell)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SpellTrainingSucceeded, copiedSpell.Groups["name"].Value.Trim());
        }

        if (SkillTeacherHints().Match(plain) is { Success: true } hintedSkill)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SkillTeacherHints, hintedSkill.Groups["name"].Value.Trim());
        }

        if (SkillTrainingSucceeded().Match(plain) is { Success: true } learnedSkill)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SkillTrainingSucceeded, learnedSkill.Groups["name"].Value.Trim());
        }

        if (SkillImproved().Match(plain) is { Success: true } improvedSkill)
        {
            return new AbilityLearningObservation(AbilityLearningKind.SkillImproved, improvedSkill.Groups["name"].Value.Trim());
        }

        return null;
    }

    [GeneratedRegex("^Nie uda[łl]o ci si[eę] nauczy[cć] zakl[eę]cia (?<name>.+?)\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SpellTrainingFailed();

    [GeneratedRegex("^.+? uczy ci[eę] zakl[eę]cia '(?<name>[^']+)'\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SpellTrainingSucceeded();

    [GeneratedRegex("^Przepisujesz zakl[eę]cie '(?<name>[^']+)' do (?:swojego modlitewnika|swojej ksi[eę]gi czar[oó]w|swojej ksi[eę]gi zakle[cć])\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SpellCopiedSucceeded();

    [GeneratedRegex("^.+? uczy ci[eę] paru wskaz[oó]wek do umiej[eę]tno[sś]ci '(?<name>[^']+)'\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SkillTeacherHints();

    [GeneratedRegex("^.+? uczy ci[eę] umiej[eę]tno[sś]ci '(?<name>[^']+)'\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SkillTrainingSucceeded();

    [GeneratedRegex("^Stajesz si[eę] lepsz(?:y|a|e) w umiej[eę]tno[sś]ci '(?<name>[^']+)'\\.$", RegexOptions.IgnoreCase)]
    private static partial Regex SkillImproved();
}

public enum AbilityLearningKind
{
    SkillTrainingSucceeded,
    SpellTrainingSucceeded,
    SpellTrainingFailed,
    SkillTeacherHints,
    SkillImproved,
}

public sealed record AbilityLearningObservation(AbilityLearningKind Kind, string AbilityName);
