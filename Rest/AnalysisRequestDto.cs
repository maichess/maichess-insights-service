using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record AnalysisRequestDto(string? CorpusId, IReadOnlyList<string>? Kinds);
