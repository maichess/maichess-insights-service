using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// Records the SparkApplication specs JobService asks to launch, so dispatch decisions
// are asserted without a cluster.
internal sealed class FakeSparkJobLauncher : ISparkJobLauncher
{
    internal List<SparkSpec> Launched { get; } = [];

    internal SparkSpec Last => Launched[^1];

    public Task LaunchAsync(SparkSpec spec, CancellationToken ct)
    {
        Launched.Add(spec);
        return Task.CompletedTask;
    }
}
</content>
