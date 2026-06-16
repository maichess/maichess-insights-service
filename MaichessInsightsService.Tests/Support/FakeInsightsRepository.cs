using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Tests.Support;

// In-memory IInsightsRepository: returns the seeded metric rows and counts reads so a
// cache hit can be asserted to skip the repository.
internal sealed class FakeInsightsRepository : IInsightsRepository
{
    public List<OpeningMetric> Openings { get; } = [];

    public List<EndgameMetric> Endgames { get; } = [];

    public List<PositionMetric> Positions { get; } = [];

    public List<TrickyMetric> Tricky { get; } = [];

    public CorpusSummaryMetric? Summary { get; set; }

    public int OpeningsCalls { get; private set; }

    public int EndgamesCalls { get; private set; }

    public int PositionsCalls { get; private set; }

    public int TrickyCalls { get; private set; }

    public int SummaryCalls { get; private set; }

    public Task<IReadOnlyList<OpeningMetric>> GetOpeningsAsync(string corpusId, CancellationToken ct)
    {
        OpeningsCalls++;
        return Task.FromResult<IReadOnlyList<OpeningMetric>>(Openings);
    }

    public Task<IReadOnlyList<EndgameMetric>> GetEndgamesAsync(string corpusId, CancellationToken ct)
    {
        EndgamesCalls++;
        return Task.FromResult<IReadOnlyList<EndgameMetric>>(Endgames);
    }

    public Task<IReadOnlyList<PositionMetric>> GetPositionsAsync(string corpusId, CancellationToken ct)
    {
        PositionsCalls++;
        return Task.FromResult<IReadOnlyList<PositionMetric>>(Positions);
    }

    public Task<IReadOnlyList<TrickyMetric>> GetTrickyAsync(string corpusId, CancellationToken ct)
    {
        TrickyCalls++;
        return Task.FromResult<IReadOnlyList<TrickyMetric>>(Tricky);
    }

    public Task<CorpusSummaryMetric?> GetSummaryAsync(string corpusId, CancellationToken ct)
    {
        SummaryCalls++;
        return Task.FromResult(Summary);
    }
}
