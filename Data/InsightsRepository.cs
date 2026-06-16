using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.WellKnownTypes;
using Maichess.Database.V1;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Data;

// Reads the materialized insights_* metric collections from insights-db via the generic
// Database gRPC CRUD (project convention: never a direct Mongo driver). These documents
// are written by the Scala Spark connector, so the field names are camelCase (the Scala
// case-class fields), unlike the snake_case catalog the control plane owns. Excluded from
// coverage: requires the live database service; the ordering/paging lives in the tested
// InsightsQueryService.
[ExcludeFromCodeCoverage]
internal sealed class InsightsRepository(Database.DatabaseClient client) : IInsightsRepository
{
    private const string Openings = "insights_openings";
    private const string Endgames = "insights_endgames";
    private const string Positions = "insights_positions";
    private const string Tricky = "insights_tricky";
    private const string Summary = "insights_summary";

    public async Task<IReadOnlyList<OpeningMetric>> GetOpeningsAsync(string corpusId, CancellationToken ct)
    {
        ListResponse response = await ListByCorpusAsync(Openings, corpusId, ct);
        return [.. response.Records.Select(ToOpening)];
    }

    public async Task<IReadOnlyList<EndgameMetric>> GetEndgamesAsync(string corpusId, CancellationToken ct)
    {
        ListResponse response = await ListByCorpusAsync(Endgames, corpusId, ct);
        return [.. response.Records.Select(ToEndgame)];
    }

    public async Task<IReadOnlyList<PositionMetric>> GetPositionsAsync(string corpusId, CancellationToken ct)
    {
        ListResponse response = await ListByCorpusAsync(Positions, corpusId, ct);
        return [.. response.Records.Select(ToPosition)];
    }

    public async Task<IReadOnlyList<TrickyMetric>> GetTrickyAsync(string corpusId, CancellationToken ct)
    {
        ListResponse response = await ListByCorpusAsync(Tricky, corpusId, ct);
        return [.. response.Records.Select(ToTricky)];
    }

    public async Task<CorpusSummaryMetric?> GetSummaryAsync(string corpusId, CancellationToken ct)
    {
        ListResponse response = await ListByCorpusAsync(Summary, corpusId, ct);
        return response.Records.Count == 0 ? null : ToSummary(response.Records[0]);
    }

    private async Task<ListResponse> ListByCorpusAsync(string collection, string corpusId, CancellationToken ct)
    {
        Struct filter = new();
        filter.Fields["corpusId"] = Value.ForString(corpusId);
        return await client.ListAsync(
            new ListRequest { Collection = collection, Filter = filter }, cancellationToken: ct);
    }

    private static OpeningMetric ToOpening(Struct r) => new(
        Str(r, "eco"),
        Str(r, "openingName"),
        (long)Num(r, "gameCount"),
        Num(r, "whiteWinRate"),
        Num(r, "blackWinRate"),
        Num(r, "drawRate"),
        Str(r, "color"),
        Str(r, "ratingBand"),
        Str(r, "timeControl"),
        [.. ListField(r, "trend").Select(ToTrend)]);

    private static OpeningTrendMetric ToTrend(Struct r) => new(
        Str(r, "yearMonth"),
        (long)Num(r, "gameCount"),
        Num(r, "whiteWinRate"),
        Num(r, "blackWinRate"),
        Num(r, "drawRate"));

    private static EndgameMetric ToEndgame(Struct r) => new(
        Str(r, "materialSignature"),
        (long)Num(r, "frequency"),
        Num(r, "strongerSideWinRate"),
        Num(r, "drawRate"),
        Num(r, "strongerSideLossRate"));

    private static PositionMetric ToPosition(Struct r) => new(
        Str(r, "normalizedFen"),
        (long)Num(r, "reachCount"),
        Num(r, "whiteWinRate"),
        Num(r, "blackWinRate"),
        Num(r, "drawRate"));

    private static TrickyMetric ToTricky(Struct r) => new(
        Str(r, "normalizedFen"),
        (long)Num(r, "support"),
        Num(r, "avgCentipawnLoss"),
        Num(r, "blunderProbability"),
        Num(r, "avgThinkTimeMs"));

    private static CorpusSummaryMetric ToSummary(Struct r) => new(
        Str(r, "corpusId"),
        (long)Num(r, "totalGames"),
        Str(r, "dateFrom"),
        Str(r, "dateTo"),
        Num(r, "drawRate"),
        Num(r, "avgPlyCount"),
        [.. ListField(r, "ratingDistribution").Select(ToCount)],
        [.. ListField(r, "terminationMix").Select(ToCount)],
        [.. ListField(r, "firstMoves").Select(ToCount)]);

    private static CountMetric ToCount(Struct r) => new(Str(r, "key"), (long)Num(r, "gameCount"));

    private static IEnumerable<Struct> ListField(Struct r, string field) =>
        r.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.ListValue
            ? v.ListValue.Values.Where(x => x.KindCase == Value.KindOneofCase.StructValue).Select(x => x.StructValue)
            : [];

    private static string Str(Struct s, string field) =>
        s.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.StringValue ? v.StringValue : string.Empty;

    private static double Num(Struct s, string field) =>
        s.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.NumberValue ? v.NumberValue : 0;
}
