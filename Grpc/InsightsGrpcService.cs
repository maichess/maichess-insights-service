using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Maichess.Insights.V1;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using Proto = Maichess.Insights.V1;

namespace MaichessInsightsService.Grpc;

// gRPC surface for the insights control plane (job submit/track + corpus list). The
// query RPCs (openings/endgames/positions/tricky/summary) are added in task 06 and stay
// as the base Unimplemented until then. Excluded from coverage: thin adapter over the
// tested JobService; mapping mirrors the proto/REST contract.
[ExcludeFromCodeCoverage]
internal sealed class InsightsGrpcService(JobService jobs) : Insights.InsightsBase
{
    public override async Task<SubmitIngestionResponse> SubmitIngestion(
        SubmitIngestionRequest request, ServerCallContext context)
    {
        IngestionInput input = new(
            request.Source?.SourceCase == IngestionSource.SourceOneofCase.LichessMonth
                ? new LichessMonthInput(request.Source.LichessMonth.YearMonth)
                : null,
            request.Source?.SourceCase == IngestionSource.SourceOneofCase.Upload
                ? new UploadInput(request.Source.Upload.ObjectKey, request.Source.Upload.Label)
                : null,
            ToFilter(request.Filter));

        SubmitResult result = await jobs.SubmitIngestionAsync(input, UserId(context), context.CancellationToken);
        return new SubmitIngestionResponse { Job = Unwrap(result) };
    }

    public override async Task<SubmitAnalysisResponse> SubmitAnalysis(
        SubmitAnalysisRequest request, ServerCallContext context)
    {
        AnalysisInput input = new(request.CorpusId, [.. request.Kinds.Select(KindName)]);
        SubmitResult result = await jobs.SubmitAnalysisAsync(input, UserId(context), context.CancellationToken);
        return new SubmitAnalysisResponse { Job = Unwrap(result) };
    }

    public override async Task<GetJobResponse> GetJob(GetJobRequest request, ServerCallContext context)
    {
        JobRecord? job = await jobs.GetJobAsync(request.Id, context.CancellationToken);
        return job is null
            ? throw new RpcException(new Status(StatusCode.NotFound, $"job {request.Id} not found"))
            : new GetJobResponse { Job = ToProto(job) };
    }

    public override async Task<ListJobsResponse> ListJobs(ListJobsRequest request, ServerCallContext context)
    {
        JobStatus? status = request.Status == Proto.JobStatus.Unspecified ? null : FromProtoStatus(request.Status);
        IReadOnlyList<JobRecord> result = await jobs.ListJobsAsync(
            status, request.Limit, request.Offset, context.CancellationToken);
        ListJobsResponse response = new();
        response.Jobs.AddRange(result.Select(ToProto));
        return response;
    }

    public override async Task<ListCorporaResponse> ListCorpora(ListCorporaRequest request, ServerCallContext context)
    {
        IReadOnlyList<CorpusRecord> result = await jobs.ListCorporaAsync(
            request.Limit, request.Offset, context.CancellationToken);
        ListCorporaResponse response = new();
        response.Corpora.AddRange(result.Select(ToProto));
        return response;
    }

    private static Proto.Job Unwrap(SubmitResult result) =>
        result switch
        {
            SubmitResult.Success ok => ToProto(ok.Job),
            SubmitResult.InvalidInput bad => throw new RpcException(new Status(StatusCode.InvalidArgument, bad.Message)),
            SubmitResult.NotFound missing => throw new RpcException(new Status(StatusCode.NotFound, missing.Message)),
            _ => throw new RpcException(new Status(StatusCode.Internal, "unexpected result")),
        };

    private static Proto.Job ToProto(JobRecord job)
    {
        Proto.Job proto = new()
        {
            Id = job.Id,
            Type = job.Type == JobType.Ingestion ? Proto.JobType.Ingestion : Proto.JobType.Analysis,
            CorpusId = job.CorpusId,
            Filter = ToProto(job.Filter),
            Status = ToProtoStatus(job.Status),
            CreatedAtMs = job.CreatedAtMs,
            StartedAtMs = job.StartedAtMs,
            FinishedAtMs = job.FinishedAtMs,
            SparkApplication = job.SparkApplication,
            Error = job.Error,
        };
        if (job.Source is not null)
        {
            proto.Source = ToProto(job.Source);
        }

        proto.AnalysisKinds.AddRange(job.AnalysisKinds.Select(ToProtoKind));
        return proto;
    }

    private static Proto.Corpus ToProto(CorpusRecord corpus) => new()
    {
        Id = corpus.Id,
        Source = ToProto(corpus.Source),
        Filter = ToProto(corpus.Filter),
        GameCount = corpus.GameCount,
        CreatedAtMs = corpus.CreatedAtMs,
    };

    private static IngestionSource ToProto(IngestionSourceSpec source) =>
        source.Kind == SourceKind.Lichess
            ? new IngestionSource { LichessMonth = new LichessMonth { YearMonth = source.LichessYearMonth } }
            : new IngestionSource { Upload = new PgnUpload { ObjectKey = source.UploadObjectKey, Label = source.UploadLabel } };

    private static CorpusFilter ToProto(CorpusFilterSpec filter) => new()
    {
        RatingBand = filter.RatingBand,
        TimeControl = filter.TimeControl,
        DateFromMs = filter.DateFromMs,
        DateToMs = filter.DateToMs,
        SampleRate = filter.SampleRate,
    };

    private static CorpusFilterSpec ToFilter(CorpusFilter? f) =>
        f is null
            ? CorpusFilterSpec.Empty
            : new CorpusFilterSpec(f.RatingBand, f.TimeControl, f.DateFromMs, f.DateToMs, f.SampleRate);

    private static Proto.JobStatus ToProtoStatus(JobStatus status) =>
        status switch
        {
            JobStatus.Pending => Proto.JobStatus.Pending,
            JobStatus.Running => Proto.JobStatus.Running,
            JobStatus.Succeeded => Proto.JobStatus.Succeeded,
            _ => Proto.JobStatus.Failed,
        };

    private static JobStatus FromProtoStatus(Proto.JobStatus status) =>
        status switch
        {
            Proto.JobStatus.Running => JobStatus.Running,
            Proto.JobStatus.Succeeded => JobStatus.Succeeded,
            Proto.JobStatus.Failed => JobStatus.Failed,
            _ => JobStatus.Pending,
        };

    private static Proto.AnalysisKind ToProtoKind(AnalysisKind kind) =>
        kind switch
        {
            AnalysisKind.Openings => Proto.AnalysisKind.Openings,
            AnalysisKind.Endgames => Proto.AnalysisKind.Endgames,
            AnalysisKind.Positions => Proto.AnalysisKind.Positions,
            AnalysisKind.Tricky => Proto.AnalysisKind.Tricky,
            _ => Proto.AnalysisKind.Summary,
        };

    private static string KindName(Proto.AnalysisKind kind) =>
        kind switch
        {
            Proto.AnalysisKind.Openings => "openings",
            Proto.AnalysisKind.Endgames => "endgames",
            Proto.AnalysisKind.Positions => "positions",
            Proto.AnalysisKind.Tricky => "tricky",
            Proto.AnalysisKind.Summary => "summary",
            _ => string.Empty,
        };

    private static string UserId(ServerCallContext context)
    {
        Metadata.Entry? entry = context.RequestHeaders.Get("x-user-id");
        return entry?.Value ?? string.Empty;
    }
}
</content>
