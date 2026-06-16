namespace MaichessInsightsService.Services;

// MinIO seam for staging uploaded PGNs into the insights-raw bucket. The real
// implementation is the Minio SDK; behind an interface so the upload flow is testable.
internal interface IObjectStore
{
    // Stores the PGN bytes under a generated object key in insights-raw and returns the
    // key (to pass as the `upload` source of an ingestion).
    Task<string> StorePgnAsync(Stream content, string fileName, CancellationToken ct);
}
</content>
