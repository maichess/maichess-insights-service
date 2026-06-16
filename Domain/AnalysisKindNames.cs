namespace MaichessInsightsService.Domain;

// Maps AnalysisKind to/from the wire name used in REST, gRPC, and the Spark --jobs arg.
internal static class AnalysisKindNames
{
    internal static IReadOnlyList<AnalysisKind> All { get; } =
        [AnalysisKind.Openings, AnalysisKind.Endgames, AnalysisKind.Positions, AnalysisKind.Tricky, AnalysisKind.Summary];

    internal static string ToName(AnalysisKind kind) =>
        kind switch
        {
            AnalysisKind.Openings => "openings",
            AnalysisKind.Endgames => "endgames",
            AnalysisKind.Positions => "positions",
            AnalysisKind.Tricky => "tricky",
            _ => "summary",
        };

    internal static bool TryParse(string raw, out AnalysisKind kind)
    {
        switch (raw.Trim().ToLowerInvariant())
        {
            case "openings": kind = AnalysisKind.Openings; return true;
            case "endgames": kind = AnalysisKind.Endgames; return true;
            case "positions": kind = AnalysisKind.Positions; return true;
            case "tricky": kind = AnalysisKind.Tricky; return true;
            case "summary": kind = AnalysisKind.Summary; return true;
            default: kind = default; return false;
        }
    }
}
</content>
