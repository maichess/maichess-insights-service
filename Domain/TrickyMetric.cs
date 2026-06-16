namespace MaichessInsightsService.Domain;

// One tricky-position row (insights_tricky): reached often enough (support) with both
// high average centipawn loss and high think time.
internal sealed record TrickyMetric(
    string NormalizedFen,
    long Support,
    double AvgCentipawnLoss,
    double BlunderProbability,
    double AvgThinkTimeMs);
