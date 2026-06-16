using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class SparkApplicationStateTests
{
    [Theory]
    [InlineData("RUNNING", JobStatus.Running)]
    [InlineData("running", JobStatus.Running)]
    [InlineData("SUCCEEDING", JobStatus.Running)]
    [InlineData("COMPLETED", JobStatus.Succeeded)]
    [InlineData("FAILED", JobStatus.Failed)]
    [InlineData("FAILING", JobStatus.Failed)]
    [InlineData("SUBMISSION_FAILED", JobStatus.Failed)]
    public void RecognizedStatesMap(string state, JobStatus expected) =>
        Assert.Equal(expected, SparkApplicationState.ToJobStatus(state, JobStatus.Pending));

    [Theory]
    [InlineData("")]
    [InlineData("SUBMITTED")]
    [InlineData("PENDING_RERUN")]
    [InlineData("UNKNOWN")]
    [InlineData(null)]
    public void UnrecognizedOrNonTerminalKeepsCurrent(string? state) =>
        Assert.Equal(JobStatus.Pending, SparkApplicationState.ToJobStatus(state, JobStatus.Pending));
}
</content>
