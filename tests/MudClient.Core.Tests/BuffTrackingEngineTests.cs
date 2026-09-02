using MudClient.Core.BuffTimers;

namespace MudClient.Core.Tests;

public sealed class BuffTrackingEngineTests
{
    [Theory]
    [InlineData("cast 'stone skin' self", "stone skin")]
    [InlineData("c armor siebie", "armor")]
    [InlineData("cast armor Gandalf", "armor")]
    public void Parser_AcceptsOnlyExplicitSelfTargets(string command, string expected)
    {
        Assert.True(SelfBuffCastParser.TryParse(command, "Gandalf", out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("cast armor orc")]
    [InlineData("cast armor")]
    [InlineData("kill orc")]
    public void Parser_RejectsNonSelfCommands(string command) =>
        Assert.False(SelfBuffCastParser.TryParse(command, "Gandalf", out _));

    [Fact]
    public void TracksOnlyConfirmedOwnBuff_AndSplitsCombatTime()
    {
        var start = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        var engine = new BuffTrackingEngine();
        BuffMeasurement? completed = null;
        engine.MeasurementCompleted += value => completed = value;
        engine.SetLevel(42);

        engine.ProcessAffects([], start);
        engine.ObserveCommand("cast 'stone skin' self", "Gandalf", start);
        engine.ProcessAffects(["stone skin"], start.AddSeconds(1));
        engine.SetCombat(true, start.AddSeconds(11));
        engine.SetCombat(false, start.AddSeconds(31));
        engine.ProcessAffects([], start.AddSeconds(41));

        Assert.NotNull(completed);
        Assert.Equal(42, completed!.CharacterLevel);
        Assert.Equal(20, completed.CombatSeconds, 3);
        Assert.Equal(20, completed.NonCombatSeconds, 3);
        Assert.Equal(40, completed.DurationSeconds, 3);
        Assert.True(completed.IsComplete);
    }

    [Fact]
    public void IgnoresAffectWithoutOwnCast()
    {
        var now = DateTimeOffset.UtcNow;
        var engine = new BuffTrackingEngine();
        engine.ProcessAffects([], now);
        engine.ProcessAffects(["armor"], now.AddSeconds(1));

        Assert.Empty(engine.Checkpoints);
    }

    [Fact]
    public void Estimator_RequiresMinimumSamples()
    {
        var now = DateTimeOffset.UtcNow;
        var samples = Enumerable.Range(0, 4).Select(index => Sample(index, 100 + index, now)).ToList();
        var estimator = new BuffDurationEstimator();

        Assert.Null(estimator.Calculate("armor", samples, 20, now, minimumSamples: 5));
        Assert.NotNull(estimator.Calculate("armor", samples, 20, now, minimumSamples: 4));
    }

    [Fact]
    public void Prediction_ChangesWhenCurrentStateIsCombat()
    {
        var now = DateTimeOffset.UtcNow;
        var samples = Enumerable.Range(0, 8)
            .Select(index => new BuffMeasurement(
                Guid.NewGuid(), "armor", now.AddDays(-index).AddSeconds(-75), now.AddDays(-index),
                75, 20, 25, 50, BuffMeasurementEndReason.NaturalExpiration, now.AddDays(-index)))
            .ToList();
        var estimator = new BuffDurationEstimator();
        var outside = new ActiveBuffCheckpoint("armor", now.AddSeconds(-10), now, 20, 0, 10, false);
        var combat = outside with { IsInCombat = true };

        var outsidePrediction = estimator.Predict(outside, samples, 20, now, 5);
        var combatPrediction = estimator.Predict(combat, samples, 20, now, 5);

        Assert.NotNull(outsidePrediction);
        Assert.NotNull(combatPrediction);
        Assert.True(combatPrediction!.RemainingSeconds <= outsidePrediction!.RemainingSeconds);
    }

    private static BuffMeasurement Sample(int index, double seconds, DateTimeOffset now) => new(
        Guid.NewGuid(), "armor", now.AddDays(-index).AddSeconds(-seconds), now.AddDays(-index),
        seconds, 20, 0, seconds, BuffMeasurementEndReason.NaturalExpiration, now.AddDays(-index));
}
