using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// Scenario-scoped wiring for the job-dispatch steps: an in-memory store, a recording
// launcher / event producer, and a fixed clock + deterministic id generator.
internal sealed class JobDispatchContext
{
    internal FakeInsightsStore Store { get; } = new();

    internal FakeSparkJobLauncher Launcher { get; } = new();

    internal FakeJobEventProducer Events { get; } = new();

    internal JobService Service { get; }

    internal SubmitResult? Result { get; set; }

    internal JobDispatchContext()
    {
        Service = new JobService(
            Store, Launcher, Events, new InsightsOptions { MongoUri = "mongodb://m" }, NextId, () => 1000);
    }

    private int idSeq;

    private string NextId() => $"gen-{++idSeq}";
}
