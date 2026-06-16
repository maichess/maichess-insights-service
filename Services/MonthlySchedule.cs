using System.Globalization;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Pure pieces of the monthly Lichess pull. Lichess publishes a month's standard dump
// only after the month ends, so the latest complete month is the previous calendar
// month. The default ingestion applies the configured filter/sample (full-month runs
// are explicit opt-in). Kept pure so the month math + default filter are unit-tested;
// the off-peak timing lives in the excluded MonthlyIngestionScheduler.
internal static class MonthlySchedule
{
    internal static string LatestAvailableMonth(DateTimeOffset now) =>
        now.UtcDateTime.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture);

    internal static IngestionInput BuildIngestion(DateTimeOffset now, InsightsOptions options) =>
        new(
            new LichessMonthInput(LatestAvailableMonth(now)),
            null,
            new CorpusFilterSpec(
                options.DefaultRatingBand,
                options.DefaultTimeControl,
                0,
                0,
                options.DefaultSampleRate));
}
</content>
