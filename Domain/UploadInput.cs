namespace MaichessInsightsService.Domain;

// The manual-upload source for an ingestion: a staged PGN object in insights-raw.
internal sealed record UploadInput(string ObjectKey, string Label);
