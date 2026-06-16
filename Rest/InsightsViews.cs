using System.Diagnostics.CodeAnalysis;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Rest;

// Maps domain records to the REST view shapes (rest/insights.md). Excluded from
// coverage: part of the thin HTTP adapter.
[ExcludeFromCodeCoverage]
internal static class InsightsViews
{
    internal static JobView ToView(JobRecord job) => new(
        job.Id,
        JobTypeNames.ToName(job.Type),
        job.CorpusId,
        job.Source is null ? null : ToView(job.Source),
        ToView(job.Filter),
        JobStatusNames.ToName(job.Status),
        [.. job.AnalysisKinds.Select(AnalysisKindNames.ToName)],
        job.CreatedAtMs,
        job.StartedAtMs,
        job.FinishedAtMs,
        job.SparkApplication,
        job.Error);

    internal static CorpusView ToView(CorpusRecord corpus) => new(
        corpus.Id,
        ToView(corpus.Source),
        ToView(corpus.Filter),
        corpus.GameCount,
        corpus.CreatedAtMs);

    internal static SourceView ToView(IngestionSourceSpec source) =>
        source.Kind == SourceKind.Lichess
            ? new SourceView(new LichessMonthView(source.LichessYearMonth), null)
            : new SourceView(null, new UploadSourceView(source.UploadObjectKey, source.UploadLabel));

    internal static FilterView ToView(CorpusFilterSpec filter) => new(
        filter.RatingBand, filter.TimeControl, filter.DateFromMs, filter.DateToMs, filter.SampleRate);
}
