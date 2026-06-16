namespace MaichessInsightsService.Domain;

// A keyed game-count entry inside a corpus summary (rating band, termination, or
// first-move SAN → game count). Mirrors the Scala Count(key, gameCount).
internal sealed record CountMetric(string Key, long GameCount);
