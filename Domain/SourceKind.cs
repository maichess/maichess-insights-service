namespace MaichessInsightsService.Domain;

// Where a corpus's games come from. Pluggable: only Lichess + manual upload ship
// first; future sources add a new value.
internal enum SourceKind
{
    Lichess,
    Upload,
}
