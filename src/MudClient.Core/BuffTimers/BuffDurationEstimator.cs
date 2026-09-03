namespace MudClient.Core.BuffTimers;

public sealed class BuffDurationEstimator
{
    public BuffStatistics? Calculate(
        string buffName,
        IEnumerable<BuffMeasurement> measurements,
        int currentLevel,
        DateTimeOffset now,
        int minimumSamples = 5) =>
        BuildModel(buffName, measurements, currentLevel, now, minimumSamples)?.Statistics;

    public BuffPrediction? Predict(
        ActiveBuffCheckpoint active,
        IEnumerable<BuffMeasurement> measurements,
        int currentLevel,
        DateTimeOffset now,
        int minimumSamples = 5)
    {
        var model = BuildModel(active.BuffName, measurements, currentLevel, now, minimumSamples);
        if (model is null)
        {
            return null;
        }

        var spent = active.NonCombatSeconds + model.Statistics.CombatRate * active.CombatSeconds;
        var survivors = model.Budgets
            .Where(item => item.Value > spent)
            .OrderBy(item => item.Value)
            .ToList();

        double remaining;
        double expirationProbability;
        if (survivors.Count == 0)
        {
            // The current activation has outlived every comparable sample. There is no honest
            // positive countdown left, but expiration should be treated as imminent.
            remaining = 0;
            expirationProbability = 1;
        }
        else
        {
            var horizonBudget = active.IsInCombat ? model.Statistics.CombatRate * 30 : 30;
            var survivorWeight = survivors.Sum(item => item.Weight);
            expirationProbability = survivors
                .Where(item => item.Value <= spent + horizonBudget)
                .Sum(item => item.Weight) / survivorWeight;

            var remainingValues = survivors
                .Select(item => new WeightedValue(
                    active.IsInCombat
                        ? (item.Value - spent) / model.Statistics.CombatRate
                        : item.Value - spent,
                    item.Weight))
                .OrderBy(item => item.Value)
                .ToList();
            remaining = WeightedMedian(remainingValues);
        }

        return new BuffPrediction(
            active.BuffName,
            Math.Max(0, remaining),
            now.AddSeconds(Math.Max(0, remaining)),
            model.Statistics,
            Math.Clamp(expirationProbability, 0, 1));
    }

    private static EstimationModel? BuildModel(
        string buffName,
        IEnumerable<BuffMeasurement> measurements,
        int currentLevel,
        DateTimeOffset now,
        int minimumSamples)
    {
        var requiredSamples = Math.Max(2, minimumSamples);
        var candidates = measurements
            .Where(sample => sample.IsComplete
                && string.Equals(sample.BuffName, buffName, StringComparison.OrdinalIgnoreCase)
                && sample.DurationSeconds > 0)
            .ToList();
        if (candidates.Count < requiredSamples)
        {
            return null;
        }

        var samples = FilterShortOutliers(candidates);
        if (samples.Count < requiredSamples)
        {
            return null;
        }

        var weighted = samples.Select(sample => new WeightedSample(sample, Weight(sample, currentLevel, now))).ToList();
        var totalWeight = weighted.Sum(item => item.Weight);
        var mean = weighted.Sum(item => item.Sample.DurationSeconds * item.Weight) / totalWeight;
        var ordered = samples.Select(sample => sample.DurationSeconds).Order().ToArray();
        var median = Median(ordered);
        var variance = weighted.Sum(item => item.Weight * Math.Pow(item.Sample.DurationSeconds - mean, 2)) / totalWeight;

        // Grid-search the combat ageing multiplier against robust effective-duration budgets.
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
        var statistics = new BuffStatistics(
            SelfBuffCastParser.NormalizeName(buffName), samples.Count, mean, median,
            ordered[0], ordered[^1], Math.Sqrt(variance), bestRate, bestBudget, confidence);
        var effectiveBudgets = weighted
            .Select(item => new WeightedValue(
                item.Sample.NonCombatSeconds + bestRate * item.Sample.CombatSeconds,
                item.Weight))
            .OrderBy(item => item.Value)
            .ToList();
        return new EstimationModel(statistics, effectiveBudgets);
    }

    private static List<BuffMeasurement> FilterShortOutliers(IReadOnlyList<BuffMeasurement> samples)
    {
        var ordered = samples.Select(sample => sample.DurationSeconds).Order().ToArray();
        var median = Median(ordered);
        var deviations = ordered.Select(value => Math.Abs(value - median)).Order().ToArray();
        var medianAbsoluteDeviation = Median(deviations);
        var robustLowerBound = medianAbsoluteDeviation > 0
            ? median - 3 * 1.4826 * medianAbsoluteDeviation
            : median * 0.5;
        var lowerBound = Math.Max(median * 0.25, robustLowerBound);
        return samples.Where(sample => sample.DurationSeconds >= lowerBound).ToList();
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
    private sealed record EstimationModel(BuffStatistics Statistics, IReadOnlyList<WeightedValue> Budgets);
}
