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
    private static readonly TimeSpan RoomPeopleCorrelationWindow = TimeSpan.FromSeconds(5);

    private long? _remaining;
    private long _pendingKillReward;
    private string? _pendingVictim;
    private DateTimeOffset? _pendingKillRewardWhen;
    private ExperienceChangeKind? _pendingLoss;
    private bool _levelAdvanced;
    private string? _currentEnemyName;
    private HashSet<string> _lastCombatOpponents = new(StringComparer.OrdinalIgnoreCase);
    private string? _recentlyDisappearedVictim;
    private DateTimeOffset? _recentlyDisappearedWhen;
    private readonly List<ExperienceChange> _unresolvedKills = [];
    private int _level;

    public int Level
    {
        get => _level;
        set
        {
            if (_level > 0 && value > _level)
            {
                _levelAdvanced = true;
            }

            _level = value;
        }
    }

    /// <summary>Current combat target supplied by GMCP. This is used for damage attribution;
    /// kill rewards without an explicit death line are correlated through Room.People instead.</summary>
    public string? CurrentEnemyName
    {
        get => _currentEnemyName;
        set => _currentEnemyName = value;
    }

    /// <summary>
    /// Observes canonical room occupants and the subset currently fighting the local character.
    /// A single combat opponent which disappears from the room can be paired with a nearby kill
    /// reward. Both GMCP-before-text and text-before-GMCP ordering are supported.
    /// </summary>
    public IReadOnlyList<ExperienceChange> ObserveRoomPeople(
        IEnumerable<string> visibleNames,
        IEnumerable<string> combatOpponentNames,
        DateTimeOffset? when = null)
    {
        var timestamp = when ?? DateTimeOffset.Now;
        var result = DrainExpiredUnresolvedKills(timestamp);
        var visible = visibleNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(NormalizeEnemy)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currentOpponents = combatOpponentNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(NormalizeEnemy)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var disappeared = _lastCombatOpponents
            .Where(name => !visible.Contains(name))
            .ToArray();

        _lastCombatOpponents = currentOpponents;
        if (disappeared.Length == 0)
        {
            return result;
        }

        _recentlyDisappearedVictim = disappeared.Length == 1 ? disappeared[0] : null;
        _recentlyDisappearedWhen = timestamp;

        if (_pendingKillReward > 0 && _pendingVictim is null &&
            IsWithinCorrelationWindow(_pendingKillRewardWhen, timestamp))
        {
            _pendingVictim = _recentlyDisappearedVictim;
        }

        if (_recentlyDisappearedVictim is { } victim)
        {
            var pendingIndex = _unresolvedKills.FindIndex(change =>
                IsWithinCorrelationWindow(change.When, timestamp));
            if (pendingIndex >= 0)
            {
                result.Add(_unresolvedKills[pendingIndex] with { EnemyName = victim });
                _unresolvedKills.RemoveAt(pendingIndex);
            }
        }

        return result;
    }

    public IReadOnlyList<ExperienceChange> ProcessLine(string line, DateTimeOffset? when = null)
    {
        var text = StripAnsiRegex().Replace(line, string.Empty).Trim();
        var timestamp = when ?? DateTimeOffset.Now;
        var result = DrainExpiredUnresolvedKills(timestamp);

        if (TryReadVictim(text, out _))
        {
            _pendingVictim ??= _lastCombatOpponents.Count == 1
                ? _lastCombatOpponents.Single()
                : null;
        }

        var reward = RewardRegex().Match(text);
        if (reward.Success)
        {
            _pendingKillReward += long.Parse(reward.Groups[1].Value, CultureInfo.InvariantCulture);
            _pendingKillRewardWhen ??= timestamp;
            _pendingVictim ??= ResolveRecentlyDisappearedVictim(timestamp);
            _pendingVictim ??= _lastCombatOpponents.Count == 1
                ? _lastCombatOpponents.Single()
                : null;
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
                result.Add(NewChange(ExperienceChangeKind.Damage, _remaining.Value, ResolveCurrentVictim(), 0, timestamp));
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
            _pendingKillRewardWhen = null;
            _pendingLoss = null;
            _recentlyDisappearedVictim = null;
            _recentlyDisappearedWhen = null;
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
                var killChange = NewChange(
                    ExperienceChangeKind.KillReward, kill, _pendingVictim, current, timestamp);
                if (string.IsNullOrWhiteSpace(killChange.EnemyName) && _lastCombatOpponents.Count > 1)
                {
                    _unresolvedKills.Add(killChange);
                }
                else
                {
                    result.Add(killChange);
                }
            }
        }
        else if (gained < 0 && _pendingLoss is { } loss)
        {
            result.Add(NewChange(loss, -gained, null, current, timestamp));
        }
        // An otherwise unexplained increase of EXP remaining is a new baseline. In particular,
        // this happens when the next level's much larger threshold reaches the prompt before the
        // textual or GMCP level-up notification. Only an explicit flee/death message is a loss.
        // A zero delta deliberately creates no loss: low-level protection can emit the flee/death
        // message without changing EXP, as seen in captured traffic.

        _remaining = current;
        _pendingKillReward = 0;
        _pendingVictim = null;
        _pendingKillRewardWhen = null;
        _pendingLoss = null;
        _recentlyDisappearedVictim = null;
        _recentlyDisappearedWhen = null;
        return result;
    }

    private string? ResolveCurrentVictim() => !string.IsNullOrWhiteSpace(_pendingVictim)
        ? _pendingVictim
        : !string.IsNullOrWhiteSpace(_currentEnemyName)
            ? NormalizeEnemy(_currentEnemyName)
            : null;

    private string? ResolveRecentlyDisappearedVictim(DateTimeOffset timestamp) =>
        IsWithinCorrelationWindow(_recentlyDisappearedWhen, timestamp)
            ? _recentlyDisappearedVictim
            : null;

    private static bool IsWithinCorrelationWindow(DateTimeOffset? first, DateTimeOffset second) =>
        first is { } value && (second - value).Duration() <= RoomPeopleCorrelationWindow;

    private List<ExperienceChange> DrainExpiredUnresolvedKills(DateTimeOffset timestamp)
    {
        var expired = _unresolvedKills
            .Where(change => timestamp - change.When > RoomPeopleCorrelationWindow)
            .ToList();
        if (expired.Count > 0)
        {
            _unresolvedKills.RemoveAll(expired.Contains);
        }

        return expired;
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

    [GeneratedRegex("^Zdobyl(?:es|as)\\s+([0-9]+)\\s+punkt(?:ow|y)?\\s+doswiadczenia\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RewardRegex();

    [GeneratedRegex("^(.+?) nie zyje!!$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DeathRegex();

    [GeneratedRegex("^(.+?) pada na ziemie\\.\\.\\. MARTWY\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex FallsDeadRegex();

    [GeneratedRegex("^(.+?) rozpada sie na kawaleczki\\.$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SkeletonDeathRegex();
}
