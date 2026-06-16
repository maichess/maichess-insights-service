namespace MaichessInsightsService.Domain;

// A GetCommonPositions request. ExcludeBook is a job-time concern (no ply is stored on
// insights_positions) so it only varies the cache key; see CONTRACT_NOTES.
internal sealed record PositionsQuery(
    string CorpusId,
    bool ExcludeBook,
    int Limit,
    int Offset);
