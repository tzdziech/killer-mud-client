namespace MudClient.Core.BuffTimers;

public sealed class BuffDurationEstimator
{
    public BuffStatistics? Calculate(
        string buffName,
        IEnumerable<BuffMeasurement> measurements,
        int currentLevel,
        DateTimeOffset now,
        int minimumSamples = 5)
    {
        var samples = measurements
            .Where(sample => sample.IsComplete
                && string.Equals(sample.BuffName, buffName, StringComparison.OrdinalIgnoreCase)
                && sample.DurationSeconds > 0)
            .ToList();
        if (samples.Count < Math.Max(2, minimumSamples))
        {
            return null;
        }

        var weighted = samples.Select(sample => new WeightedSample(sample, Weight(sample, currentLevel, now))).ToList();
        var totalWeight = weighted.Sum(item => item.Weight);
        var mean = weighted.Sum(item => item.Sample.DurationSeconds * item.Weight) / totalWeight;
        var ordered = samples.Select(sample => sample.DurationSeconds).Order().ToArray();
        var median = Median(ordered);
        var variance = weighted.Sum(item => item.Weight * Math.Pow(item.Sample.DurationSeconds - mean, 2)) / totalWeight;

        // Grid-search a conservative combat ageing multiplier. For each candidate, the weighted
        // median effective budget is robust against dispels and other short outliers.
        var bestRate = 1d;
        var bestBudget = median;
        var bestError = double.MaxValue;
        for (var rate = 1d; rate <= 3.0001; rate += 0.05)
        {
            var budgets = weighted
                .Select(item => new WeightedValue(
                    item.Sample.NonCombatSeconds + rate * item.Sample.CombatSeconds,
                    item.Weight))
                .OrderBy(item => item.Value).ToList();
            var budget = WeightedMedian(budgets);
            var error = budgets.Sum(item => item.Weight * Math.Abs(item.Value - budget));
            if (error < bestError)
            {
                bestError = error;
                bestRate = rate;
                bestBudget = budget;
            }
        }

        var coefficientOfVariation = mean <= 0 ? 1 : Math.Sqrt(variance) / mean;
        var sampleConfidence = Math.Min(1, samples.Count / 20d);
        var stabilityConfidence = Math.Clamp(1 - coefficientOfVariation, 0, 1);
        var confidence = sampleConfidence * 0.6 + stabilityConfidence * 0.4;
        return new BuffStatistics(
            SelfBuffCastParser.NormalizeName(buffName), samples.Count, mean, median,
            ordered[0], ordered[^1], Math.Sqrt(variance), bestRate, bestBudget, confidence);
    }

    public BuffPrediction? Predict(
        ActiveBuffCheckpoint active,
        IEnumerable<BuffMeasurement> measurements,
        int currentLevel,
        DateTimeOffset now,
        int minimumSamples = 5)
    {
        var statistics = Calculate(active.BuffName, measurements, currentLevel, now, minimumSamples);
        if (statistics is null)
        {
            return null;
        }

        var spent = active.NonCombatSeconds + statistics.CombatRate * active.CombatSeconds;
        var remainingBudget = Math.Max(0, statistics.PredictedBudgetSeconds - spent);
        var remaining = active.IsInCombat ? remainingBudget / statistics.CombatRate : remainingBudget;
        return new BuffPrediction(active.BuffName, remaining, now.AddSeconds(remaining), statistics);
    }

    private static double Weight(BuffMeasurement sample, int level, DateTimeOffset now)
    {
        var ageDays = Math.Max(0, (now - sample.EndedAtUtc).TotalDays);
        var recency = Math.Pow(0.5, ageDays / 90d);
        var levelSimilarity = Math.Exp(-Math.Abs(sample.CharacterLevel - level) / 10d);
        return Math.Max(0.0001, recency * levelSimilarity);
    }

    private static double Median(double[] ordered) => ordered.Length % 2 == 1
        ? ordered[ordered.Length / 2]
        : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;

    private static double WeightedMedian(IReadOnlyList<WeightedValue> ordered)
    {
        var half = ordered.Sum(item => item.Weight) / 2;
        var accumulated = 0d;
        foreach (var item in ordered)
        {
            accumulated += item.Weight;
            if (accumulated >= half)
            {
                return item.Value;
            }
        }

        return ordered[^1].Value;
    }

    private sealed record WeightedSample(BuffMeasurement Sample, double Weight);
    private sealed record WeightedValue(double Value, double Weight);
}
