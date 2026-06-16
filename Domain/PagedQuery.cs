namespace MaichessInsightsService.Domain;

// A corpus-scoped paged query with no extra filters (endgames, tricky).
internal sealed record PagedQuery(string CorpusId, int Limit, int Offset);
