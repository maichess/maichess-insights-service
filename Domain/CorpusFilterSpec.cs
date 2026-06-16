namespace MaichessInsightsService.Domain;

// The slice an ingestion keeps. Empty fields mean "all"; recorded on the corpus so a
// slice is always reproducible. SampleRate is in (0,1]; 0 (or 1) means the full corpus.
internal sealed record CorpusFilterSpec(
    string RatingBand,
    string TimeControl,
    long DateFromMs,
    long DateToMs,
    double SampleRate)
{
    internal static CorpusFilterSpec Empty { get; } = new(string.Empty, string.Empty, 0, 0, 0);
}
</content>
