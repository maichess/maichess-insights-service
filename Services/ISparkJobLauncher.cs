using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Launch seam for Spark jobs. The real implementation creates a SparkApplication CR
// via the C# Kubernetes client; JobService is tested against a fake so its decisions
// (which job, what spec) are verified without a cluster.
internal interface ISparkJobLauncher
{
    Task LaunchAsync(SparkSpec spec, CancellationToken ct);
}
