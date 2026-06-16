namespace MaichessInsightsService.Domain;

// One month's popularity + success for an opening (insights_openings trend point).
internal sealed record OpeningTrendMetric(
    string YearMonth,
    long GameCount,
    double WhiteWinRate,
    double BlackWinRate,
    double DrawRate);
