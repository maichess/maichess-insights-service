namespace MaichessInsightsService.Services;

// Redis L1 over the hot insights aggregates. A plain string get/set keyed by
// corpus id + query params; the query service stores the JSON-serialized full
// sorted metric list. No expiry (allkeys-lru), rebuildable from Mongo on a miss.
internal interface IInsightsCache
{
    // Returns the cached payload for a key, or null on a miss (key absent).
    Task<string?> GetAsync(string key, CancellationToken ct);

    // Populates a key with a payload (overwrites). No expiry.
    Task SetAsync(string key, string payload, CancellationToken ct);
}
