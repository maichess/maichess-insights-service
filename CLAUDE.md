# Insights Service

The .NET **control plane** for the insights program: accepts ingestion/analysis requests
and manual PGN uploads, launches and tracks Apache Spark jobs as `SparkApplication`
custom resources, and surfaces the job/corpus catalog over REST + gRPC. It never touches
Spark/Parquet/MinIO data itself — the Scala `maichess-insights-spark` module does the
heavy lifting and writes the materialized `insights_*` collections; this service reads the
catalog via database-service gRPC. The metric **query** API (openings/endgames/positions/
tricky/summary, task 06) is implemented: `InsightsQueryService` reads the `insights_*` metric
collections via `IInsightsRepository` (camelCase docs written by Spark) behind a rebuildable
Redis L1 (`IInsightsCache`), served over the corpus-scoped REST routes + the query gRPC RPCs.

## Contracts

- **gRPC:** `maichess-api-contracts/protos/insights-service/v1/insights.proto`
- **REST:** `maichess-api-contracts/rest/insights.md`
- **Events:** produces `insights.events.v1`
  (`protos/events/v1/insights_events.proto`, relayed to `socket.outbound.v1`)
- **Generated stubs:** `Maichess.PlatformProtos` (>= 0.14.0)

Implement against these contracts exactly. Document blockers in `CONTRACT_NOTES.md`.

## Stack

- **Runtime:** ASP.NET (net10.0), C#, nullable enabled (no AOT — Grpc.Net.Client /
  KubernetesClient).
- **Persistence:** `insights-db` DatabaseService instance via `Database.DatabaseClient`
  gRPC (`Services:InsightsDatabase`) — collections `insights_jobs` + `insights_corpora`
  (the catalog the control plane owns). The Spark module writes the metric collections to
  the same Mongo database (`maichess`); the control plane points it there with
  `--mongo-db maichess`.
- **Spark:** SparkApplication CRDs created via the C# **KubernetesClient** under the
  `insights-service` ServiceAccount (RBAC from task 02).
- **MinIO:** the **Minio** SDK stages uploaded PGNs into `insights-raw`.
- **Kafka:** Confluent client + Protobuf serde, gated by `Kafka:Enabled` (job-lifecycle
  events only).

## Structure

```
Domain/    # Pure decision pieces: corpus id, Spark app name, slug, name mappings,
           # the SparkApplication-state → JobStatus map, and the domain records
Services/  # JobService (the tested control-plane core), SparkArguments builder,
           # InsightsOptions, the store/launcher/object/event seams, SubmitResult
Data/      # Excluded glue: InsightsStore (db gRPC), SparkJobLauncher (k8s),
           # SparkStatusReconciler (status watch), MinioObjectStore
Grpc/      # InsightsGrpcService (domain <-> proto adapter; query RPCs are task 06)
Rest/      # InsightsEndpoints + DTO/view records (thin HTTP adapter)
Kafka/     # InsightsJobEventProducer / Noop / serde shells
Program.cs # DI wiring
```

## Key Design Decisions

- **Control plane owns the catalog.** JobService creates the corpus (on ingestion submit)
  and the job record (PENDING), builds the `SparkApplication` spec, launches it, then the
  `SparkStatusReconciler` writes status transitions back. The corpus id is the record id
  (`lichess-2024-12-blitz-1600-1999-s15` / `upload-{id}`) so a slice is reproducible and
  re-ingestion reuses it.
- **Decisions are pure, side effects are seams.** Validation, corpus-id building, job
  choice, and the Spark argument list are computed in `JobService` / `SparkArguments` /
  `Domain` with no k8s/db/MinIO dependency, so they are 100% unit-tested with fakes.
- **Spark arg contract mirrors the Scala mains** (`ingest/JobArgs.scala`,
  `analysis/AnalysisArgs.scala`): `--corpus-id`, `--source-type`, filter flags, `--replay`,
  `--mongo-uri/--mongo-db maichess`, `--job-id`, `--spark-application`. Epoch-ms date
  bounds are rendered to `YYYY-MM-DD` (the Scala filter compares dates lexicographically).
- **insights_jobs has two writers.** The control plane is authoritative (snake_case,
  id = jobId, status via the k8s watch). The shipped task-04 AnalysisJob *also* appends a
  camelCase completion doc — a known integration gap recorded in `CONTRACT_NOTES.md` to
  reconcile (the Scala side should update the control-plane record by jobId, not append).

## Code Style

Same strictness as the other new services: `TreatWarningsAsErrors`, `AnalysisMode=All`,
StyleCop, one type per file, explicit types except where apparent, no comments unless
explaining a non-obvious constraint.

## Testing Requirements

- 100% line/branch/method coverage on non-excluded code. Run
  `dotnet test MaichessInsightsService.Tests/MaichessInsightsService.Tests.csproj
  -p:CollectCoverage=true "-p:Include=[MaichessInsightsService]*"`.
- Reqnroll for the headline flows (job dispatch, upload routing, status mapping) plus
  xUnit facts for the pure helpers and JobService branches; deterministic fakes in
  `Tests/Support`.
- Excluded from coverage (`[ExcludeFromCodeCoverage]`):
  - `Rest/*` (thin HTTP adapter + DTO/view records)
  - `Grpc/InsightsGrpcService` (proto adapter)
  - `Data/*` (live DatabaseService / Kubernetes / MinIO glue)
  - `Kafka/*` (live producer/serde shells)
  - `Services/InsightsOptions` (configuration POCO)
  - `Program.cs`, `*.g.cs`, `*.generated.cs` (coverlet `ExcludeByFile`)

### Mutation testing

Stryker.NET is wired as a local dotnet tool (`.config/dotnet-tools.json`); config in
`MaichessInsightsService.Tests/stryker-config.json` mirrors the coverage exclusions. Run
`dotnet tool restore` then `dotnet stryker` from the test project directory.
