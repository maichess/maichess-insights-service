using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// In-memory IInsightsStore so the control plane can be driven without a database.
internal sealed class FakeInsightsStore : IInsightsStore
{
    private readonly Dictionary<string, JobRecord> jobs = [];
    private readonly Dictionary<string, CorpusRecord> corpora = [];
    private int jobSeq;

    internal IReadOnlyDictionary<string, JobRecord> Jobs => jobs;

    internal IReadOnlyDictionary<string, CorpusRecord> Corpora => corpora;

    public Task<JobRecord> InsertJobAsync(JobRecord job, CancellationToken ct)
    {
        job.Id = $"job-{++jobSeq}";
        jobs[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task UpdateJobAsync(JobRecord job, CancellationToken ct)
    {
        jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<JobRecord?> GetJobAsync(string id, CancellationToken ct) =>
        Task.FromResult(jobs.GetValueOrDefault(id));

    public Task<IReadOnlyList<JobRecord>> ListJobsAsync(JobStatus? status, int limit, int offset, CancellationToken ct)
    {
        IReadOnlyList<JobRecord> result =
        [
            .. jobs.Values
                .Where(job => status is null || job.Status == status)
                .OrderByDescending(job => job.CreatedAtMs)
                .Skip(offset)
                .Take(limit),
        ];
        return Task.FromResult(result);
    }

    public Task InsertCorpusAsync(CorpusRecord corpus, CancellationToken ct)
    {
        corpora[corpus.Id] = corpus;
        return Task.CompletedTask;
    }

    public Task<CorpusRecord?> GetCorpusAsync(string id, CancellationToken ct) =>
        Task.FromResult(corpora.GetValueOrDefault(id));

    public Task<IReadOnlyList<CorpusRecord>> ListCorporaAsync(int limit, int offset, CancellationToken ct)
    {
        IReadOnlyList<CorpusRecord> result =
        [
            .. corpora.Values
                .OrderByDescending(corpus => corpus.CreatedAtMs)
                .Skip(offset)
                .Take(limit),
        ];
        return Task.FromResult(result);
    }
}
</content>
