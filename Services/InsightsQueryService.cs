using System.Text.Json;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// The insights read API: serve the materialized insights_* metrics with an L1 cache.
// For each query: clamp paging, look up the L1 (keyed by corpus + filters), on a miss
// confirm the corpus exists (404 vs empty), read the repository, sort to the contract
// order, cache the full list, then page. Pure orchestration over injected seams, so it
// is fully unit-tested with fakes. A null list/summary means "no corpus with that id".
internal sealed class InsightsQueryService(
    IInsightsRepository repository,
    IInsightsCache cache,
    IInsightsStore store)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 500;

    public Task<IReadOnlyList<OpeningMetric>?> GetTopOpeningsAsync(OpeningsQuery query, CancellationToken ct) =>
        CachedPageAsync(
            query.CorpusId,
            $"insights:openings:{query.CorpusId}:{query.Color}:{query.RatingBand}:{query.TimeControl}",
            query.Limit,
            query.Offset,
            c => repository.GetOpeningsAsync(query.CorpusId, c),
            rows =>
            [
                .. rows
                    .Where(r => Matches(r.Color, query.Color)
                        && Matches(r.RatingBand, query.RatingBand)
                        && Matches(r.TimeControl, query.TimeControl))
                    .OrderByDescending(r => r.GameCount),
            ],
            ct);

    public Task<IReadOnlyList<EndgameMetric>?> GetCommonEndgamesAsync(PagedQuery query, CancellationToken ct) =>
        CachedPageAsync(
            query.CorpusId,
            $"insights:endgames:{query.CorpusId}",
            query.Limit,
            query.Offset,
            c => repository.GetEndgamesAsync(query.CorpusId, c),
            rows => [.. rows.OrderByDescending(r => r.Frequency)],
            ct);

    public Task<IReadOnlyList<PositionMetric>?> GetCommonPositionsAsync(PositionsQuery query, CancellationToken ct) =>
        CachedPageAsync(
            query.CorpusId,
            $"insights:positions:{query.CorpusId}:{query.ExcludeBook}",
            query.Limit,
            query.Offset,
            c => repository.GetPositionsAsync(query.CorpusId, c),
            rows => [.. rows.OrderByDescending(r => r.ReachCount)],
            ct);

    public Task<IReadOnlyList<TrickyMetric>?> GetTrickyPositionsAsync(PagedQuery query, CancellationToken ct) =>
        CachedPageAsync(
            query.CorpusId,
            $"insights:tricky:{query.CorpusId}",
            query.Limit,
            query.Offset,
            c => repository.GetTrickyAsync(query.CorpusId, c),
            rows => [.. rows.OrderByDescending(r => r.AvgCentipawnLoss).ThenByDescending(r => r.AvgThinkTimeMs)],
            ct);

    public async Task<CorpusSummaryMetric?> GetCorpusSummaryAsync(string corpusId, CancellationToken ct)
    {
        string key = $"insights:summary:{corpusId}";
        string? cached = await cache.GetAsync(key, ct);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<CorpusSummaryMetric>(cached);
        }

        CorpusSummaryMetric? summary = await repository.GetSummaryAsync(corpusId, ct);
        if (summary is null)
        {
            return null;
        }

        CorpusSummaryMetric ordered = summary with
        {
            FirstMoves = [.. summary.FirstMoves.OrderByDescending(m => m.GameCount)],
        };
        await cache.SetAsync(key, JsonSerializer.Serialize(ordered), ct);
        return ordered;
    }

    private static bool Matches(string rowValue, string filter) =>
        filter.Length == 0 || string.Equals(rowValue, filter, StringComparison.Ordinal);

    private async Task<IReadOnlyList<T>?> CachedPageAsync<T>(
        string corpusId,
        string cacheKey,
        int limit,
        int offset,
        Func<CancellationToken, Task<IReadOnlyList<T>>> read,
        Func<IReadOnlyList<T>, IReadOnlyList<T>> order,
        CancellationToken ct)
    {
        IReadOnlyList<T> full;
        string? cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
        {
            full = JsonSerializer.Deserialize<List<T>>(cached) ?? [];
        }
        else
        {
            if (await store.GetCorpusAsync(corpusId, ct) is null)
            {
                return null;
            }

            full = order(await read(ct));
            await cache.SetAsync(cacheKey, JsonSerializer.Serialize(full), ct);
        }

        return [.. full.Skip(Math.Max(offset, 0)).Take(Clamp(limit))];
    }

    private static int Clamp(int limit) => limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
}
