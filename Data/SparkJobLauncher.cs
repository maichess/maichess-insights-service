using System.Diagnostics.CodeAnalysis;
using k8s;
using k8s.Autorest;
using MaichessInsightsService.Domain;
using MaichessInsightsService.Services;

namespace MaichessInsightsService.Data;

// Creates SparkApplication custom resources via the C# Kubernetes client (the
// insights-service ServiceAccount has RBAC for this, task 02). Excluded from coverage:
// requires a live cluster. The decision of *what* to launch lives in the tested
// JobService; this only translates a SparkSpec into the operator's CR shape.
[ExcludeFromCodeCoverage]
internal sealed class SparkJobLauncher(IKubernetes kube, InsightsOptions options, ILogger<SparkJobLauncher> logger)
    : ISparkJobLauncher
{
    internal const string Group = "sparkoperator.k8s.io";
    internal const string Version = "v1beta2";
    internal const string Plural = "sparkapplications";

    public async Task LaunchAsync(SparkSpec spec, CancellationToken ct)
    {
        object body = BuildManifest(spec);
        try
        {
            await kube.CustomObjects.CreateNamespacedCustomObjectAsync(
                body, Group, Version, options.Namespace, Plural, cancellationToken: ct);
            logger.LogInformation("Created SparkApplication {Name} ({Class})", spec.ApplicationName, spec.MainClass);
        }
        catch (HttpOperationException ex)
        {
            logger.LogError(ex, "Failed to create SparkApplication {Name}: {Body}", spec.ApplicationName, ex.Response?.Content);
            throw;
        }
    }

    private object BuildManifest(SparkSpec spec)
    {
        Dictionary<string, object> driver = new()
        {
            ["cores"] = options.DriverCores,
            ["memory"] = options.DriverMemory,
            ["serviceAccount"] = options.ServiceAccount,
            ["labels"] = new Dictionary<string, string> { ["maichess/insights-job"] = spec.ApplicationName },
        };
        Dictionary<string, object> executor = new()
        {
            ["instances"] = options.ExecutorInstances,
            ["cores"] = options.ExecutorCores,
            ["memory"] = options.ExecutorMemory,
            ["labels"] = new Dictionary<string, string> { ["maichess/insights-job"] = spec.ApplicationName },
        };

        if (!string.IsNullOrWhiteSpace(options.PriorityClassName))
        {
            driver["priorityClassName"] = options.PriorityClassName;
            executor["priorityClassName"] = options.PriorityClassName;
        }

        if (!string.IsNullOrWhiteSpace(options.ComputeNodeHostname))
        {
            Dictionary<string, string> pin = new() { ["kubernetes.io/hostname"] = options.ComputeNodeHostname };
            driver["nodeSelector"] = pin;
            executor["nodeSelector"] = pin;
        }

        Dictionary<string, object> sparkSpec = new()
        {
            ["type"] = "Scala",
            ["mode"] = "cluster",
            ["image"] = options.Image,
            ["imagePullPolicy"] = "IfNotPresent",
            ["mainClass"] = spec.MainClass,
            ["mainApplicationFile"] = options.JarPath,
            ["sparkVersion"] = options.SparkVersion,
            ["arguments"] = spec.Arguments,
            ["restartPolicy"] = new Dictionary<string, object> { ["type"] = "Never" },
            ["driver"] = driver,
            ["executor"] = executor,
        };

        return new Dictionary<string, object>
        {
            ["apiVersion"] = $"{Group}/{Version}",
            ["kind"] = "SparkApplication",
            ["metadata"] = new Dictionary<string, object>
            {
                ["name"] = spec.ApplicationName,
                ["namespace"] = options.Namespace,
                ["labels"] = new Dictionary<string, string>
                {
                    ["app.kubernetes.io/managed-by"] = "insights-service",
                    ["maichess/insights-job-type"] = JobTypeNames.ToName(spec.Type),
                },
            },
            ["spec"] = sparkSpec,
        };
    }
}
</content>
