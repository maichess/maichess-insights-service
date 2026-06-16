namespace MaichessInsightsService.Domain;

// A GetTopOpenings request: corpus + optional split filters + paging. Empty split
// fields return the un-split aggregate rows.
internal sealed record OpeningsQuery(
    string CorpusId,
    string Color,
    string RatingBand,
    string TimeControl,
    int Limit,
    int Offset);
