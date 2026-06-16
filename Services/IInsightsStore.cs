using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Persistence boundary for the insights catalog (insights_jobs + insights_corpora),
// backed by the insights-db DatabaseService instance. An interface so the control-plane
// logic is tested against an in-memory substitute.
internal interface IInsightsStore
{
    // Inserts a new job and returns it with its assigned id.
    Task<JobRecord> InsertJobAsync(JobRecord job, CancellationToken ct);

    // Persists changes to an existing job.
    Task UpdateJobAsync(JobRecord job, CancellationToken ct);

    // Returns a job by id, or null when it does not exist.
    Task<JobRecord?> GetJobAsync(string id, CancellationToken ct);

    // Returns jobs filtered by status (null = all), newest first.
    Task<IReadOnlyList<JobRecord>> ListJobsAsync(JobStatus? status, int limit, int offset, CancellationToken ct);

    // Inserts a new corpus (id is supplied by the caller).
    Task InsertCorpusAsync(CorpusRecord corpus, CancellationToken ct);

    // Returns a corpus by id, or null when it does not exist.
    Task<CorpusRecord?> GetCorpusAsync(string id, CancellationToken ct);

    // Returns corpora, newest first.
    Task<IReadOnlyList<CorpusRecord>> ListCorporaAsync(int limit, int offset, CancellationToken ct);
}
</content>
