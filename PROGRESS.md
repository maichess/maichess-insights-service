# Insights program — progress log (for handoff)

> Running log so a later session can pick up. Newest section at the bottom. The task
> program is `maichess-knowledge-base/tasks/planned/insights/` (`01`–`07`).

## State as of this session

- **01–04** shipped earlier (contract `v0.14.0`, deploy infra, Scala ingestion + analysis).
- **05 control plane** — DONE, green. Branch `dev` on `maichess-insights-service`, pushed,
  docker-publish pipeline success. 106 tests, 100% line/branch/method. See `CONTRACT_NOTES.md`.
- **06 query API** — DONE, green. 124 tests, 100% line/branch/method. Pushed to `dev`. See below.
- **07 client page** — not started. Touches `maichess-client` (separate repo), consumes 06.

## Working conventions for this repo (reminder)

- Branch: work on `dev`. Build/test needs GitHub Packages creds:
  `set -a; source ../maichess-engine-service/.env; set +a; export GITHUB_TOKEN GITHUB_ACTOR GITHUB_USER=$GITHUB_ACTOR`
- Build: `dotnet build maichess-insights-service.sln -c Release`
- Test+cov: `dotnet test MaichessInsightsService.Tests/MaichessInsightsService.Tests.csproj -p:CollectCoverage=true "-p:Include=[MaichessInsightsService]*"`
  → must be 100% line/branch/method on non-excluded code.
- One type per file (StyleCop SA1402). `TreatWarningsAsErrors`. Generated `*.feature.cs` is gitignored.
- `[ExcludeFromCodeCoverage]` on I/O glue: `Data/*`, `Grpc/InsightsGrpcService`, `Rest/*` adapter +
  DTO/view records, `Kafka/*`, `Services/InsightsOptions`, `Program.cs`.
- Internal enums into `[Theory]` go as `(int)` with `int` params (house pattern), cast inside.

## Task 06 — query API (in progress)

Goal: serve computed insights over REST + gRPC reading the materialized `insights_*` collections
via database-service gRPC (no direct Mongo driver), with a rebuildable Redis L1 cache on hot
aggregates. RPCs: GetTopOpenings, GetCommonEndgames, GetCommonPositions, GetTrickyPositions,
GetCorpusSummary (ListCorpora already done in task 05).

### Design decisions (task 06)

- **Read path:** `IInsightsRepository` (excluded glue) reads the `insights_*` metric
  collections from `insights-db` via `Database.DatabaseClient.List` filtered by `corpusId`.
  **Field names are camelCase** — the Spark MongoDB connector writes the Scala case-class
  field names verbatim (`corpusId`, `openingName`, `whiteWinRate`, `normalizedFen`,
  `avgCentipawnLoss`, …). This differs from the snake_case `insights_jobs`/`insights_corpora`
  the control plane owns. Confirmed against `maichess-insights-spark .../analysis/Outputs.scala`.
- **Ordering** (Mongo List is unordered, so re-sort in the pure service, mirroring the Scala job):
  openings → `gameCount` desc; endgames → `frequency` desc; positions → `reachCount` desc;
  tricky → `avgCentipawnLoss` desc, then `avgThinkTimeMs` desc; summary `first_moves` → `gameCount` desc.
- **Paging/caching:** `InsightsQueryService` (pure, fully covered) clamps limit (default 50,
  cap 500) + offset (≥0), builds an L1 key from `corpus + filters` (NOT paging), miss→repo→
  sort→serialize→`IInsightsCache.SetAsync`, then pages the full sorted list after retrieval.
  Cache is JSON string get/set, no expiry (allkeys-lru), rebuildable — `RedisInsightsCache`
  (excluded) mirrors `RedisAnalysisResultCache`.
- **404 semantics:** metric reads return `null` ⇒ "no corpus with that id" (→404), empty list ⇒
  corpus exists but nothing materialized (→200 empty). Flow: cache-hit ⇒ 200; miss ⇒ check
  `store.GetCorpusAsync`; absent ⇒ null/404; else repo. Summary `null` ⇒ 404 (no corpus or no
  summary), no corpus distinction needed.
- **REST** returns thin wrapper records holding the **domain** metric lists directly (snake_case
  JSON policy converts property names); **gRPC** maps domain→proto rows in `InsightsGrpcService`.

### Known gaps to record in CONTRACT_NOTES (task-04 follow-ups, not blocking 06)

- `OpeningStat` (task 04) emits no split (`color`/`rating_band`/`time_control`) or `trend` rows,
  so `GetTopOpenings` split filters return empty and `trend` is always `[]`. The query honors the
  filters/shape per contract; task 04 would need to emit split/trend rows.
- `PositionStat` carries no ply, so `exclude_book` can't be applied at query time (it's a job-time
  `bookPlies` concern). The flag is accepted + included in the cache key but does not change results.

### Status: task 06 DONE — built, 124 tests, 100% coverage, pushed to dev.

New files: `Domain/{Opening,OpeningTrend,Endgame,Position,Tricky,CorpusSummary,Count}Metric.cs`,
`Domain/{Openings,Positions,Paged}Query.cs`, `Services/{IInsightsRepository,IInsightsCache,
InsightsQueryService}.cs`, `Data/{InsightsRepository,RedisInsightsCache}.cs`, 5 query RPC
overrides + proto mappers in `Grpc/InsightsGrpcService.cs`, 5 REST routes + handlers in
`Rest/InsightsEndpoints.cs`, `Rest/{Summary,Openings,Endgames,Positions,Tricky}Response.cs`,
DI + `ConnectionStrings:Redis` in `Program.cs`/`appsettings.json`, `StackExchange.Redis` pkg.
Tests: `MaichessInsightsService.Tests/InsightsQueryServiceTests.cs` + `Support/Fake{InsightsRepository,
InsightsCache}.cs`. Two known task-04 gaps recorded in `CONTRACT_NOTES.md` (opening split/trend,
positions `exclude_book`).

## Task 07 — client Insights page (NOT started)

Touches **`maichess-client`** (separate repo, not this one). Read
`tasks/planned/insights/07-client-insights-page.md` and the client's existing pages/conventions.
Consumes task-06's REST surface (`/insights/corpora/{id}/{summary,openings,endgames,positions,
tricky}` + `/insights/jobs`, `/insights/corpora`, submit/upload). Build opening explorer,
endgames, common/tricky positions, and a job submit + status view. Work on a `dev` branch there
and make its pipeline build, same as here.
