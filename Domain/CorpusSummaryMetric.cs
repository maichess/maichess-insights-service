namespace MaichessInsightsService.Domain;

// The headline aggregate per corpus (insights_summary). The Count lists carry the
// rating-band / termination / first-move-SAN distributions.
internal sealed record CorpusSummaryMetric(
    string CorpusId,
    long TotalGames,
    string DateFrom,
    string DateTo,
    double DrawRate,
    double AvgPlyCount,
    IReadOnlyList<CountMetric> RatingDistribution,
    IReadOnlyList<CountMetric> TerminationMix,
    IReadOnlyList<CountMetric> FirstMoves);
