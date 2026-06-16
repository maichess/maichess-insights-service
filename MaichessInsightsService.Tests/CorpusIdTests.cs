using MaichessInsightsService.Domain;
using Xunit;

namespace MaichessInsightsService.Tests;

public class CorpusIdTests
{
    [Fact]
    public void LichessWithNoFilterIsJustTheMonth() =>
        Assert.Equal("lichess-2024-12", CorpusId.ForLichess("2024-12", CorpusFilterSpec.Empty));

    [Fact]
    public void LichessEncodesEveryFilterDimension() =>
        Assert.Equal(
            "lichess-2024-12-blitz-1600-1999-s15",
            CorpusId.ForLichess("2024-12", new CorpusFilterSpec("1600-1999", "blitz", 0, 0, 0.15)));

    [Fact]
    public void LichessOmitsFullAndZeroSampleRate()
    {
        Assert.Equal("lichess-2024-12", CorpusId.ForLichess("2024-12", new CorpusFilterSpec("", "", 0, 0, 1.0)));
        Assert.Equal("lichess-2024-12", CorpusId.ForLichess("2024-12", new CorpusFilterSpec("", "", 0, 0, 0)));
    }

    [Fact]
    public void LichessRoundsSamplePercentAwayFromZero() =>
        Assert.Equal("lichess-2024-12-s16", CorpusId.ForLichess("2024-12", new CorpusFilterSpec("", "", 0, 0, 0.155)));

    [Fact]
    public void LichessTimeControlOnly() =>
        Assert.Equal("lichess-2024-12-blitz", CorpusId.ForLichess("2024-12", new CorpusFilterSpec("", "blitz", 0, 0, 0)));

    [Fact]
    public void UploadIsPrefixedAndSlugged() =>
        Assert.Equal("upload-abc-123", CorpusId.ForUpload("ABC-123"));
}
</content>
