namespace MaichessInsightsService.Domain;

// Maps JobType to/from the lowercase string persisted in insights_jobs and used on the
// wire.
internal static class JobTypeNames
{
    internal static string ToName(JobType type) =>
        type == JobType.Ingestion ? "ingestion" : "analysis";

    internal static JobType FromName(string name) =>
        name == "analysis" ? JobType.Analysis : JobType.Ingestion;
}
</content>
