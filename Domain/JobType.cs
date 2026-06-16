namespace MaichessInsightsService.Domain;

// The two pipeline stages a Job can record.
internal enum JobType
{
    Ingestion,
    Analysis,
}
