namespace MaichessInsightsService.Domain;

// Which analysis job an analysis run computes; each maps to one insights_* collection.
internal enum AnalysisKind
{
    Openings,
    Endgames,
    Positions,
    Tricky,
    Summary,
}
</content>
