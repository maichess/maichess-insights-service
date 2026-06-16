using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using k8s;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Data;

// Polls the SparkApplication status of non-terminal jobs and writes transitions back
// into insights_jobs, emitting the matching lifecycle event. Excluded from coverage:
// requires a live cluster + database. The status→JobStatus mapping is the only logic
// and is duplicated in the tested SparkApplicationState for assertion.
[ExcludeFromCodeCoverage]
internal sealed class SparkStatusReconciler(
    IKubernetes kube,
    IInsightsStore store,
    IInsightsJobEventProducer events,
    Func<long> clock,
    ILogger<SparkStatusReconciler> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval);
        do
        {
#pragma warning disable CA1031 // Resilient reconcile loop: log and continue on any per-tick failure.
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Spark status reconcile failed");
            }
#pragma warning restore CA1031
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        foreach (JobStatus status in (JobStatus[])[JobStatus.Pending, JobStatus.Running])
        {
            IReadOnlyList<JobRecord> jobs = await store.ListJobsAsync(status, 100, 0, ct);
            foreach (JobRecord job in jobs)
            {
                await ReconcileJobAsync(job, ct);
            }
        }
    }

    private async Task ReconcileJobAsync(JobRecord job, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(job.SparkApplication))
        {
            return;
        }

        JsonElement? state = await ReadStateAsync(job.SparkApplication, ct);
        var mapped = SparkApplicationState.ToJobStatus(state?.GetProperty("state").GetString(), job.Status);
        if (mapped == job.Status)
        {
            return;
        }

        job.Status = mapped;
        switch (mapped)
        {
            case JobStatus.Running:
                job.StartedAtMs = clock();
                await store.UpdateJobAsync(job, ct);
                await events.JobRunningAsync(job, ct);
                break;
            case JobStatus.Succeeded:
                job.FinishedAtMs = clock();
                await store.UpdateJobAsync(job, ct);
                await events.JobSucceededAsync(job, ct);
                break;
            case JobStatus.Failed:
                job.FinishedAtMs = clock();
                job.Error = state?.TryGetProperty("errorMessage", out JsonElement err) == true ? err.GetString() ?? string.Empty : string.Empty;
                await store.UpdateJobAsync(job, ct);
                await events.JobFailedAsync(job, ct);
                break;
            default:
                break;
        }
    }

    private async Task<JsonElement?> ReadStateAsync(string name, CancellationToken ct)
    {
        object raw = await kube.CustomObjects.GetNamespacedCustomObjectStatusAsync(
            SparkJobLauncher.Group, SparkJobLauncher.Version, "maichess", SparkJobLauncher.Plural, name, cancellationToken: ct);
        JsonElement root = JsonSerializer.SerializeToElement(raw);
        return root.TryGetProperty("status", out JsonElement statusEl)
            && statusEl.TryGetProperty("applicationState", out JsonElement appState)
            ? appState
            : null;
    }
}
