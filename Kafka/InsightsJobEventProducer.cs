using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Maichess.Events.V1;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Kafka;

// Emits insights.events.v1 job-lifecycle events (relayed to socket.outbound.v1 for the
// submitting user). Keyed by jobId. Excluded from coverage: live-Kafka producer shell;
// the lifecycle decisions live in the tested JobService + SparkStatusReconciler.
[ExcludeFromCodeCoverage]
internal sealed class InsightsJobEventProducer : IInsightsJobEventProducer, IDisposable
{
    public const string Topic = "insights.events.v1";

    private readonly IProducer<string, InsightsJobEvent> producer;
    private readonly Func<long> clock;
    private readonly Func<string> idGen;

    public InsightsJobEventProducer(string bootstrapServers, ISchemaRegistryClient registry, Func<long> clock, Func<string> idGen)
    {
        this.clock = clock;
        this.idGen = idGen;
        ProducerConfig config = new() { BootstrapServers = bootstrapServers, EnableIdempotence = true };
        producer = new ProducerBuilder<string, InsightsJobEvent>(config)
            .SetValueSerializer(ProtobufEventSerdes.Serializer<InsightsJobEvent>(registry))
            .Build();
    }

    public Task JobSubmittedAsync(JobRecord job, CancellationToken ct) =>
        Emit(
            job,
            "insights.JobSubmitted",
            e => e.JobSubmitted = new JobSubmitted
            {
                JobId = job.Id,
                Kind = job.Type == JobType.Ingestion ? JobKind.Ingestion : JobKind.Analysis,
                CorpusId = job.CorpusId,
                UserId = job.SubmittedBy,
            },
            ct);

    public Task JobRunningAsync(JobRecord job, CancellationToken ct) =>
        Emit(
            job,
            "insights.JobRunning",
            e => e.JobRunning = new JobRunning
            {
                JobId = job.Id, CorpusId = job.CorpusId, UserId = job.SubmittedBy,
            },
            ct);

    public Task JobSucceededAsync(JobRecord job, CancellationToken ct) =>
        Emit(
            job,
            "insights.JobSucceeded",
            e => e.JobSucceeded = new JobSucceeded
            {
                JobId = job.Id, CorpusId = job.CorpusId, UserId = job.SubmittedBy,
            },
            ct);

    public Task JobFailedAsync(JobRecord job, CancellationToken ct) =>
        Emit(
            job,
            "insights.JobFailed",
            e => e.JobFailed = new JobFailed
            {
                JobId = job.Id, CorpusId = job.CorpusId, UserId = job.SubmittedBy, Message = job.Error,
            },
            ct);

    public void Dispose() => producer.Dispose();

    private async Task Emit(JobRecord job, string eventType, Action<InsightsJobEvent> setPayload, CancellationToken ct)
    {
        InsightsJobEvent envelope = new()
        {
            EventId = idGen(),
            EventType = eventType,
            AggregateId = job.Id,
            OccurredAt = clock(),
            Producer = "insights-service",
            CorrelationId = job.Id,
        };
        setPayload(envelope);
        await producer.ProduceAsync(Topic, new Message<string, InsightsJobEvent> { Key = job.Id, Value = envelope }, ct);
    }
}
