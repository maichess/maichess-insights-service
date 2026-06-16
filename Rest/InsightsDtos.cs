using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

// REST request/response shapes, mirroring rest/insights.md. Serialized snake_case by the
// global JSON options. Excluded from coverage: DTO records used only by the thin handler.
[ExcludeFromCodeCoverage]
internal sealed record IngestionRequestDto(LichessMonthDto? LichessMonth, UploadSourceDto? Upload, FilterDto? Filter);

[ExcludeFromCodeCoverage]
internal sealed record LichessMonthDto(string? YearMonth);

[ExcludeFromCodeCoverage]
internal sealed record UploadSourceDto(string? ObjectKey, string? Label);

[ExcludeFromCodeCoverage]
internal sealed record FilterDto(
    string? RatingBand, string? TimeControl, long DateFromMs, long DateToMs, double SampleRate);

[ExcludeFromCodeCoverage]
internal sealed record AnalysisRequestDto(string? CorpusId, IReadOnlyList<string>? Kinds);

[ExcludeFromCodeCoverage]
internal sealed record UploadResponseDto(string ObjectKey, string Label);

[ExcludeFromCodeCoverage]
internal sealed record SourceView(LichessMonthView? LichessMonth, UploadSourceView? Upload);

[ExcludeFromCodeCoverage]
internal sealed record LichessMonthView(string YearMonth);

[ExcludeFromCodeCoverage]
internal sealed record UploadSourceView(string ObjectKey, string Label);

[ExcludeFromCodeCoverage]
internal sealed record FilterView(
    string RatingBand, string TimeControl, long DateFromMs, long DateToMs, double SampleRate);

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

[ExcludeFromCodeCoverage]
internal sealed record JobListResponse(IReadOnlyList<JobView> Jobs);

[ExcludeFromCodeCoverage]
internal sealed record CorpusView(
    string Id, SourceView Source, FilterView Filter, long GameCount, long CreatedAtMs);

[ExcludeFromCodeCoverage]
internal sealed record CorpusListResponse(IReadOnlyList<CorpusView> Corpora);

[ExcludeFromCodeCoverage]
internal sealed record ErrorResponse(string Error);
</content>
