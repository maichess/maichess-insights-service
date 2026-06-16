using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using MaichessInsightsService.Tests.Support;
using Xunit;

namespace MaichessInsightsService.Tests;

public class JobServiceTests
{
    private const string User = "user-1";

    private readonly FakeInsightsStore store = new();
    private readonly FakeSparkJobLauncher launcher = new();
    private readonly FakeJobEventProducer events = new();
    private readonly JobService service;
    private int idSeq;

    public JobServiceTests()
    {
        service = new JobService(
            store, launcher, events, new InsightsOptions { MongoUri = "mongodb://m" }, () => $"gen-{++idSeq}", () => 1000);
    }

    // ─── SubmitIngestion ────────────────────────────────────────────────────

    [Fact]
    public async Task IngestLichessRecordsCorpusJobAndLaunch()
    {
        SubmitResult result = await service.SubmitIngestionAsync(
            new IngestionInput(new LichessMonthInput("2024-12"), null, CorpusFilterSpec.Empty), User, default);

        JobRecord job = Assert.IsType<SubmitResult.Success>(result).Job;
        Assert.Equal("job-1", job.Id);
        Assert.Equal(JobType.Ingestion, job.Type);
        Assert.Equal("lichess-2024-12", job.CorpusId);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal("insights-ingest-job-1", job.SparkApplication);
        Assert.Equal(User, job.SubmittedBy);
        Assert.Empty(job.AnalysisKinds);
        Assert.Equal(1000, job.CreatedAtMs);
        Assert.Equal(0, job.StartedAtMs);
        Assert.Equal(0, job.FinishedAtMs);
        Assert.Equal(string.Empty, job.Error);
        Assert.Equal(SourceKind.Lichess, job.Source!.Kind);

        SparkSpec spec = launcher.Last;
        Assert.Equal(JobType.Ingestion, spec.Type);
        Assert.Equal("insights-ingest-job-1", spec.ApplicationName);
        Assert.Equal("maichess.insights.ingest.IngestJob", spec.MainClass);
        Assert.Contains("lichess-2024-12", spec.Arguments);

        CorpusRecord corpus = store.Corpora["lichess-2024-12"];
        Assert.Equal("lichess-2024-12", corpus.Id);
        Assert.Equal(0, corpus.GameCount);
        Assert.Equal(1000, corpus.CreatedAtMs);
        Assert.Equal(SourceKind.Lichess, corpus.Source.Kind);
        Assert.Equal(CorpusFilterSpec.Empty, corpus.Filter);

        Assert.Equal(("submitted", "job-1"), Assert.Single(events.Events));
    }

    [Fact]
    public async Task IngestUploadRoutesToUploadSource()
    {
        SubmitResult result = await service.SubmitIngestionAsync(
            new IngestionInput(null, new UploadInput("uploads/x.pgn", "club"), CorpusFilterSpec.Empty), User, default);

        JobRecord job = Assert.IsType<SubmitResult.Success>(result).Job;
        Assert.Equal("upload-gen-1", job.CorpusId);
        Assert.Equal(SourceKind.Upload, job.Source!.Kind);
        Assert.Equal("uploads/x.pgn", job.Source.UploadObjectKey);
        Assert.Equal("club", job.Source.UploadLabel);
        Assert.Equal("upload", Value(launcher.Last.Arguments, "--source-type"));
        Assert.Equal("uploads/x.pgn", Value(launcher.Last.Arguments, "--upload-key"));
        Assert.True(store.Corpora.ContainsKey("upload-gen-1"));
    }

    [Fact]
    public async Task ReingestingSameLichessSliceReusesTheCorpus()
    {
        IngestionInput input = new(new LichessMonthInput("2024-12"), null, CorpusFilterSpec.Empty);
        await service.SubmitIngestionAsync(input, User, default);
        await service.SubmitIngestionAsync(input, User, default);

        Assert.Single(store.Corpora);
        Assert.Equal(2, store.Jobs.Count);
    }

    [Theory]
    [InlineData(null, null, "exactly one of lichess_month or upload is required")]
    [InlineData("2024-12", "uploads/x.pgn", "exactly one of lichess_month or upload is required")]
    public async Task IngestRejectsWrongSourceCount(string? month, string? uploadKey, string expected)
    {
        IngestionInput input = new(
            month is null ? null : new LichessMonthInput(month),
            uploadKey is null ? null : new UploadInput(uploadKey, string.Empty),
            CorpusFilterSpec.Empty);

        SubmitResult result = await service.SubmitIngestionAsync(input, User, default);
        Assert.Equal(expected, Assert.IsType<SubmitResult.InvalidInput>(result).Message);
        Assert.Empty(store.Jobs);
        Assert.Empty(launcher.Launched);
    }

    [Theory]
    [InlineData("2024-13")]
    [InlineData("2024-1")]
    [InlineData("not-a-month")]
    public async Task IngestRejectsMalformedYearMonth(string month)
    {
        SubmitResult result = await service.SubmitIngestionAsync(
            new IngestionInput(new LichessMonthInput(month), null, CorpusFilterSpec.Empty), User, default);
        Assert.Equal(
            "lichess_month.year_month must be in YYYY-MM form",
            Assert.IsType<SubmitResult.InvalidInput>(result).Message);
    }

    [Fact]
    public async Task IngestRejectsEmptyUploadKey()
    {
        SubmitResult result = await service.SubmitIngestionAsync(
            new IngestionInput(null, new UploadInput("  ", "label"), CorpusFilterSpec.Empty), User, default);
        Assert.Equal("upload.object_key is required", Assert.IsType<SubmitResult.InvalidInput>(result).Message);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(-0.1)]
    public async Task IngestRejectsOutOfRangeSampleRate(double rate)
    {
        SubmitResult result = await service.SubmitIngestionAsync(
            new IngestionInput(new LichessMonthInput("2024-12"), null, new CorpusFilterSpec("", "", 0, 0, rate)),
            User,
            default);
        Assert.Equal("sample_rate must be within (0,1]", Assert.IsType<SubmitResult.InvalidInput>(result).Message);
    }

    // ─── SubmitAnalysis ─────────────────────────────────────────────────────

    [Fact]
    public async Task AnalysisWithNoKindsRunsAllKinds()
    {
        await SeedCorpusAsync("lichess-2024-12");

        SubmitResult result = await service.SubmitAnalysisAsync(
            new AnalysisInput("lichess-2024-12", []), "user-2", default);

        JobRecord job = Assert.IsType<SubmitResult.Success>(result).Job;
        Assert.Equal(JobType.Analysis, job.Type);
        Assert.Equal(AnalysisKindNames.All, job.AnalysisKinds);
        Assert.Equal("insights-analysis-job-2", job.SparkApplication);
        Assert.Equal("user-2", job.SubmittedBy);
        Assert.Equal(SourceKind.Lichess, job.Source!.Kind);
        Assert.Equal("all", Value(launcher.Last.Arguments, "--jobs"));
        Assert.Equal("job-2", Value(launcher.Last.Arguments, "--job-id"));
        Assert.Equal("insights-analysis-job-2", Value(launcher.Last.Arguments, "--spark-application"));
        Assert.Equal(("submitted", "job-2"), events.Events[^1]);
    }

    [Fact]
    public async Task AnalysisSubsetIsDeduplicated()
    {
        await SeedCorpusAsync("cid");

        SubmitResult result = await service.SubmitAnalysisAsync(
            new AnalysisInput("cid", ["openings", "tricky", "openings"]), User, default);

        JobRecord job = Assert.IsType<SubmitResult.Success>(result).Job;
        Assert.Equal([AnalysisKind.Openings, AnalysisKind.Tricky], job.AnalysisKinds);
        Assert.Equal("openings,tricky", Value(launcher.Last.Arguments, "--jobs"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalysisRejectsMissingCorpusId(string corpusId)
    {
        SubmitResult result = await service.SubmitAnalysisAsync(new AnalysisInput(corpusId, []), User, default);
        Assert.Equal("corpus_id is required", Assert.IsType<SubmitResult.InvalidInput>(result).Message);
    }

    [Fact]
    public async Task AnalysisRejectsUnknownKindBeforeCorpusLookup()
    {
        SubmitResult result = await service.SubmitAnalysisAsync(
            new AnalysisInput("cid", ["openings", "bogus"]), User, default);
        Assert.Equal("unknown analysis kind 'bogus'", Assert.IsType<SubmitResult.InvalidInput>(result).Message);
        Assert.Empty(store.Jobs);
    }

    [Fact]
    public async Task AnalysisReturnsNotFoundForUnknownCorpus()
    {
        SubmitResult result = await service.SubmitAnalysisAsync(new AnalysisInput("missing", []), User, default);
        Assert.Equal("corpus missing not found", Assert.IsType<SubmitResult.NotFound>(result).Message);
        Assert.Empty(launcher.Launched);
    }

    // ─── Reads ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobReturnsJobOrNull()
    {
        await service.SubmitIngestionAsync(
            new IngestionInput(new LichessMonthInput("2024-12"), null, CorpusFilterSpec.Empty), User, default);

        Assert.NotNull(await service.GetJobAsync("job-1", default));
        Assert.Null(await service.GetJobAsync("nope", default));
    }

    [Fact]
    public async Task ListJobsOrdersNewestFirstAndFiltersByStatus()
    {
        await InsertJobAsync(JobStatus.Pending, createdAt: 1);
        await InsertJobAsync(JobStatus.Running, createdAt: 3);
        await InsertJobAsync(JobStatus.Pending, createdAt: 2);

        IReadOnlyList<JobRecord> all = await service.ListJobsAsync(null, 0, 0, default);
        Assert.Equal([3, 2, 1], all.Select(job => job.CreatedAtMs));

        IReadOnlyList<JobRecord> pending = await service.ListJobsAsync(JobStatus.Pending, 0, 0, default);
        Assert.Equal(2, pending.Count);
        Assert.All(pending, job => Assert.Equal(JobStatus.Pending, job.Status));
    }

    [Fact]
    public async Task ListJobsClampsAndOffsets()
    {
        await InsertJobAsync(JobStatus.Pending, createdAt: 1);
        await InsertJobAsync(JobStatus.Pending, createdAt: 2);

        Assert.Single(await service.ListJobsAsync(null, 1, 0, default)); // limit clamp to 1
        Assert.Equal(2, (await service.ListJobsAsync(null, 999, 0, default)).Count); // limit clamped to max=100
        Assert.Single(await service.ListJobsAsync(null, 0, 1, default)); // default limit, offset 1
        Assert.Equal(2, (await service.ListJobsAsync(null, 0, -5, default)).Count); // negative offset clamped to 0
    }

    [Fact]
    public async Task ListCorporaAndGetCorpus()
    {
        await SeedCorpusAsync("a", createdAt: 1);
        await SeedCorpusAsync("b", createdAt: 2);

        IReadOnlyList<CorpusRecord> corpora = await service.ListCorporaAsync(0, 0, default);
        Assert.Equal(["b", "a"], corpora.Select(corpus => corpus.Id));
        Assert.Equal(2, corpora[0].CreatedAtMs);

        Assert.NotNull(await service.GetCorpusAsync("a", default));
        Assert.Null(await service.GetCorpusAsync("missing", default));
    }

    private async Task SeedCorpusAsync(string id, long createdAt = 0) =>
        await store.InsertCorpusAsync(
            new CorpusRecord
            {
                Id = id,
                Source = IngestionSourceSpec.Lichess("2024-12"),
                Filter = CorpusFilterSpec.Empty,
                CreatedAtMs = createdAt,
            },
            default);

    private async Task InsertJobAsync(JobStatus status, long createdAt)
    {
        JobRecord job = await store.InsertJobAsync(
            new JobRecord { Type = JobType.Ingestion, Status = status, CreatedAtMs = createdAt }, default);
        job.Status = status;
        await store.UpdateJobAsync(job, default);
    }

    private static string? Value(IReadOnlyList<string> args, string flag)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
</content>
