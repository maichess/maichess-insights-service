using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record CorpusView(
    string Id, SourceView Source, FilterView Filter, long GameCount, long CreatedAtMs);
