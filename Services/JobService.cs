using System.Text.RegularExpressions;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// The insights control plane: validate a submit, choose the corpus + job(s), build the
// SparkApplication spec, record the job, and launch it. Pure decision/validation logic
// over injected seams (store / launcher / events) so it is fully testable without k8s.
internal sealed partial class JobService(
    IInsightsStore store,
    ISparkJobLauncher launcher,
    IInsightsJobEventProducer events,
    InsightsOptions options,
    Func<string> idGen,
    Func<long> clock)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    internal async Task<SubmitResult> SubmitIngestionAsync(
        IngestionInput input, string submittedBy, CancellationToken ct)
    {
        string? error = ValidateIngestion(input);
        if (error is not null)
        {
            return new SubmitResult.InvalidInput(error);
        }

        CorpusFilterSpec filter = input.Filter;
        IngestionSourceSpec source;
        string corpusId;
        if (input.Lichess is { } lichess)
        {
            source = IngestionSourceSpec.Lichess(lichess.YearMonth);
            corpusId = CorpusId.ForLichess(lichess.YearMonth, filter);
        }
        else
        {
            UploadInput upload = input.Upload!;
            source = IngestionSourceSpec.Upload(upload.ObjectKey, upload.Label);
            corpusId = CorpusId.ForUpload(idGen());
        }

        await EnsureCorpusAsync(corpusId, source, filter, ct);

        JobRecord job = new()
        {
            Type = JobType.Ingestion,
            CorpusId = corpusId,
            Source = source,
            Filter = filter,
            Status = JobStatus.Pending,
            AnalysisKinds = [],
            CreatedAtMs = clock(),
            SubmittedBy = submittedBy,
        };

        SparkSpec spec = new(
            SparkApplicationName.For(JobType.Ingestion, await InsertAndIdAsync(job, ct)),
            JobType.Ingestion,
            options.IngestionMainClass,
            SparkArguments.ForIngestion(corpusId, source, filter, options));

        return await LaunchAsync(job, spec, ct);
    }

    internal async Task<SubmitResult> SubmitAnalysisAsync(
        AnalysisInput input, string submittedBy, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.CorpusId))
        {
            return new SubmitResult.InvalidInput("corpus_id is required");
        }

        List<AnalysisKind> kinds = [];
        foreach (string raw in input.Kinds)
        {
            if (!AnalysisKindNames.TryParse(raw, out AnalysisKind kind))
            {
                return new SubmitResult.InvalidInput($"unknown analysis kind '{raw}'");
            }

            if (!kinds.Contains(kind))
            {
                kinds.Add(kind);
            }
        }

        if (kinds.Count == 0)
        {
            kinds = [.. AnalysisKindNames.All];
        }

        CorpusRecord? corpus = await store.GetCorpusAsync(input.CorpusId, ct);
        if (corpus is null)
        {
            return new SubmitResult.NotFound($"corpus {input.CorpusId} not found");
        }

        JobRecord job = new()
        {
            Type = JobType.Analysis,
            CorpusId = corpus.Id,
            Source = corpus.Source,
            Filter = corpus.Filter,
            Status = JobStatus.Pending,
            AnalysisKinds = kinds,
            CreatedAtMs = clock(),
            SubmittedBy = submittedBy,
        };

        string jobId = await InsertAndIdAsync(job, ct);
        string appName = SparkApplicationName.For(JobType.Analysis, jobId);
        SparkSpec spec = new(
            appName,
            JobType.Analysis,
            options.AnalysisMainClass,
            SparkArguments.ForAnalysis(corpus.Id, jobId, appName, kinds, options));

        return await LaunchAsync(job, spec, ct);
    }

    internal Task<JobRecord?> GetJobAsync(string id, CancellationToken ct) => store.GetJobAsync(id, ct);

    internal Task<IReadOnlyList<JobRecord>> ListJobsAsync(
        JobStatus? status, int limit, int offset, CancellationToken ct) =>
        store.ListJobsAsync(status, Clamp(limit), Math.Max(offset, 0), ct);

    internal Task<IReadOnlyList<CorpusRecord>> ListCorporaAsync(int limit, int offset, CancellationToken ct) =>
        store.ListCorporaAsync(Clamp(limit), Math.Max(offset, 0), ct);

    internal Task<CorpusRecord?> GetCorpusAsync(string id, CancellationToken ct) => store.GetCorpusAsync(id, ct);

    private static string? ValidateIngestion(IngestionInput input)
    {
        int sources = (input.Lichess is null ? 0 : 1) + (input.Upload is null ? 0 : 1);
        if (sources != 1)
        {
            return "exactly one of lichess_month or upload is required";
        }

        if (input.Lichess is { } lichess && !YearMonthPattern().IsMatch(lichess.YearMonth))
        {
            return "lichess_month.year_month must be in YYYY-MM form";
        }

        if (input.Upload is { } upload && string.IsNullOrWhiteSpace(upload.ObjectKey))
        {
            return "upload.object_key is required";
        }

        if (input.Filter.SampleRate < 0 || input.Filter.SampleRate > 1)
        {
            return "sample_rate must be within (0,1]";
        }

        return null;
    }

    private async Task EnsureCorpusAsync(
        string corpusId, IngestionSourceSpec source, CorpusFilterSpec filter, CancellationToken ct)
    {
        // Re-ingesting the same Lichess slice reuses its corpus record (the Spark job
        // overwrites the Parquet); a fresh upload always gets a new corpus id.
        CorpusRecord? existing = await store.GetCorpusAsync(corpusId, ct);
        if (existing is not null)
        {
            return;
        }

        await store.InsertCorpusAsync(
            new CorpusRecord { Id = corpusId, Source = source, Filter = filter, GameCount = 0, CreatedAtMs = clock() },
            ct);
    }

    private async Task<string> InsertAndIdAsync(JobRecord job, CancellationToken ct)
    {
        JobRecord inserted = await store.InsertJobAsync(job, ct);
        job.Id = inserted.Id;
        return inserted.Id;
    }

    private async Task<SubmitResult> LaunchAsync(JobRecord job, SparkSpec spec, CancellationToken ct)
    {
        job.SparkApplication = spec.ApplicationName;
        await store.UpdateJobAsync(job, ct);
        await launcher.LaunchAsync(spec, ct);
        await events.JobSubmittedAsync(job, ct);
        return new SubmitResult.Success(job);
    }

    private static int Clamp(int limit) => limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex YearMonthPattern();
}
