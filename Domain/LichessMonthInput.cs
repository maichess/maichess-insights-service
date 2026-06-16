namespace MaichessInsightsService.Domain;

// The Lichess source for an ingestion: a standard-games month ("YYYY-MM").
internal sealed record LichessMonthInput(string YearMonth);
