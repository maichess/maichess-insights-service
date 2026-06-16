using System.Diagnostics.CodeAnalysis;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Kafka;

// Used when Kafka is disabled: job lifecycle is still tracked durably in insights_jobs,
// just not pushed live. Excluded from coverage (trivial no-op glue).
[ExcludeFromCodeCoverage]
internal sealed class NoopInsightsJobEventProducer : IInsightsJobEventProducer
{
    public Task JobSubmittedAsync(JobRecord job, CancellationToken ct) => Task.CompletedTask;

    public Task JobRunningAsync(JobRecord job, CancellationToken ct) => Task.CompletedTask;

    public Task JobSucceededAsync(JobRecord job, CancellationToken ct) => Task.CompletedTask;

    public Task JobFailedAsync(JobRecord job, CancellationToken ct) => Task.CompletedTask;
}
