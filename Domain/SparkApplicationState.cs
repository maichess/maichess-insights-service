namespace MaichessInsightsService.Domain;

// Maps the Kubeflow Spark Operator's applicationState.state to the control-plane
// JobStatus. Non-terminal / unrecognized states leave the job's current status
// unchanged so a transient blank/UNKNOWN never regresses a tracked job.
internal static class SparkApplicationState
{
    internal static JobStatus ToJobStatus(string? state, JobStatus current) =>
        (state ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "RUNNING" => JobStatus.Running,
            "SUCCEEDING" => JobStatus.Running,
            "COMPLETED" => JobStatus.Succeeded,
            "FAILED" => JobStatus.Failed,
            "FAILING" => JobStatus.Failed,
            "SUBMISSION_FAILED" => JobStatus.Failed,
            _ => current,
        };
}
</content>
