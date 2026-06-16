using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// In-memory IInsightsCache backed by a dictionary; tracks get/set counts and exposes
// the stored payloads so tests can assert miss→populate and hit paths.
internal sealed class FakeInsightsCache : IInsightsCache
{
    private readonly Dictionary<string, string> entries = [];

    public int Gets { get; private set; }

    public int Sets { get; private set; }

    public IReadOnlyDictionary<string, string> Entries => entries;

    public Task<string?> GetAsync(string key, CancellationToken ct)
    {
        Gets++;
        return Task.FromResult(entries.GetValueOrDefault(key));
    }

    public Task SetAsync(string key, string payload, CancellationToken ct)
    {
        Sets++;
        entries[key] = payload;
        return Task.CompletedTask;
    }

    public void Seed(string key, string payload) => entries[key] = payload;
}
