namespace MaichessInsightsService.Domain;

// A request to run analysis jobs over an already-ingested corpus. Kinds are the raw
// names from the wire ("openings", "tricky", …); empty means all kinds. Parsing /
// validation of the names happens in JobService so it stays unit-tested.
internal sealed record AnalysisInput(string CorpusId, IReadOnlyList<string> Kinds);
