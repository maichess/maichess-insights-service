using System.Diagnostics.CodeAnalysis;
using MaichessInsightsService.Domain;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record OpeningsResponse(IReadOnlyList<OpeningMetric> Openings);
