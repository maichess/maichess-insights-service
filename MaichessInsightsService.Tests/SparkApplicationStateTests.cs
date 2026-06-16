using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class SparkApplicationStateTests
{
    [Theory]
    [InlineData("RUNNING", (int)JobStatus.Running)]
    [InlineData("running", (int)JobStatus.Running)]
    [InlineData("SUCCEEDING", (int)JobStatus.Running)]
    [InlineData("COMPLETED", (int)JobStatus.Succeeded)]
    [InlineData("FAILED", (int)JobStatus.Failed)]
    [InlineData("FAILING", (int)JobStatus.Failed)]
    [InlineData("SUBMISSION_FAILED", (int)JobStatus.Failed)]
    public void RecognizedStatesMap(string state, int expected) =>
        Assert.Equal((JobStatus)expected, SparkApplicationState.ToJobStatus(state, JobStatus.Pending));

    [Theory]
    [InlineData("")]
    [InlineData("SUBMITTED")]
    [InlineData("PENDING_RERUN")]
    [InlineData("UNKNOWN")]
    [InlineData(null)]
    public void UnrecognizedOrNonTerminalKeepsCurrent(string? state) =>
        Assert.Equal(JobStatus.Pending, SparkApplicationState.ToJobStatus(state, JobStatus.Pending));
}
