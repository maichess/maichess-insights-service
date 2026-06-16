using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MaichessInsightsService.Services;
using Minio;
using Minio.DataModel.Args;

namespace MaichessInsightsService.Data;

// Stages uploaded PGNs into the insights-raw MinIO bucket via the Minio SDK. Excluded
// from coverage: live object storage. The object key it returns is what the user passes
// as the `upload` source of POST /insights/ingestions.
[ExcludeFromCodeCoverage]
internal sealed class MinioObjectStore(IMinioClient client, string bucket, Func<long> clock, Func<string> idGen)
    : IObjectStore
{
    public async Task<string> StorePgnAsync(Stream content, string fileName, CancellationToken ct)
    {
        // Buffer so the object size is known (uploads arrive without a content length).
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        string day = DateTimeOffset.FromUnixTimeMilliseconds(clock()).UtcDateTime
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string extension = fileName.EndsWith(".pgn.zst", StringComparison.OrdinalIgnoreCase) ? ".pgn.zst" : ".pgn";
        string key = $"uploads/{day}/{idGen()}{extension}";

        await client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(key)
                .WithStreamData(buffer)
                .WithObjectSize(buffer.Length)
                .WithContentType("application/x-chess-pgn"),
            ct);
        return key;
    }
}
