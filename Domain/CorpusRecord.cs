namespace MaichessInsightsService.Domain;

// One analyzed dataset: a source + filter producing a set of games that every metric
// is computed over. The control plane creates it on ingestion submit (GameCount 0);
// the Spark ingestion job updates GameCount once the corpus is parsed.
internal sealed class CorpusRecord
{
    public string Id { get; set; } = string.Empty;

    public IngestionSourceSpec Source { get; set; } = IngestionSourceSpec.Lichess(string.Empty);

    public CorpusFilterSpec Filter { get; set; } = CorpusFilterSpec.Empty;

    public long GameCount { get; set; }

    public long CreatedAtMs { get; set; }
}
</content>
