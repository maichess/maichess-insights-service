using System.Diagnostics.CodeAnalysis;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record EndgamesResponse(IReadOnlyList<EndgameMetric> Endgames);
