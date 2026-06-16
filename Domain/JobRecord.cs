namespace MaichessInsightsService.Domain;

// One ingestion or analysis run, recorded in insights_jobs. The control plane creates
// it (PENDING) on submit and advances Status/Started/Finished as the backing
// SparkApplication progresses. SubmittedBy is the authenticated user, used for live
// event fan-out (not part of the public Job shape).
internal sealed class JobRecord
{
    public string Id { get; set; } = string.Empty;

    public JobType Type { get; set; }

    public string CorpusId { get; set; } = string.Empty;

    // Set on ingestion jobs; describes the source that produced the corpus.
    public IngestionSourceSpec? Source { get; set; }

    public CorpusFilterSpec Filter { get; set; } = CorpusFilterSpec.Empty;

    public JobStatus Status { get; set; }

    // The analysis jobs requested; empty for ingestion jobs.
    public IReadOnlyList<AnalysisKind> AnalysisKinds { get; set; } = [];

    public long CreatedAtMs { get; set; }

    public long StartedAtMs { get; set; }

    public long FinishedAtMs { get; set; }

    public string SparkApplication { get; set; } = string.Empty;

    public string SubmittedBy { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;
}
</content>
