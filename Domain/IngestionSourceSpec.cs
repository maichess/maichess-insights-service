namespace MaichessInsightsService.Domain;

// The validated, persisted source of a corpus. Exactly one arm is meaningful per the
// Kind: a Lichess month, or an uploaded PGN object already staged in MinIO.
internal sealed record IngestionSourceSpec(
    SourceKind Kind,
    string LichessYearMonth,
    string UploadObjectKey,
    string UploadLabel)
{
    internal static IngestionSourceSpec Lichess(string yearMonth) =>
        new(SourceKind.Lichess, yearMonth, string.Empty, string.Empty);

    internal static IngestionSourceSpec Upload(string objectKey, string label) =>
        new(SourceKind.Upload, string.Empty, objectKey, label);
}
