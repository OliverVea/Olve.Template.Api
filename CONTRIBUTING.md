# Contributing

## Project structure

```
src/
├── spec/                               The API contract (TypeSpec, one .tsp per area); bun run build → artifacts/ (gitignored):
│                                       openapi.json, the server's generated C# and the Hey API TypeScript client
├── backend/
│   ├── Olve.Template.Api/              The server (minimal API on the generated surface, Native AOT)
│   │   ├── Configuration/              Host, logging, auth, telemetry, API wiring, the error writer
│   │   ├── Messages/                   The worked example: message CRUD
│   │   ├── Stores/                     EntityStore snapshot persistence
│   │   └── Health/                     /health
│   ├── Olve.Template.Api.UnitTests/    Unit tests (TUnit + Rocks)
│   └── Olve.Template.Api.ApiTests/     API tests over HTTP (TUnit): in-process, or API_BASE_URL
├── frontend/                           Vanilla Web Components + TypeScript SPA (bun), served from the server's wwwroot
└── deploy/helm/                        The chart: a ClusterIP Service, no Ingress
.pipelines/                             The Olve.Pipelines pipeline (beta, then prod)
docs/design/                            Architecture and roadmap
docs/STANDARDS.md                       MUST/SHOULD rules for API behaviour, code and tests
tools/check.sh                          The checks; the pipeline's test step runs --full
tools/api-image.sh                      The API tests against the Docker image (the AOT check)
tools/version.cs                        CalVer versions
```

## Build and run

Needs the .NET 10 SDK and [bun](https://bun.sh) (the contract and the frontend).

```bash
dotnet build && dotnet test                             # compiles src/spec/ first (bun)
dotnet run --project src/backend/Olve.Template.Api      # localhost:5000
cd src/frontend && bun install && bun run dev           # the SPA on Vite, proxying /api
tools/check.sh [--full|--fix|--changed]                 # what the pipeline checks; --fix formats first
```

`git config core.hooksPath .githooks` runs `tools/check.sh --hook` before every push; Claude Code runs it
from the `PreToolUse` hook in `.claude/settings.json`. The hook checks only what changed against the
upstream branch (`--changed`): a docs-only push checks nothing, a backend-only push skips the frontend.

### Configuration

Sources, highest first: command-line args (`--Port 9090`), user secrets (`dotnet user-secrets set "Key"
"value"`), environment variables, `appsettings.{Environment}.json`, `appsettings.json`.

| Key | Default | Description |
|-----|---------|-------------|
| `Host` | `localhost` | Listen address |
| `Port` | `5000` | Listen port |
| `Auth:Authority` | `https://auth.ovea.pro/...` | OIDC authority (Authentik) |
| `Auth:Audience` | `olve-template-api` | JWT audience |
| `Auth:SigningKey` | — | Local HS256 key (bypasses OIDC, for dev and tests) |
| `Logging:Console:FormatterName` | `simple` | `json` for JSON lines (the chart sets it) |
| `OpenTelemetry:Endpoint` | — | OTLP endpoint (traces, metrics and logs under `/v1/…`); unset or empty = no exporter |
| `OpenTelemetry:Protocol` | gRPC | `http` for OTLP over HTTP/protobuf |
| `OpenTelemetry:Headers` | — | OTLP headers, `k1=v1,k2=v2`, e.g. a backend's API key |
| `Storage:Mode` | `Ephemeral` | `Ephemeral` (in-memory) or `Persistent` (snapshot to disk) |
| `Storage:Directory` | `data` | Directory for `Persistent` snapshots |

**Telemetry.** Deployed, the app writes JSON log lines to stdout and the homelab's node collector ships
them to OpenObserve; the app holds no telemetry credentials. OTLP export is off by default; the chart's
`telemetry` value turns it on (`exporter: otlp`, `endpoint`, `protocol`, and `headers` from a Secret).

**Persistence.** `Messages` is backed by an in-memory `EntityStore<Message>`. `Storage:Mode=Persistent`
loads it on startup and saves a debounced JSON snapshot to `Storage:Directory` (`FileSnapshotStore`,
wired in `Messages/MessageServices.cs`). Everything sits behind the `ISnapshotStore` seam (`Stores/`), so
moving to S3 or a database is a swap at registration.

## The contract

An API change is a `.tsp` change plus one `[OperationHandler]` class per operation, implementing the
generated `I<Group><Op>Handler` and returning a variant of its response union; a missing handler fails
the build. The server build compiles the contract when it changed (Olve.TypeSpec's `TypeSpecContract`
target, with bun); `-p:SkipSpecGen=true` compiles already-generated files (the Docker build). `AddApi`
and `MapApi` in `Configuration/ApiConfiguration.cs` wire the handlers, and `MapApi` opens the anonymous
routes. The published server serves the OpenAPI document at `/openapi/v1.json`.

The frontend calls the server only through the generated client (`@api/*`, in `artifacts/clients/ts`).
Nothing generated is committed. There is no C# client: the API tests talk raw HTTP.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | No | The SPA (`src/frontend/`), served from `wwwroot` |
| GET | `/health` | No | 200 when up |
| GET | `/api/auth-config` | No | Public OIDC settings for the SPA login |
| GET | `/api/messages?page=<n>&pageSize=<n>` | No | List messages (1-based pages) |
| POST | `/api/messages` | Yes | Create a message (`{ "text": "…" }`) |
| PUT | `/api/messages/{id}` | Yes | Update a message |
| DELETE | `/api/messages/{id}` | Yes | Delete a message |
| GET | `/openapi/v1.json` | No | The OpenAPI document |

## Tests

`dotnet test` runs the unit tests and the API tests in-process (`dotnet test -p:RunApiTests=false` for
unit tests only). The API tests (`ApiTarget`) run against any server when `API_BASE_URL` is set:
authenticated tests send `API_TOKEN`, or mint one from `API_SIGNING_KEY`, `API_ISSUER` and
`API_AUDIENCE`; with neither, they skip.

```bash
API_BASE_URL=https://… dotnet test --project src/backend/Olve.Template.Api.ApiTests
tools/api-image.sh       # against the real Docker image (needs Docker)
```

In-process tests run JIT, so they can't catch what only breaks in the AOT binary (a missing
`[JsonSerializable]`, reflection); run `tools/api-image.sh` when you touch serialization. To add a
dependency (e.g. PostgreSQL), give the in-process host its connection string in
`ApiFactory.ConfigureWebHost` and pass the same setting as an env var in `tools/api-image.sh`.

## Deploy

`.pipelines/pipeline.ts` on Olve.Pipelines v2 (pipeline "Olve.Template.Api"); pushing to `main` deploys.
Load the `ovea-olve-pipelines` skill before changing `.pipelines/` or running `pl`, and check a change
with `pl dev describe`.

1. `source` (the pushed commit), then `build` (kaniko, base images through the homelab pull-through
   caches) ∥ `test` (`tools/check.sh --full`).
2. `deploy-beta` (Helm, `apps-beta`) → `beta-test`: the whole API test suite against beta's in-cluster
   Service with a token from beta Authentik's `olve-template-api-tests` machine client, and the SPA.
3. `deploy-prod` (`apps`) → `prod-test`: read-only smoke test (`/health`, `/api/messages`,
   `/api/auth-config`, the SPA, an anonymous write answering 401).

Any failure stops the run before prod; `deployTo=beta` stops after beta. Secrets are by name only
(`GITHUB_TOKEN`, `SSH_PRIVATE_KEY`, `API_TEST_CLIENT_SECRET`, set with `pl secret set`). The pipeline
follows the SDK's `beta` tag with no lockfile.

A fresh deploy needs, per namespace:

- **A route.** The chart has no Ingress: add the app to `Olve.Homelab`'s `values-{beta,prod}.yaml`
  `apps:` list (host `<app>-private.ovea.pro`, external-dns target `100.100.117.17`). The pipeline's
  checks use the in-cluster Service, so they don't need it.
- **The Authentik CA.** The chiseled image can't validate `*.ovea.pro` TLS, and JWT validation fetches
  JWKS from Authentik, so the chart mounts the shared `authentik-ca` configMap (`authentikCa`).
- **Authentik applications** for the API and the SPA (in Olve.Authentik), and the beta machine client.

## Versioning

```bash
dotnet run tools/version.cs                                    # 0.0.0-dev+cb9a99b
dotnet run tools/version.cs -- --ci --run-number 42            # 2026.3.28.42+cb9a99b
dotnet run tools/version.cs -- --ci --run-number 42 --rid linux-x64
```
