using System.Diagnostics.CodeAnalysis;

namespace MaichessInsightsService.Services;

// Control-plane configuration (bound from the "Insights" section). The Spark-job
// parameters here feed both the argument builders (tested) and the SparkJobLauncher's
// SparkApplication spec (infra glue). Excluded from coverage: a pure configuration POCO
// (no behaviour), several fields read only by the excluded launcher.
[ExcludeFromCodeCoverage]
internal sealed class InsightsOptions
{
    // Namespace the SparkApplication CRs are created in (release namespace).
    public string Namespace { get; set; } = "maichess";

    // Custom Spark image carrying the Scala assembly (knowledge/operations/spark-and-minio.md).
    public string Image { get; set; } = "ghcr.io/maichess/maichess-insights-spark:main";

    public string SparkVersion { get; set; } = "3.5.3";

    public string IngestionMainClass { get; set; } = "maichess.insights.ingest.IngestJob";

    public string AnalysisMainClass { get; set; } = "maichess.insights.analysis.AnalysisJob";

    // Path to the assembly jar inside the image.
    public string JarPath { get; set; } = "local:///opt/spark/jars/maichess-insights-spark.jar";

    // Operator-managed driver ServiceAccount (sparkOperator.spark.serviceAccount).
    public string ServiceAccount { get; set; } = "spark";

    // Mongo connection the Spark analysis connector writes through; the control plane
    // reads the same database via database-service gRPC, so MongoDb must be "maichess"
    // (the database every DatabaseService Mongo instance shares).
    public string MongoUri { get; set; } = string.Empty;

    public string MongoDb { get; set; } = "maichess";

    public string RawBucket { get; set; } = "insights-raw";

    public string ParsedBucket { get; set; } = "insights-parsed";

    public string AggBucket { get; set; } = "insights-agg";

    // Whether ingestion replays the board (needed for endgame/position FENs).
    public bool ReplayBoard { get; set; } = true;

    // Analysis tuning (book-ply cutoff + min support thresholds).
    public int BookPlies { get; set; } = 10;

    public int MinReach { get; set; } = 50;

    public int MinSupport { get; set; } = 30;

    // Spark blast-radius controls (knowledge/operations/spark-and-minio.md).
    public string PriorityClassName { get; set; } = "insights-low-priority";

    // Hostname the driver/executors pin to (maichess.nodeSelectorCompute, task 02).
    public string ComputeNodeHostname { get; set; } = string.Empty;

    public int DriverCores { get; set; } = 1;

    public string DriverMemory { get; set; } = "2g";

    public int ExecutorInstances { get; set; } = 2;

    public int ExecutorCores { get; set; } = 4;

    public string ExecutorMemory { get; set; } = "6g";
}
</content>
