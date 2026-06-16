using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class SparkApplicationNameTests
{
    [Fact]
    public void IngestionNameTiesToJobId() =>
        Assert.Equal("insights-ingest-job-1", SparkApplicationName.For(JobType.Ingestion, "job-1"));

    [Fact]
    public void AnalysisNameTiesToJobId() =>
        Assert.Equal("insights-analysis-job-1", SparkApplicationName.For(JobType.Analysis, "job-1"));

    [Fact]
    public void NameIsSluggedAndBoundedTo63Chars()
    {
        string name = SparkApplicationName.For(JobType.Ingestion, new string('a', 80));
        Assert.Equal(63, name.Length);
        Assert.StartsWith("insights-ingest-", name);
        Assert.DoesNotContain('-', name[16..]);
    }
}
</content>
