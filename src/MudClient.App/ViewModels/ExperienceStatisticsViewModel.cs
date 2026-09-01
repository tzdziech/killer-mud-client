using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MudClient.App.Models;
using MudClient.Core.Statistics;

namespace MudClient.App.ViewModels;

public sealed partial class ExperienceStatisticsViewModel : ObservableObject
{
    private ExperienceStatisticsData _data = new();
    private ExperienceSessionData _session = new();

    public ObservableCollection<MobExperienceSummary> Mobs { get; } = [];
    public ObservableCollection<SessionExperienceSummary> History { get; } = [];

    [ObservableProperty] private long _gainedExperience;
    [ObservableProperty] private long _damageExperience;
    [ObservableProperty] private long _killExperience;
    [ObservableProperty] private long _lostExperience;
    [ObservableProperty] private long _fleeLoss;
    [ObservableProperty] private long _deathLoss;
    [ObservableProperty] private int _killCount;
    [ObservableProperty] private string _lastEnemy = "—";
    [ObservableProperty] private long _lastKillExperience;
    [ObservableProperty] private long? _remainingToLevel;
    [ObservableProperty] private string _analysis = "Zbieram dane o przeciwnikach…";

    public DateTimeOffset SessionStartedAt => _session.StartedAt;
    public TimeSpan SessionDuration => DateTimeOffset.Now - _session.StartedAt;
    public long NetExperience => GainedExperience - LostExperience;
    public double ExperiencePerHour => SessionDuration.TotalHours <= 0 ? 0 : NetExperience / SessionDuration.TotalHours;
    public double AveragePerKill => KillCount == 0 ? 0 : (double)KillExperience / KillCount;
    public double? LastKillProgressPercent => RemainingToLevel is > 0
        ? 100d * LastKillExperience / (RemainingToLevel.Value + LastKillExperience)
        : null;
    public TimeSpan? EstimatedTimeToLevel => RemainingToLevel is > 0 && ExperiencePerHour > 0
        ? TimeSpan.FromHours(RemainingToLevel.Value / ExperiencePerHour)
        : null;
    public long BestExperiencePerHour => History.Count == 0 ? 0 : History.Max(item => item.ExperiencePerHour);
    public long LargestKill => _data.Sessions.SelectMany(session => session.Changes)
        .Where(change => change.Kind == ExperienceChangeKind.KillReward).Select(change => change.Amount).DefaultIfEmpty().Max();
    public int TotalKills => _data.Sessions.Sum(session => session.Changes.Count(change => change.Kind == ExperienceChangeKind.KillReward));

    public void Start(ExperienceStatisticsData data)
    {
        _data = data;
        _session = new ExperienceSessionData();
        _data.Sessions.Add(_session);
        Refresh();
    }

    public void Apply(IEnumerable<ExperienceChange> changes)
    {
        foreach (var change in changes)
        {
            _session.Changes.Add(new ExperienceChangeData
            {
                Kind = change.Kind,
                Amount = change.Amount,
                EnemyName = change.EnemyName,
                Level = change.Level,
                RemainingToLevel = change.RemainingToLevel,
                When = change.When,
            });
            _session.LastUpdatedAt = change.When;
        }
        Refresh();
    }

    public ExperienceStatisticsData Data => _data;

    private void Refresh()
    {
        var gains = _session.Changes.Where(IsGain).ToList();
        var losses = _session.Changes.Where(change => !IsGain(change)).ToList();
        DamageExperience = gains.Where(change => change.Kind == ExperienceChangeKind.Damage).Sum(change => change.Amount);
        KillExperience = gains.Where(change => change.Kind == ExperienceChangeKind.KillReward).Sum(change => change.Amount);
        GainedExperience = gains.Sum(change => change.Amount);
        FleeLoss = losses.Where(change => change.Kind == ExperienceChangeKind.FleeLoss).Sum(change => change.Amount);
        DeathLoss = losses.Where(change => change.Kind == ExperienceChangeKind.DeathLoss).Sum(change => change.Amount);
        LostExperience = losses.Sum(change => change.Amount);
        var kills = gains.Where(change => change.Kind == ExperienceChangeKind.KillReward).ToList();
        KillCount = kills.Count;
        if (kills.LastOrDefault() is { } last)
        {
            LastEnemy = last.EnemyName ?? "Nieznany przeciwnik";
            LastKillExperience = last.Amount;
        }
        RemainingToLevel = _session.Changes.LastOrDefault(change => change.RemainingToLevel.HasValue)?.RemainingToLevel;

        Mobs.Clear();
        foreach (var group in _data.Sessions.SelectMany(session => session.Changes)
                     .Where(change => change.Kind == ExperienceChangeKind.KillReward)
                     .GroupBy(change => change.EnemyName ?? "Nieznany przeciwnik", StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(group => group.Max(change => change.When)))
        {
            var ordered = group.OrderBy(change => change.When).ToList();
            Mobs.Add(new MobExperienceSummary(group.Key, ordered));
        }

        History.Clear();
        foreach (var session in _data.Sessions.OrderByDescending(item => item.StartedAt))
        {
            History.Add(new SessionExperienceSummary(session));
        }
        Analysis = BuildAnalysis(Mobs);
        OnPropertyChanged(nameof(SessionStartedAt));
        OnPropertyChanged(nameof(SessionDuration));
        OnPropertyChanged(nameof(NetExperience));
        OnPropertyChanged(nameof(ExperiencePerHour));
        OnPropertyChanged(nameof(AveragePerKill));
        OnPropertyChanged(nameof(LastKillProgressPercent));
        OnPropertyChanged(nameof(EstimatedTimeToLevel));
        OnPropertyChanged(nameof(BestExperiencePerHour));
        OnPropertyChanged(nameof(LargestKill));
        OnPropertyChanged(nameof(TotalKills));
    }

    private static bool IsGain(ExperienceChangeData change) => change.Kind is
        ExperienceChangeKind.Damage or ExperienceChangeKind.KillReward or ExperienceChangeKind.UnknownGain;

    private static string BuildAnalysis(IEnumerable<MobExperienceSummary> mobs)
    {
        var useful = mobs.Where(mob => mob.Kills >= 2).OrderByDescending(mob => mob.RecentAverage).ToList();
        if (useful.Count == 0) return "Potrzeba co najmniej dwóch zabójstw tego samego przeciwnika, aby ocenić trend.";
        var best = useful[0];
        return $"{best.Name} daje obecnie średnio {best.RecentAverage:N0} EXP. Trend: {best.Trend.ToLowerInvariant()}.";
    }
}

public sealed class MobExperienceSummary
{
    public MobExperienceSummary(string name, IReadOnlyList<ExperienceChangeData> kills)
    {
        Name = name; Kills = kills.Count; Total = kills.Sum(item => item.Amount);
        Average = Kills == 0 ? 0 : Total / Kills; Last = kills[^1].Amount; LastKilledAt = kills[^1].When;
        RecentAverage = (long)kills.TakeLast(Math.Min(5, Kills)).Average(item => item.Amount);
        Trend = Kills < 2 ? "Za mało danych" : Last < kills[0].Amount * 0.95 ? "Spada" : Last > kills[0].Amount * 1.05 ? "Rośnie" : "Stabilny";
    }
    public string Name { get; }
    public int Kills { get; }
    public long Total { get; }
    public long Average { get; }
    public long RecentAverage { get; }
    public long Last { get; }
    public string Trend { get; }
    public DateTimeOffset LastKilledAt { get; }
}

public sealed class SessionExperienceSummary
{
    public SessionExperienceSummary(ExperienceSessionData session)
    {
        StartedAt = session.StartedAt;
        Duration = session.LastUpdatedAt - session.StartedAt;
        Experience = session.Changes.Where(change => change.Kind is ExperienceChangeKind.Damage or ExperienceChangeKind.KillReward or ExperienceChangeKind.UnknownGain).Sum(change => change.Amount)
            - session.Changes.Where(change => change.Kind is ExperienceChangeKind.FleeLoss or ExperienceChangeKind.DeathLoss or ExperienceChangeKind.UnknownLoss).Sum(change => change.Amount);
        Kills = session.Changes.Count(change => change.Kind == ExperienceChangeKind.KillReward);
        ExperiencePerHour = Duration.TotalHours <= 0 ? 0 : (long)(Experience / Duration.TotalHours);
        MostKilled = session.Changes.Where(change => change.Kind == ExperienceChangeKind.KillReward)
            .GroupBy(change => change.EnemyName ?? "Nieznany").OrderByDescending(group => group.Count()).FirstOrDefault()?.Key ?? "—";
    }
    public DateTimeOffset StartedAt { get; }
    public TimeSpan Duration { get; }
    public long Experience { get; }
    public long ExperiencePerHour { get; }
    public int Kills { get; }
    public string MostKilled { get; }
}
