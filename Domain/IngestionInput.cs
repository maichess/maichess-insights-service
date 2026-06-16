namespace MaichessInsightsService.Domain;

// A request to ingest a source into a new corpus. Exactly one of Lichess / Upload must
// be present; Filter narrows which games are kept (Empty ingests the full source).
internal sealed record IngestionInput(
    LichessMonthInput? Lichess,
    UploadInput? Upload,
    CorpusFilterSpec Filter);
