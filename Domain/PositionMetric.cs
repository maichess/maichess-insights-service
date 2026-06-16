namespace MaichessInsightsService.Domain;

// One most-reached normalized-FEN row (insights_positions). reachCount is the number
// of distinct games that reached the position.
internal sealed record PositionMetric(
    string NormalizedFen,
    long ReachCount,
    double WhiteWinRate,
    double BlackWinRate,
    double DrawRate);
