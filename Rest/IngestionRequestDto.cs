using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record IngestionRequestDto(LichessMonthDto? LichessMonth, UploadSourceDto? Upload, FilterDto? Filter);
