namespace MaichessInsightsService.Domain;

// Maps JobStatus to/from the lowercase string persisted in insights_jobs and used on
// the wire. The Spark side writes "succeeded"/"failed" terminal states with the same
// spelling, so the control plane reads either writer's records uniformly.
internal static class JobStatusNames
{
    internal static string ToName(JobStatus status) =>
        status switch
        {
            JobStatus.Pending => "pending",
            JobStatus.Running => "running",
            JobStatus.Succeeded => "succeeded",
            _ => "failed",
        };

    internal static JobStatus FromName(string name) =>
        name switch
        {
            "running" => JobStatus.Running,
            "succeeded" => JobStatus.Succeeded,
            "failed" => JobStatus.Failed,
            _ => JobStatus.Pending,
        };

    internal static bool TryParse(string raw, out JobStatus status)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "pending": status = JobStatus.Pending; return true;
            case "running": status = JobStatus.Running; return true;
            case "succeeded": status = JobStatus.Succeeded; return true;
            case "failed": status = JobStatus.Failed; return true;
            default: status = default; return false;
        }
    }
}
</content>
