using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class NameMappingTests
{
    [Fact]
    public void AnalysisKindAllHasEveryKind() =>
        Assert.Equal(5, AnalysisKindNames.All.Count);

    [Theory]
    [InlineData((int)AnalysisKind.Openings, "openings")]
    [InlineData((int)AnalysisKind.Endgames, "endgames")]
    [InlineData((int)AnalysisKind.Positions, "positions")]
    [InlineData((int)AnalysisKind.Tricky, "tricky")]
    [InlineData((int)AnalysisKind.Summary, "summary")]
    public void AnalysisKindToName(int kind, string expected) =>
        Assert.Equal(expected, AnalysisKindNames.ToName((AnalysisKind)kind));

    [Theory]
    [InlineData("openings", (int)AnalysisKind.Openings)]
    [InlineData("ENDGAMES", (int)AnalysisKind.Endgames)]
    [InlineData(" positions ", (int)AnalysisKind.Positions)]
    [InlineData("tricky", (int)AnalysisKind.Tricky)]
    [InlineData("summary", (int)AnalysisKind.Summary)]
    public void AnalysisKindTryParseKnown(string raw, int expected)
    {
        Assert.True(AnalysisKindNames.TryParse(raw, out AnalysisKind kind));
        Assert.Equal((AnalysisKind)expected, kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bogus")]
    public void AnalysisKindTryParseUnknown(string raw) =>
        Assert.False(AnalysisKindNames.TryParse(raw, out _));

    [Theory]
    [InlineData((int)JobStatus.Pending, "pending")]
    [InlineData((int)JobStatus.Running, "running")]
    [InlineData((int)JobStatus.Succeeded, "succeeded")]
    [InlineData((int)JobStatus.Failed, "failed")]
    public void JobStatusToName(int status, string expected) =>
        Assert.Equal(expected, JobStatusNames.ToName((JobStatus)status));

    [Theory]
    [InlineData("running", (int)JobStatus.Running)]
    [InlineData("succeeded", (int)JobStatus.Succeeded)]
    [InlineData("failed", (int)JobStatus.Failed)]
    [InlineData("pending", (int)JobStatus.Pending)]
    [InlineData("anything-else", (int)JobStatus.Pending)]
    public void JobStatusFromName(string name, int expected) =>
        Assert.Equal((JobStatus)expected, JobStatusNames.FromName(name));

    [Theory]
    [InlineData("pending", (int)JobStatus.Pending)]
    [InlineData("RUNNING", (int)JobStatus.Running)]
    [InlineData(" succeeded ", (int)JobStatus.Succeeded)]
    [InlineData("failed", (int)JobStatus.Failed)]
    public void JobStatusTryParseKnown(string raw, int expected)
    {
        Assert.True(JobStatusNames.TryParse(raw, out JobStatus status));
        Assert.Equal((JobStatus)expected, status);
    }

    [Fact]
    public void JobStatusTryParseUnknown() =>
        Assert.False(JobStatusNames.TryParse("queued", out _));

    [Theory]
    [InlineData((int)JobType.Ingestion, "ingestion")]
    [InlineData((int)JobType.Analysis, "analysis")]
    public void JobTypeToName(int type, string expected) =>
        Assert.Equal(expected, JobTypeNames.ToName((JobType)type));

    [Theory]
    [InlineData("analysis", (int)JobType.Analysis)]
    [InlineData("ingestion", (int)JobType.Ingestion)]
    [InlineData("other", (int)JobType.Ingestion)]
    public void JobTypeFromName(string name, int expected) =>
        Assert.Equal((JobType)expected, JobTypeNames.FromName(name));
}
