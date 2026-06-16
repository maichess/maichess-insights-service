using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record JobListResponse(IReadOnlyList<JobView> Jobs);
