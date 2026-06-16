namespace MaichessInsightsService.Domain;

// The parameters needed to launch one Spark job. JobService builds this purely; the
// SparkJobLauncher turns it into a SparkApplication custom resource (infra glue).
internal sealed record SparkSpec(
    string ApplicationName,
    JobType Type,
    string MainClass,
    IReadOnlyList<string> Arguments);
</content>
