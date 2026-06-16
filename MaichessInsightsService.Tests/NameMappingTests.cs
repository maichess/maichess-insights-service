using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class NameMappingTests
{
    [Fact]
    public void AnalysisKindAllHasEveryKind() =>
        Assert.Equal(5, AnalysisKindNames.All.Count);

    [Theory]
    [InlineData(AnalysisKind.Openings, "openings")]
    [InlineData(AnalysisKind.Endgames, "endgames")]
    [InlineData(AnalysisKind.Positions, "positions")]
    [InlineData(AnalysisKind.Tricky, "tricky")]
    [InlineData(AnalysisKind.Summary, "summary")]
    public void AnalysisKindToName(AnalysisKind kind, string expected) =>
        Assert.Equal(expected, AnalysisKindNames.ToName(kind));

    [Theory]
    [InlineData("openings", AnalysisKind.Openings)]
    [InlineData("ENDGAMES", AnalysisKind.Endgames)]
    [InlineData(" positions ", AnalysisKind.Positions)]
    [InlineData("tricky", AnalysisKind.Tricky)]
    [InlineData("summary", AnalysisKind.Summary)]
    public void AnalysisKindTryParseKnown(string raw, AnalysisKind expected)
    {
        Assert.True(AnalysisKindNames.TryParse(raw, out AnalysisKind kind));
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bogus")]
    public void AnalysisKindTryParseUnknown(string raw) =>
        Assert.False(AnalysisKindNames.TryParse(raw, out _));

    [Theory]
    [InlineData(JobStatus.Pending, "pending")]
    [InlineData(JobStatus.Running, "running")]
    [InlineData(JobStatus.Succeeded, "succeeded")]
    [InlineData(JobStatus.Failed, "failed")]
    public void JobStatusToName(JobStatus status, string expected) =>
        Assert.Equal(expected, JobStatusNames.ToName(status));

    [Theory]
    [InlineData("running", JobStatus.Running)]
    [InlineData("succeeded", JobStatus.Succeeded)]
    [InlineData("failed", JobStatus.Failed)]
    [InlineData("pending", JobStatus.Pending)]
    [InlineData("anything-else", JobStatus.Pending)]
    public void JobStatusFromName(string name, JobStatus expected) =>
        Assert.Equal(expected, JobStatusNames.FromName(name));

    [Theory]
    [InlineData("pending", JobStatus.Pending)]
    [InlineData("RUNNING", JobStatus.Running)]
    [InlineData(" succeeded ", JobStatus.Succeeded)]
    [InlineData("failed", JobStatus.Failed)]
    public void JobStatusTryParseKnown(string raw, JobStatus expected)
    {
        Assert.True(JobStatusNames.TryParse(raw, out JobStatus status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void JobStatusTryParseUnknown() =>
        Assert.False(JobStatusNames.TryParse("queued", out _));

    [Theory]
    [InlineData(JobType.Ingestion, "ingestion")]
    [InlineData(JobType.Analysis, "analysis")]
    public void JobTypeToName(JobType type, string expected) =>
        Assert.Equal(expected, JobTypeNames.ToName(type));

    [Theory]
    [InlineData("analysis", JobType.Analysis)]
    [InlineData("ingestion", JobType.Ingestion)]
    [InlineData("other", JobType.Ingestion)]
    public void JobTypeFromName(string name, JobType expected) =>
        Assert.Equal(expected, JobTypeNames.FromName(name));
}
</content>
