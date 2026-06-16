using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;
using Xunit;

namespace MaichessInsightsService.Tests;

public class MonthlyScheduleTests
{
    [Theory]
    [InlineData("2026-06-16T09:00:00Z", "2026-05")]
    [InlineData("2026-01-01T00:00:00Z", "2025-12")]
    [InlineData("2026-03-31T23:59:59Z", "2026-02")]
    public void LatestAvailableMonthIsThePreviousCalendarMonth(string nowIso, string expected) =>
        Assert.Equal(expected, MonthlySchedule.LatestAvailableMonth(DateTimeOffset.Parse(nowIso)));

    [Fact]
    public void BuildIngestionTargetsLatestMonthWithDefaultFilter()
    {
        InsightsOptions options = new()
        {
            DefaultRatingBand = "1600-1999",
            DefaultTimeControl = "blitz",
            DefaultSampleRate = 0.15,
        };

        IngestionInput input = MonthlySchedule.BuildIngestion(
            DateTimeOffset.Parse("2026-06-16T09:00:00Z"), options);

        Assert.Equal("2026-05", input.Lichess!.YearMonth);
        Assert.Null(input.Upload);
        Assert.Equal("1600-1999", input.Filter.RatingBand);
        Assert.Equal("blitz", input.Filter.TimeControl);
        Assert.Equal(0, input.Filter.DateFromMs);
        Assert.Equal(0, input.Filter.DateToMs);
        Assert.Equal(0.15, input.Filter.SampleRate);
    }

    [Fact]
    public void BuildIngestionUsesUnfilteredSampledDefaults()
    {
        IngestionInput input = MonthlySchedule.BuildIngestion(
            DateTimeOffset.Parse("2026-06-16T09:00:00Z"), new InsightsOptions());

        Assert.Equal(string.Empty, input.Filter.RatingBand);
        Assert.Equal(string.Empty, input.Filter.TimeControl);
        Assert.Equal(0.1, input.Filter.SampleRate);
    }
}
