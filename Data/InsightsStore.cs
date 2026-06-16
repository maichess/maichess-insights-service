using System.Diagnostics.CodeAnalysis;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Maichess.Database.V1;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Data;

// IInsightsStore over the insights-db DatabaseService instance (project convention:
// generic Database gRPC CRUD, never a direct Mongo driver). Excluded from coverage:
// requires the live database service. The Database List filter is equality-only and
// unordered, so newest-first ordering + paging is applied to the fetched page here.
[ExcludeFromCodeCoverage]
internal sealed class InsightsStore(Database.DatabaseClient client) : IInsightsStore
{
    private const string Jobs = "insights_jobs";
    private const string Corpora = "insights_corpora";

    public async Task<JobRecord> InsertJobAsync(JobRecord job, CancellationToken ct)
    {
        InsertResponse response = await client.InsertAsync(
            new InsertRequest { Collection = Jobs, Record = JobToStruct(job) }, cancellationToken: ct);
        return ToJob(response.Record);
    }

    public async Task UpdateJobAsync(JobRecord job, CancellationToken ct) =>
        await client.UpdateAsync(
            new UpdateRequest { Collection = Jobs, Id = job.Id, Fields = JobToStruct(job) }, cancellationToken: ct);

    public async Task<JobRecord?> GetJobAsync(string id, CancellationToken ct)
    {
        try
        {
            GetResponse response = await client.GetAsync(
                new GetRequest { Collection = Jobs, Id = id }, cancellationToken: ct);
            return ToJob(response.Record);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<JobRecord>> ListJobsAsync(
        JobStatus? status, int limit, int offset, CancellationToken ct)
    {
        ListRequest request = new() { Collection = Jobs };
        if (status is { } s)
        {
            request.Filter = new Struct();
            request.Filter.Fields["status"] = Value.ForString(JobStatusNames.ToName(s));
        }

        ListResponse response = await client.ListAsync(request, cancellationToken: ct);
        return
        [
            .. response.Records
                .Select(ToJob)
                .OrderByDescending(job => job.CreatedAtMs)
                .Skip(offset)
                .Take(limit),
        ];
    }

    public async Task InsertCorpusAsync(CorpusRecord corpus, CancellationToken ct)
    {
        Struct record = CorpusToStruct(corpus);
        record.Fields["id"] = Value.ForString(corpus.Id);
        await client.InsertAsync(new InsertRequest { Collection = Corpora, Record = record }, cancellationToken: ct);
    }

    public async Task<CorpusRecord?> GetCorpusAsync(string id, CancellationToken ct)
    {
        try
        {
            GetResponse response = await client.GetAsync(
                new GetRequest { Collection = Corpora, Id = id }, cancellationToken: ct);
            return ToCorpus(response.Record);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<CorpusRecord>> ListCorporaAsync(int limit, int offset, CancellationToken ct)
    {
        ListResponse response = await client.ListAsync(
            new ListRequest { Collection = Corpora }, cancellationToken: ct);
        return
        [
            .. response.Records
                .Select(ToCorpus)
                .OrderByDescending(corpus => corpus.CreatedAtMs)
                .Skip(offset)
                .Take(limit),
        ];
    }

    private static Struct JobToStruct(JobRecord job)
    {
        Struct s = new();
        s.Fields["type"] = Value.ForString(JobTypeNames.ToName(job.Type));
        s.Fields["corpus_id"] = Value.ForString(job.CorpusId);
        s.Fields["source"] = job.Source is null ? Value.ForNull() : Value.ForStruct(SourceToStruct(job.Source));
        s.Fields["filter"] = Value.ForStruct(FilterToStruct(job.Filter));
        s.Fields["status"] = Value.ForString(JobStatusNames.ToName(job.Status));
        s.Fields["analysis_kinds"] =
            Value.ForList([.. job.AnalysisKinds.Select(k => Value.ForString(AnalysisKindNames.ToName(k)))]);
        s.Fields["created_at_ms"] = Value.ForNumber(job.CreatedAtMs);
        s.Fields["started_at_ms"] = Value.ForNumber(job.StartedAtMs);
        s.Fields["finished_at_ms"] = Value.ForNumber(job.FinishedAtMs);
        s.Fields["spark_application"] = Value.ForString(job.SparkApplication);
        s.Fields["submitted_by"] = Value.ForString(job.SubmittedBy);
        s.Fields["error"] = Value.ForString(job.Error);
        return s;
    }

    private static JobRecord ToJob(Struct r) => new()
    {
        Id = Str(r, "id"),
        Type = JobTypeNames.FromName(Str(r, "type")),
        CorpusId = Str(r, "corpus_id"),
        Source = SourceFrom(r),
        Filter = FilterFrom(StructField(r, "filter")),
        Status = JobStatusNames.FromName(Str(r, "status")),
        AnalysisKinds = [.. KindStrings(r).Select(ParseKind).Where(k => k is not null).Select(k => k!.Value)],
        CreatedAtMs = (long)Num(r, "created_at_ms"),
        StartedAtMs = (long)Num(r, "started_at_ms"),
        FinishedAtMs = (long)Num(r, "finished_at_ms"),
        SparkApplication = Str(r, "spark_application"),
        SubmittedBy = Str(r, "submitted_by"),
        Error = Str(r, "error"),
    };

    private static Struct CorpusToStruct(CorpusRecord corpus)
    {
        Struct s = new();
        s.Fields["source"] = Value.ForStruct(SourceToStruct(corpus.Source));
        s.Fields["filter"] = Value.ForStruct(FilterToStruct(corpus.Filter));
        s.Fields["game_count"] = Value.ForNumber(corpus.GameCount);
        s.Fields["created_at_ms"] = Value.ForNumber(corpus.CreatedAtMs);
        return s;
    }

    private static CorpusRecord ToCorpus(Struct r) => new()
    {
        Id = Str(r, "id"),
        Source = SourceFrom(r) ?? IngestionSourceSpec.Lichess(string.Empty),
        Filter = FilterFrom(StructField(r, "filter")),
        GameCount = (long)Num(r, "game_count"),
        CreatedAtMs = (long)Num(r, "created_at_ms"),
    };

    private static Struct SourceToStruct(IngestionSourceSpec source)
    {
        Struct s = new();
        s.Fields["kind"] = Value.ForString(source.Kind == SourceKind.Lichess ? "lichess" : "upload");
        s.Fields["lichess_year_month"] = Value.ForString(source.LichessYearMonth);
        s.Fields["upload_object_key"] = Value.ForString(source.UploadObjectKey);
        s.Fields["upload_label"] = Value.ForString(source.UploadLabel);
        return s;
    }

    private static IngestionSourceSpec? SourceFrom(Struct record)
    {
        Struct? s = StructField(record, "source");
        if (s is null)
        {
            return null;
        }

        return Str(s, "kind") == "upload"
            ? IngestionSourceSpec.Upload(Str(s, "upload_object_key"), Str(s, "upload_label"))
            : IngestionSourceSpec.Lichess(Str(s, "lichess_year_month"));
    }

    private static Struct FilterToStruct(CorpusFilterSpec filter)
    {
        Struct s = new();
        s.Fields["rating_band"] = Value.ForString(filter.RatingBand);
        s.Fields["time_control"] = Value.ForString(filter.TimeControl);
        s.Fields["date_from_ms"] = Value.ForNumber(filter.DateFromMs);
        s.Fields["date_to_ms"] = Value.ForNumber(filter.DateToMs);
        s.Fields["sample_rate"] = Value.ForNumber(filter.SampleRate);
        return s;
    }

    private static CorpusFilterSpec FilterFrom(Struct? s) =>
        s is null
            ? CorpusFilterSpec.Empty
            : new CorpusFilterSpec(
                Str(s, "rating_band"),
                Str(s, "time_control"),
                (long)Num(s, "date_from_ms"),
                (long)Num(s, "date_to_ms"),
                Num(s, "sample_rate"));

    private static AnalysisKind? ParseKind(string raw) =>
        AnalysisKindNames.TryParse(raw, out AnalysisKind kind) ? kind : null;

    private static IEnumerable<string> KindStrings(Struct r) =>
        r.Fields.TryGetValue("analysis_kinds", out Value? v) && v.KindCase == Value.KindOneofCase.ListValue
            ? v.ListValue.Values.Where(x => x.KindCase == Value.KindOneofCase.StringValue).Select(x => x.StringValue)
            : [];

    private static Struct? StructField(Struct r, string field) =>
        r.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.StructValue ? v.StructValue : null;

    private static string Str(Struct s, string field) =>
        s.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.StringValue ? v.StringValue : string.Empty;

    private static double Num(Struct s, string field) =>
        s.Fields.TryGetValue(field, out Value? v) && v.KindCase == Value.KindOneofCase.NumberValue ? v.NumberValue : 0;
}
</content>
