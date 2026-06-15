# Contract Notes — maichess-insights-service

## Pending: `Maichess.PlatformProtos` v0.14.0 publish (task 01)

The insights contract was authored in `maichess-api-contracts` (task
`planned/insights/01-contracts-and-insights-db.md`):

- `protos/insights-service/v1/insights.proto` — the `Insights` service (job control
  + query RPCs) and all metric/row messages.
- `protos/events/v1/insights_events.proto` — `insights.events.v1` job-lifecycle
  envelope (relayed to `socket.outbound.v1`).
- `rest/insights.md` — the REST surface.

These are **purely additive** (a brand-new service file + a new event topic), so
`buf breaking` is a no-op and no existing contract changed.

**Blocker / handoff:** the package is not published yet. Before this service can
compile against the generated types, the user must commit, tag **`v0.14.0`** (next
after `0.13.0`), and push `maichess-api-contracts` so the
`Maichess.PlatformProtos` package (C#/Scala/TS) builds. `buf lint` / `buf generate`
were **not** runnable in the authoring shell (no local `buf`); CI runs them on the
tag, and the user verifies generation.

### Version reconcile decision

Convention 2 (reconcile *all* consumers to the new version) is intentionally
**deferred** for this additive contract. Every existing service still consumes only
`0.13.0` symbols — nothing they use changed — so mass-bumping their pinned
`Maichess.PlatformProtos` to `0.14.0` now would only break their builds until the
publish lands, for no functional gain. Each insights consumer is pinned to
`0.14.0` (or later) when its code is written:

- `maichess-insights-service` (.NET) — tasks 05 / 06.
- `maichess-insights-spark` (Scala) — tasks 03 / 04.
- `maichess-client` (TS) — task 07.

The contracts repo's own `Version` markers (`dotnet/*.csproj`, `npm/package.json`)
were bumped to `0.14.0` to reflect the next published baseline.
