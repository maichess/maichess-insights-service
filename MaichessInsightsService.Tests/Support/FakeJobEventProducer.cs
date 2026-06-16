using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// Records emitted lifecycle events so the control plane's event fan-out is asserted.
internal sealed class FakeJobEventProducer : IInsightsJobEventProducer
{
    internal List<(string Event, string JobId)> Events { get; } = [];

    public Task JobSubmittedAsync(JobRecord job, CancellationToken ct)
    {
        Events.Add(("submitted", job.Id));
        return Task.CompletedTask;
    }

    public Task JobRunningAsync(JobRecord job, CancellationToken ct)
    {
        Events.Add(("running", job.Id));
        return Task.CompletedTask;
    }

    public Task JobSucceededAsync(JobRecord job, CancellationToken ct)
    {
        Events.Add(("succeeded", job.Id));
        return Task.CompletedTask;
    }

    public Task JobFailedAsync(JobRecord job, CancellationToken ct)
    {
        Events.Add(("failed", job.Id));
        return Task.CompletedTask;
    }
}
