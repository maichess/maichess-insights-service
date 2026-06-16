using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record FilterView(
    string RatingBand, string TimeControl, long DateFromMs, long DateToMs, double SampleRate);
