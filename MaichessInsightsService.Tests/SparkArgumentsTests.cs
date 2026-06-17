using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using Xunit;

namespace MaichessInsightsService.Tests;

public class SparkArgumentsTests
{
    private static readonly InsightsOptions Options = new()
    {
        ReplayBoard = true,
        RawBucket = "insights-raw",
        ParsedBucket = "insights-parsed",
        AggBucket = "insights-agg",
        MongoUri = "mongodb://m",
        MongoDb = "maichess",
        BookPlies = 10,
        MinReach = 50,
        MinSupport = 30,
    };

    [Fact]
    public void IngestionLichessMinimal()
    {
        IReadOnlyList<string> args = SparkArguments.ForIngestion(
            "lichess-2024-12", IngestionSourceSpec.Lichess("2024-12"), CorpusFilterSpec.Empty, Options);

        Assert.Equal(
            [
                "--corpus-id", "lichess-2024-12",
                "--source-type", "lichess", "--lichess-month", "2024-12",
                "--replay", "true",
                "--raw-bucket", "insights-raw",
                "--parsed-bucket", "insights-parsed",
                "--mongo-uri", "mongodb://m",
                "--mongo-db", "maichess",
            ],
            args);
    }

    [Fact]
    public void IngestionUploadUsesUploadKey()
    {
        IReadOnlyList<string> args = SparkArguments.ForIngestion(
            "upload-x", IngestionSourceSpec.Upload("uploads/x.pgn", "club"), CorpusFilterSpec.Empty, Options);

        Assert.Equal("upload", Value(args, "--source-type"));
        Assert.Equal("uploads/x.pgn", Value(args, "--upload-key"));
        Assert.DoesNotContain("--lichess-month", args);
    }

    [Fact]
    public void IngestionEncodesEveryFilterDimension()
    {
        IReadOnlyList<string> args = SparkArguments.ForIngestion(
            "cid",
            IngestionSourceSpec.Lichess("2024-12"),
            new CorpusFilterSpec("1600-1999", "blitz", 1704067200000, 1704067200000, 0.15),
            Options);

        Assert.Equal("1600-1999", Value(args, "--rating-band"));
        Assert.Equal("blitz", Value(args, "--time-control"));
        Assert.Equal("2024-01-01", Value(args, "--date-from"));
        Assert.Equal("2024-01-01", Value(args, "--date-to"));
        Assert.Equal("0.15", Value(args, "--sample-rate"));
    }

    [Fact]
    public void IngestionOmitsFullAndZeroSampleRateAndZeroDates()
    {
        IReadOnlyList<string> full = SparkArguments.ForIngestion(
            "cid", IngestionSourceSpec.Lichess("2024-12"), new CorpusFilterSpec("", "", 0, 0, 1.0), Options);
        Assert.DoesNotContain("--sample-rate", full);
        Assert.DoesNotContain("--date-from", full);
        Assert.DoesNotContain("--date-to", full);

        IReadOnlyList<string> zero = SparkArguments.ForIngestion(
            "cid", IngestionSourceSpec.Lichess("2024-12"), new CorpusFilterSpec("", "", 0, 0, 0), Options);
        Assert.DoesNotContain("--sample-rate", zero);
    }

    [Fact]
    public void IngestionReplayFlagFollowsOptions()
    {
        InsightsOptions noReplay = new() { ReplayBoard = false };
        IReadOnlyList<string> args = SparkArguments.ForIngestion(
            "cid", IngestionSourceSpec.Lichess("2024-12"), CorpusFilterSpec.Empty, noReplay);
        Assert.Equal("false", Value(args, "--replay"));
    }

    [Fact]
    public void AnalysisAllKindsUsesAllToken()
    {
        IReadOnlyList<string> args = SparkArguments.ForAnalysis(
            "cid", "job-9", "insights-analysis-job-9", AnalysisKindNames.All, Options);

        Assert.Equal("all", Value(args, "--jobs"));
        Assert.Equal("cid", Value(args, "--corpus-id"));
        Assert.Equal("mongodb://m", Value(args, "--mongo-uri"));
        Assert.Equal("maichess", Value(args, "--mongo-db"));
        Assert.Equal("10", Value(args, "--book-plies"));
        Assert.Equal("50", Value(args, "--min-reach"));
        Assert.Equal("30", Value(args, "--min-support"));
        Assert.Equal("job-9", Value(args, "--job-id"));
        Assert.Equal("insights-analysis-job-9", Value(args, "--spark-application"));
    }

    [Fact]
    public void AnalysisSubsetJoinsKindNames()
    {
        IReadOnlyList<string> args = SparkArguments.ForAnalysis(
            "cid", "job-9", "app", [AnalysisKind.Openings, AnalysisKind.Tricky], Options);
        Assert.Equal("openings,tricky", Value(args, "--jobs"));
    }

    private static string? Value(IReadOnlyList<string> args, string flag)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
