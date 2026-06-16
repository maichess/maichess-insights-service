using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record UploadSourceDto(string? ObjectKey, string? Label);
