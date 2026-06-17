using System.Globalization;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Services;

// Builds the `--key value` argument list passed to the Spark job mains (IngestJob /
// AnalysisJob). Pure and unit-tested; the SparkJobLauncher just hands these to the
// SparkApplication spec. Mirrors the arg contract in the Scala module
// (ingest/JobArgs.scala, analysis/AnalysisArgs.scala).
internal static class SparkArguments
{
    internal static IReadOnlyList<string> ForIngestion(
        string corpusId, IngestionSourceSpec source, CorpusFilterSpec filter, InsightsOptions options)
    {
        List<string> args = ["--corpus-id", corpusId];

        if (source.Kind == SourceKind.Lichess)
        {
            args.AddRange(["--source-type", "lichess", "--lichess-month", source.LichessYearMonth]);
        }
        else
        {
            args.AddRange(["--source-type", "upload", "--upload-key", source.UploadObjectKey]);
        }

        if (!string.IsNullOrWhiteSpace(filter.RatingBand))
        {
            args.AddRange(["--rating-band", filter.RatingBand]);
        }

        if (!string.IsNullOrWhiteSpace(filter.TimeControl))
        {
            args.AddRange(["--time-control", filter.TimeControl]);
        }

        if (filter.DateFromMs > 0)
        {
            args.AddRange(["--date-from", ToDate(filter.DateFromMs)]);
        }

        if (filter.DateToMs > 0)
        {
            args.AddRange(["--date-to", ToDate(filter.DateToMs)]);
        }

        if (filter.SampleRate > 0 && filter.SampleRate < 1)
        {
            args.AddRange(["--sample-rate", filter.SampleRate.ToString(CultureInfo.InvariantCulture)]);
        }

        args.AddRange(["--replay", options.ReplayBoard ? "true" : "false"]);
        args.AddRange(["--raw-bucket", options.RawBucket]);
        args.AddRange(["--parsed-bucket", options.ParsedBucket]);

        // The job writes the parsed game count back to the catalog (insights_corpora,
        // created with GameCount 0 on submit), so it needs the catalog Mongo connection.
        args.AddRange(["--mongo-uri", options.MongoUri]);
        args.AddRange(["--mongo-db", options.MongoDb]);
        return args;
    }

    internal static IReadOnlyList<string> ForAnalysis(
        string corpusId,
        string jobId,
        string sparkApplication,
        IReadOnlyList<AnalysisKind> kinds,
        InsightsOptions options)
    {
        string jobs = kinds.Count == AnalysisKindNames.All.Count
            ? "all"
            : string.Join(',', kinds.Select(AnalysisKindNames.ToName));

        return
        [
            "--corpus-id", corpusId,
            "--jobs", jobs,
            "--parsed-bucket", options.ParsedBucket,
            "--agg-bucket", options.AggBucket,
            "--mongo-uri", options.MongoUri,
            "--mongo-db", options.MongoDb,
            "--book-plies", options.BookPlies.ToString(CultureInfo.InvariantCulture),
            "--min-reach", options.MinReach.ToString(CultureInfo.InvariantCulture),
            "--min-support", options.MinSupport.ToString(CultureInfo.InvariantCulture),
            "--job-id", jobId,
            "--spark-application", sparkApplication,
        ];
    }

    // The Scala filter compares game UTCDate lexicographically as "YYYY-MM-DD", so the
    // epoch-ms slice bounds are rendered to that form (UTC).
    private static string ToDate(long epochMs) =>
        DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
