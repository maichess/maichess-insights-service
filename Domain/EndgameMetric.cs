namespace MaichessInsightsService.Domain;

// One endgame material-signature row (insights_endgames). Result tendency is from
// the stronger side's perspective; the three rates sum to ~1.
internal sealed record EndgameMetric(
    string MaterialSignature,
    long Frequency,
    double StrongerSideWinRate,
    double DrawRate,
    double StrongerSideLossRate);
