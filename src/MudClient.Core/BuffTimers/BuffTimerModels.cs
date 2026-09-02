namespace MudClient.Core.BuffTimers;

public sealed record BuffCharacterKey(string Host, int Port, string CharacterName)
{
    public static BuffCharacterKey Create(string host, int port, string characterName) =>
        new(host.Trim().ToLowerInvariant(), port, characterName.Trim().ToLowerInvariant());
}

public enum BuffMeasurementEndReason
{
    NaturalExpiration,
    SessionEnded,
    CharacterChanged,
    CharacterDeath,
}

public sealed record BuffMeasurement(
    Guid Id,
    string BuffName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    double DurationSeconds,
    int CharacterLevel,
    double CombatSeconds,
    double NonCombatSeconds,
    BuffMeasurementEndReason EndReason,
    DateTimeOffset RecordedAtUtc)
{
    public bool IsComplete => EndReason == BuffMeasurementEndReason.NaturalExpiration;
}

public sealed record ActiveBuffCheckpoint(
    string BuffName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastUpdatedAtUtc,
    int CharacterLevel,
    double CombatSeconds,
    double NonCombatSeconds,
    bool IsInCombat);

public sealed record BuffStatistics(
    string BuffName,
    int SampleCount,
    double MeanSeconds,
    double MedianSeconds,
    double MinimumSeconds,
    double MaximumSeconds,
    double StandardDeviationSeconds,
    double CombatRate,
    double PredictedBudgetSeconds,
    double Confidence);

public sealed record BuffPrediction(
    string BuffName,
    double RemainingSeconds,
    DateTimeOffset PredictedEndUtc,
    BuffStatistics Statistics);
