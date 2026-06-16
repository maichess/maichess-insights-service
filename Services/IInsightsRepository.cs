using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Reads the materialized insights_* metric collections (written by the Scala Spark
// jobs) from insights-db. Each method returns every row for a corpus, unordered;
// ordering + paging is applied in the tested InsightsQueryService. Behind a seam so
// the query service is unit-tested with a fake.
internal interface IInsightsRepository
{
    Task<IReadOnlyList<OpeningMetric>> GetOpeningsAsync(string corpusId, CancellationToken ct);

    Task<IReadOnlyList<EndgameMetric>> GetEndgamesAsync(string corpusId, CancellationToken ct);

    Task<IReadOnlyList<PositionMetric>> GetPositionsAsync(string corpusId, CancellationToken ct);

    Task<IReadOnlyList<TrickyMetric>> GetTrickyAsync(string corpusId, CancellationToken ct);

    Task<CorpusSummaryMetric?> GetSummaryAsync(string corpusId, CancellationToken ct);
}
