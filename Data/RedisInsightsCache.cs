using System.Diagnostics.CodeAnalysis;
using MaichessInsightsService.Services;
using StackExchange.Redis;

namespace MaichessInsightsService.Data;

// StackExchange.Redis implementation of the insights L1. Each query result is a single
// string value keyed by corpus id + query params; no expiry (allkeys-lru only) and
// rebuildable from Mongo on a miss. Excluded from coverage like the repositories: it
// requires a live Redis; the miss→read→populate logic lives in the tested
// InsightsQueryService against a fake IInsightsCache.
[ExcludeFromCodeCoverage]
internal sealed class RedisInsightsCache(IConnectionMultiplexer redis) : IInsightsCache
{
    private IDatabase Db => redis.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken ct)
    {
        RedisValue value = await Db.StringGetAsync(key);
        return value.IsNull ? null : value.ToString();
    }

    public async Task SetAsync(string key, string payload, CancellationToken ct) =>
        await Db.StringSetAsync(key, payload);
}
