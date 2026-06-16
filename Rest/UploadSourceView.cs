using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record UploadSourceView(string ObjectKey, string Label);
