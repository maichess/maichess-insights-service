using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using MaichessInsightsService.Tests.Support;
using Xunit;

namespace MaichessInsightsService.Tests;

public class InsightsQueryServiceTests
{
    private const string Corpus = "c";

    private readonly FakeInsightsRepository repo = new();
    private readonly FakeInsightsCache cache = new();
    private readonly FakeInsightsStore store = new();
    private readonly InsightsQueryService service;

    public InsightsQueryServiceTests() => service = new InsightsQueryService(repo, cache, store);

    // ─── Openings ───────────────────────────────────────────────────────────

    [Fact]
    public async Task OpeningsReturnNullWhenCorpusMissing()
    {
        IReadOnlyList<OpeningMetric>? result = await service.GetTopOpeningsAsync(OpeningsQ(), default);

        Assert.Null(result);
        Assert.Equal(0, repo.OpeningsCalls);
        Assert.Equal(0, cache.Sets);
    }

    [Fact]
    public async Task OpeningsSortedByGameCountAndPaged()
    {
        await SeedCorpusAsync();
        repo.Openings.AddRange([Opening("A", 10), Opening("B", 30), Opening("C", 20)]);

        IReadOnlyList<OpeningMetric>? result = await service.GetTopOpeningsAsync(OpeningsQ(limit: 2), default);

        Assert.Equal(["B", "C"], result!.Select(o => o.Eco));
    }

    [Fact]
    public async Task OpeningsOffsetSkipsAndNegativeOffsetIsZero()
    {
        await SeedCorpusAsync();
        repo.Openings.AddRange([Opening("A", 10), Opening("B", 30), Opening("C", 20)]);

        IReadOnlyList<OpeningMetric>? page = await service.GetTopOpeningsAsync(OpeningsQ(limit: 2, offset: 1), default);
        Assert.Equal(["C", "A"], page!.Select(o => o.Eco));

        IReadOnlyList<OpeningMetric>? negative = await service.GetTopOpeningsAsync(OpeningsQ(limit: 1, offset: -5), default);
        Assert.Equal(["B"], negative!.Select(o => o.Eco));
    }

    [Fact]
    public async Task OpeningsSecondCallServedFromCache()
    {
        await SeedCorpusAsync();
        repo.Openings.Add(Opening("A", 10));

        await service.GetTopOpeningsAsync(OpeningsQ(), default);
        IReadOnlyList<OpeningMetric>? second = await service.GetTopOpeningsAsync(OpeningsQ(), default);

        Assert.Equal(1, repo.OpeningsCalls);
        Assert.Equal(1, cache.Sets);
        Assert.Equal(["A"], second!.Select(o => o.Eco));
    }

    [Fact]
    public async Task OpeningsSplitFilterKeepsMatchingRows()
    {
        await SeedCorpusAsync();
        repo.Openings.AddRange(
        [
            Opening("A", 10, color: "white"),
            Opening("B", 30, color: "black"),
            Opening("C", 20, color: "white"),
        ]);

        IReadOnlyList<OpeningMetric>? result = await service.GetTopOpeningsAsync(OpeningsQ(color: "white"), default);

        Assert.Equal(["C", "A"], result!.Select(o => o.Eco));
    }

    [Fact]
    public async Task OpeningsFilterNoMatchReturnsEmptyNotNull()
    {
        await SeedCorpusAsync();
        repo.Openings.Add(Opening("A", 10, color: "white", band: "1600-1999", tc: "blitz"));

        IReadOnlyList<OpeningMetric>? result = await service.GetTopOpeningsAsync(
            OpeningsQ(color: "white", band: "2000-2399", tc: "blitz"), default);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public async Task OpeningsTrendSurvivesCacheRoundTrip()
    {
        await SeedCorpusAsync();
        repo.Openings.Add(new OpeningMetric(
            "B10",
            "Caro-Kann Defense",
            480000,
            0.51,
            0.45,
            0.04,
            string.Empty,
            string.Empty,
            string.Empty,
            [new OpeningTrendMetric("2024-12", 480000, 0.51, 0.45, 0.04)]));

        // First call materializes + caches; second deserializes the nested trend from L1.
        await service.GetTopOpeningsAsync(OpeningsQ(), default);
        IReadOnlyList<OpeningMetric>? cached = await service.GetTopOpeningsAsync(OpeningsQ(), default);

        OpeningTrendMetric trend = Assert.Single(cached![0].Trend);
        Assert.Equal("2024-12", trend.YearMonth);
        Assert.Equal(480000, trend.GameCount);
        Assert.Equal(0.51, trend.WhiteWinRate);
        Assert.Equal(0.45, trend.BlackWinRate);
        Assert.Equal(0.04, trend.DrawRate);
    }

    // ─── Endgames ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EndgamesNullWhenCorpusMissing() =>
        Assert.Null(await service.GetCommonEndgamesAsync(new PagedQuery(Corpus, 0, 0), default));

    [Fact]
    public async Task EndgamesSortedByFrequency()
    {
        await SeedCorpusAsync();
        repo.Endgames.AddRange(
        [
            new EndgameMetric("KRvK", 5, 0.9, 0.1, 0.0),
            new EndgameMetric("KQvK", 50, 0.95, 0.05, 0.0),
            new EndgameMetric("KBNvK", 20, 0.8, 0.15, 0.05),
        ]);

        IReadOnlyList<EndgameMetric>? result = await service.GetCommonEndgamesAsync(new PagedQuery(Corpus, 0, 0), default);

        Assert.Equal(["KQvK", "KBNvK", "KRvK"], result!.Select(e => e.MaterialSignature));
    }

    [Fact]
    public async Task LimitDefaultsToFiftyAndCapsAtFiveHundred()
    {
        await SeedCorpusAsync();
        repo.Endgames.AddRange(
            Enumerable.Range(0, 600).Select(i => new EndgameMetric($"S{i}", 600 - i, 0.5, 0.4, 0.1)));

        IReadOnlyList<EndgameMetric>? defaulted = await service.GetCommonEndgamesAsync(new PagedQuery(Corpus, 0, 0), default);
        IReadOnlyList<EndgameMetric>? capped = await service.GetCommonEndgamesAsync(new PagedQuery(Corpus, 1000, 0), default);

        Assert.Equal(50, defaulted!.Count);
        Assert.Equal(500, capped!.Count);
        Assert.Equal(1, repo.EndgamesCalls);
    }

    [Fact]
    public async Task NullCachePayloadYieldsEmptyListWithoutRepo()
    {
        await SeedCorpusAsync();
        cache.Seed($"insights:endgames:{Corpus}", "null");

        IReadOnlyList<EndgameMetric>? result = await service.GetCommonEndgamesAsync(new PagedQuery(Corpus, 0, 0), default);

        Assert.NotNull(result);
        Assert.Empty(result!);
        Assert.Equal(0, repo.EndgamesCalls);
    }

    // ─── Positions ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PositionsNullWhenCorpusMissing() =>
        Assert.Null(await service.GetCommonPositionsAsync(new PositionsQuery(Corpus, false, 0, 0), default));

    [Fact]
    public async Task PositionsSortedByReachCount()
    {
        await SeedCorpusAsync();
        repo.Positions.AddRange(
        [
            new PositionMetric("f1", 5, 0.5, 0.45, 0.05),
            new PositionMetric("f2", 40, 0.5, 0.45, 0.05),
        ]);

        IReadOnlyList<PositionMetric>? result = await service.GetCommonPositionsAsync(
            new PositionsQuery(Corpus, false, 0, 0), default);

        Assert.Equal(["f2", "f1"], result!.Select(p => p.NormalizedFen));
    }

    [Fact]
    public async Task PositionsExcludeBookUsesSeparateCacheKey()
    {
        await SeedCorpusAsync();
        repo.Positions.Add(new PositionMetric("f1", 5, 0.5, 0.45, 0.05));

        await service.GetCommonPositionsAsync(new PositionsQuery(Corpus, false, 0, 0), default);
        await service.GetCommonPositionsAsync(new PositionsQuery(Corpus, true, 0, 0), default);

        Assert.Equal(2, repo.PositionsCalls);
        Assert.Equal(2, cache.Sets);
    }

    // ─── Tricky ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task TrickyNullWhenCorpusMissing() =>
        Assert.Null(await service.GetTrickyPositionsAsync(new PagedQuery(Corpus, 0, 0), default));

    [Fact]
    public async Task TrickySortedByCentipawnLossThenThinkTime()
    {
        await SeedCorpusAsync();
        repo.Tricky.AddRange(
        [
            new TrickyMetric("a", 100, 150.0, 0.3, 5000),
            new TrickyMetric("b", 100, 150.0, 0.3, 9000),
            new TrickyMetric("c", 100, 200.0, 0.3, 1000),
        ]);

        IReadOnlyList<TrickyMetric>? result = await service.GetTrickyPositionsAsync(new PagedQuery(Corpus, 0, 0), default);

        Assert.Equal(["c", "b", "a"], result!.Select(t => t.NormalizedFen));
    }

    // ─── Summary ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SummaryNullWhenNotMaterialized() =>
        Assert.Null(await service.GetCorpusSummaryAsync(Corpus, default));

    [Fact]
    public async Task SummarySortsFirstMovesAndCaches()
    {
        repo.Summary = new CorpusSummaryMetric(
            Corpus,
            100,
            "2024-12",
            "2024-12",
            0.04,
            70.0,
            [new CountMetric("1600-1999", 100)],
            [new CountMetric("resign", 60), new CountMetric("mate", 40)],
            [new CountMetric("d4", 30), new CountMetric("e4", 70)]);

        CorpusSummaryMetric? result = await service.GetCorpusSummaryAsync(Corpus, default);
        Assert.Equal(["e4", "d4"], result!.FirstMoves.Select(m => m.Key));
        Assert.Equal(1, cache.Sets);

        CorpusSummaryMetric? second = await service.GetCorpusSummaryAsync(Corpus, default);
        Assert.Equal(1, repo.SummaryCalls);
        Assert.Equal(["e4", "d4"], second!.FirstMoves.Select(m => m.Key));
    }

    private static OpeningMetric Opening(string eco, long gameCount, string color = "", string band = "", string tc = "") =>
        new(eco, eco + " Name", gameCount, 0.5, 0.4, 0.1, color, band, tc, []);

    private async Task SeedCorpusAsync(string id = Corpus) =>
        await store.InsertCorpusAsync(
            new CorpusRecord
            {
                Id = id,
                Source = IngestionSourceSpec.Lichess("2024-12"),
                Filter = CorpusFilterSpec.Empty,
            },
            default);

    private static OpeningsQuery OpeningsQ(int limit = 0, int offset = 0, string color = "", string band = "", string tc = "") =>
        new(Corpus, color, band, tc, limit, offset);
}
