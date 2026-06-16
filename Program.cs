using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.SchemaRegistry;
using Grpc.Net.Client;
using k8s;
using Maichess.Database.V1;
using MaichessInsightsService.Data;
using MaichessInsightsService.Grpc;
using MaichessInsightsService.Kafka;
using MaichessInsightsService.Rest;
using MaichessInsightsService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Minio;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

DotNetEnv.Env.Load();
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string insightsDbUrl = builder.Configuration["Services:InsightsDatabase"]
    ?? throw new InvalidOperationException("Services:InsightsDatabase is not configured");
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");

InsightsOptions insightsOptions = builder.Configuration.GetSection("Insights").Get<InsightsOptions>() ?? new InsightsOptions();
builder.Services.AddSingleton(insightsOptions);

Func<long> clock = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
Func<string> idGen = () => Guid.NewGuid().ToString();
builder.Services.AddSingleton(clock);
builder.Services.AddSingleton(idGen);

builder.Services.AddSingleton(
    new Database.DatabaseClient(GrpcChannel.ForAddress(insightsDbUrl)));
builder.Services.AddSingleton<IInsightsStore>(sp =>
    new InsightsStore(sp.GetRequiredService<Database.DatabaseClient>()));

// The C# Kubernetes client uses the in-cluster ServiceAccount (insights-service, task
// 02) when running in the cluster, else the local kubeconfig for dev.
KubernetesClientConfiguration k8sConfig = KubernetesClientConfiguration.IsInCluster()
    ? KubernetesClientConfiguration.InClusterConfig()
    : KubernetesClientConfiguration.BuildConfigFromConfigFile();
builder.Services.AddSingleton<IKubernetes>(new Kubernetes(k8sConfig));
builder.Services.AddSingleton<ISparkJobLauncher, SparkJobLauncher>();

// MinIO staging for uploaded PGNs (insights-raw bucket).
string minioEndpoint = builder.Configuration["Insights:Minio:Endpoint"] ?? "minio:9000";
string minioAccessKey = builder.Configuration["Insights:Minio:AccessKey"] ?? string.Empty;
string minioSecretKey = builder.Configuration["Insights:Minio:SecretKey"] ?? string.Empty;
bool minioSsl = builder.Configuration.GetValue("Insights:Minio:UseSsl", false);
IMinioClient minio = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(minioSsl)
    .Build();
builder.Services.AddSingleton<IObjectStore>(new MinioObjectStore(minio, insightsOptions.RawBucket, clock, idGen));

// Job-lifecycle events: pushed live when Kafka is enabled, else durably tracked only.
if (builder.Configuration.GetValue("Kafka:Enabled", false))
{
    string bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "kafka:9092";
    string registryUrl = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL") ?? "http://schema-registry:8081";
    builder.Services.AddSingleton<ISchemaRegistryClient>(
        _ => new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = registryUrl }));
    builder.Services.AddSingleton<IInsightsJobEventProducer>(sp =>
        new InsightsJobEventProducer(bootstrap, sp.GetRequiredService<ISchemaRegistryClient>(), clock, idGen));
}
else
{
    builder.Services.AddSingleton<IInsightsJobEventProducer>(new NoopInsightsJobEventProducer());
}

builder.Services.AddSingleton(sp => new JobService(
    sp.GetRequiredService<IInsightsStore>(),
    sp.GetRequiredService<ISparkJobLauncher>(),
    sp.GetRequiredService<IInsightsJobEventProducer>(),
    sp.GetRequiredService<InsightsOptions>(),
    idGen,
    clock));

// Tracks SparkApplication status transitions back into insights_jobs.
builder.Services.AddHostedService(sp => new SparkStatusReconciler(
    sp.GetRequiredService<IKubernetes>(),
    sp.GetRequiredService<IInsightsStore>(),
    sp.GetRequiredService<IInsightsJobEventProducer>(),
    clock,
    sp.GetRequiredService<ILogger<SparkStatusReconciler>>()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out string? token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(jsonOptions =>
{
    jsonOptions.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    jsonOptions.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

string otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://otel-collector:4317";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("insights-service"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok());
app.MapGrpcService<InsightsGrpcService>();
app.MapInsightsEndpoints();

await app.RunAsync();
</content>
