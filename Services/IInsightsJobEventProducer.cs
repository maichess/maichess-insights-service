using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Emits job-lifecycle events to insights.events.v1 (relayed to socket.outbound.v1 for
// the submitting user). Optional: a no-op producer is used when Kafka is disabled.
// JobService emits the Submitted event; the status reconciler emits the rest as the
// backing SparkApplication advances.
internal interface IInsightsJobEventProducer
{
    Task JobSubmittedAsync(JobRecord job, CancellationToken ct);

    Task JobRunningAsync(JobRecord job, CancellationToken ct);

    Task JobSucceededAsync(JobRecord job, CancellationToken ct);

    Task JobFailedAsync(JobRecord job, CancellationToken ct);
}
