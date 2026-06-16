using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record FilterDto(
    string? RatingBand, string? TimeControl, long DateFromMs, long DateToMs, double SampleRate);
