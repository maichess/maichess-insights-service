namespace MaichessInsightsService.Domain;

// The SparkApplication CR name backing a job. Tied to the job id so the status
// watcher can map a SparkApplication back to its job record. RFC1123: lowercase
// alphanumeric + '-', <= 63 chars, no trailing '-'.
internal static class SparkApplicationName
{
    private const int MaxLength = 63;

    internal static string For(JobType type, string jobId)
    {
        string kind = type == JobType.Ingestion ? "ingest" : "analysis";
        string suffix = Slug.Make(jobId);
        string name = $"insights-{kind}-{suffix}";
        return name.Length <= MaxLength ? name : name[..MaxLength].TrimEnd('-');
    }
}
</content>
