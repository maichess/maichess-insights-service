# maichess-insights-service

.NET control plane for the [insights & Spark analytics](../../maichess-knowledge-base/knowledge/architecture/insights-and-spark.md)
program. Submits and tracks Apache Spark ingestion/analysis jobs as `SparkApplication`
custom resources, accepts manual PGN uploads, and serves the job/corpus catalog over
REST + gRPC. The Scala [`maichess-insights-spark`](../maichess-insights-spark) module does
the data processing.

## Run locally

```bash
dotnet run                      # needs Services:InsightsDatabase + Jwt:Key configured
```

Configuration lives in `appsettings.json` (`Insights` section for the Spark/MinIO
parameters, `Services:InsightsDatabase` for the catalog DB). A local kubeconfig is used
off-cluster; in-cluster it uses the `insights-service` ServiceAccount.

## Test

```bash
dotnet test MaichessInsightsService.Tests/MaichessInsightsService.Tests.csproj \
  -p:CollectCoverage=true "-p:Include=[MaichessInsightsService]*"
```

100% line/branch/method coverage on non-excluded code is required (see `CLAUDE.md` for the
exclusions). Reqnroll feature files live under `MaichessInsightsService.Tests/Features`.

## Mutation testing

```bash
dotnet tool restore
cd MaichessInsightsService.Tests && dotnet stryker
```

## REST surface

`POST /insights/ingestions`, `POST /insights/uploads`, `POST /insights/analyses`,
`GET /insights/jobs`, `GET /insights/jobs/{id}`, `GET /insights/corpora`. The metric query
endpoints (`/corpora/{id}/openings` etc.) arrive in task 06. See
`maichess-api-contracts/rest/insights.md`.
