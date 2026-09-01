using System.Globalization;
using System.Text.RegularExpressions;

namespace MudClient.Core.Statistics;

public enum ExperienceChangeKind
{
    Damage,
    KillReward,
    FleeLoss,
    DeathLoss,
    UnknownGain,
    UnknownLoss,
}

public sealed record ExperienceChange(
    ExperienceChangeKind Kind,
    long Amount,
    string? EnemyName,
    int Level,
    long? RemainingToLevel,
    DateTimeOffset When);

/// <summary>
/// Correlates Killer's plain-text messages with the numeric EXP-to-level prompt. Messages
/// identify the cause; the prompt delta remains the authority for the amount actually applied.
/// </summary>
public sealed partial class ExperienceTracker
{
    private long? _remaining;
    private long _pendingKillReward;
    private string? _pendingVictim;
    private ExperienceChangeKind? _pendingLoss;
    private bool _levelAdvanced;

    public int Level { get; set; }

    /// <summary>Current combat target supplied by GMCP, used for opponents whose death has no
    /// dedicated text line (for example some ghosts).</summary>
    public string? CurrentEnemyName { get; set; }

    public IReadOnlyList<ExperienceChange> ProcessLine(string line, DateTimeOffset? when = null)
    {
        var result = new List<ExperienceChange>();
        var text = StripAnsiRegex().Replace(line, string.Empty).Trim();
        var timestamp = when ?? DateTimeOffset.Now;

        if (TryReadVictim(text, out var victim))
        {
            _pendingVictim = victim;
        }

        var reward = RewardRegex().Match(text);
        if (reward.Success)
        {
            _pendingKillReward += long.Parse(reward.Groups[1].Value, CultureInfo.InvariantCulture);
            _pendingVictim ??= string.IsNullOrWhiteSpace(CurrentEnemyName)
                ? null
                : NormalizeEnemy(CurrentEnemyName);
        }

        if (text.Equals("Tracisz troszke punktow doswiadczenia.", StringComparison.OrdinalIgnoreCase))
        {
            _pendingLoss = ExperienceChangeKind.FleeLoss;
        }
        else if (text.Contains("Nie zyjesz, co za pech", StringComparison.OrdinalIgnoreCase))
        {
            _pendingLoss = ExperienceChangeKind.DeathLoss;
        }

        if (text.Contains("Zdobywasz poziom!", StringComparison.OrdinalIgnoreCase))
        {
            // The old threshold was reached between prompts. Count only the known remainder;
            // the following larger prompt is a new level's target, never an apparent loss.
            if (_remaining is > 0)
            {
                result.Add(NewChange(ExperienceChangeKind.Damage, _remaining.Value, null, 0, timestamp));
            }

            _levelAdvanced = true;
            Level++;
        }

        var prompt = PromptRegex().Match(text);
        if (!prompt.Success)
        {
            return result;
        }

        var current = long.Parse(prompt.Groups[1].Value, CultureInfo.InvariantCulture);
        if (_remaining is null || _levelAdvanced)
        {
            _remaining = current;
            _levelAdvanced = false;
            _pendingKillReward = 0;
            _pendingVictim = null;
            _pendingLoss = null;
            return result;
        }

        var gained = _remaining.Value - current;
        if (gained > 0)
        {
            var kill = Math.Min(gained, _pendingKillReward);
            var damage = gained - kill;
            if (damage > 0)
            {
                result.Add(NewChange(ExperienceChangeKind.Damage, damage, _pendingVictim, current, timestamp));
            }
            if (kill > 0)
            {
                result.Add(NewChange(ExperienceChangeKind.KillReward, kill, _pendingVictim, current, timestamp));
            }
        }
        else if (gained < 0)
        {
            result.Add(NewChange(_pendingLoss ?? ExperienceChangeKind.UnknownLoss, -gained, null, current, timestamp));
        }
        // A zero delta deliberately creates no loss: low-level protection can emit the flee/death
        // message without changing EXP, as seen in captured traffic.

        _remaining = current;
        _pendingKillReward = 0;
        _pendingVictim = null;
        _pendingLoss = null;
        return result;
    }

    private ExperienceChange NewChange(ExperienceChangeKind kind, long amount, string? enemy, long remaining, DateTimeOffset when) =>
        new(kind, amount, enemy, Level, remaining, when);

    private static bool TryReadVictim(string text, out string victim)
    {
        foreach (var regex in new[] { DeathRegex(), FallsDeadRegex(), SkeletonDeathRegex() })
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                victim = NormalizeEnemy(match.Groups[1].Value);
                return true;
            }
        }

        victim = string.Empty;
        return false;
    }

    private static string NormalizeEnemy(string value) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Trim().ToLower(CultureInfo.CurrentCulture));

    [GeneratedRegex("\\x1B\\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled)]
    private static partial Regex StripAnsiRegex();

    [GeneratedRegex("^<[-0-9]+(?:/[-0-9]+)?hp\\s+([0-9]+)\\s", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PromptRegex();

    [GeneratedRegex("^Zdobyles\\s+([0-9]+)\\s+punkt(?:ow|y)?\\s+doswiadczenia\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RewardRegex();

    [GeneratedRegex("^(.+?) nie zyje!!$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DeathRegex();

    [GeneratedRegex("^(.+?) pada na ziemie\\.\\.\\. MARTWY\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FallsDeadRegex();

    [GeneratedRegex("^(.+?) rozpada sie na kawaleczki\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SkeletonDeathRegex();
}
