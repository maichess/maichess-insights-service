namespace MaichessInsightsService.Domain;

// Lifecycle of a Spark job as tracked by the control plane.
internal enum JobStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
}
</content>
