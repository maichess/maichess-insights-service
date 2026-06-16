using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record JobView(
    string Id,
    string Type,
    string CorpusId,
    SourceView? Source,
    FilterView Filter,
    string Status,
    IReadOnlyList<string> AnalysisKinds,
    long CreatedAtMs,
    long StartedAtMs,
    long FinishedAtMs,
    string SparkApplication,
    string Error);
