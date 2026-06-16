namespace MaichessInsightsService.Domain;

// One opening-success row (insights_openings). Split dimensions are empty for the
// un-split aggregate; Trend is empty until task 04 emits per-month trend points.
internal sealed record OpeningMetric(
    string Eco,
    string OpeningName,
    long GameCount,
    double WhiteWinRate,
    double BlackWinRate,
    double DrawRate,
    string Color,
    string RatingBand,
    string TimeControl,
    IReadOnlyList<OpeningTrendMetric> Trend);
