# Olve.Template.Api

A .NET 10 minimal API service template. Install with `dotnet new` and scaffold a full solution with auth, telemetry, Helm chart, and client generation.

## Usage

```bash
# Install the template
dotnet new install .

# Create a new project
dotnet new olve-api -n "MyCompany.MyService"
```

## Project Structure

```
src/Olve.Template.Api/                          # API application (minimal API)
├── Configuration/                              # Auth, telemetry, JSON, host config
├── Messages/                                   # Message CRUD example feature
├── Stores/                                     # EntityStore snapshot persistence (promotion-shaped)
├── Health/                                     # Health check endpoints
└── appsettings.json                            # Default configuration
test/Olve.Template.Api.UnitTests/               # Unit tests (TUnit + Rocks)
test/Olve.Template.Api.ApiTests/                # API tests over HTTP (in-process or any base URL)
frontend/                                       # Vanilla Web Components + TS frontend (see frontend/README.md)
docs/STANDARDS.md                               # MUST/SHOULD rules for API behaviour, code and tests
artifacts/                                      # Generated output (OpenAPI document), gitignored
mise.toml                                       # Toolchain pins + tasks; `mise run ci` is the pipeline gate
tools/version.cs                                # CalVer versioning script
helm/                                           # Helm chart for Kubernetes (ClusterIP Service + SLO)
.pipelines/                                     # Olve.Pipelines CD config (build + check, beta → test → prod)
Dockerfile                                      # Multi-stage build (AOT, chiseled)
Directory.Build.props                           # Shared build properties (TFM, nullable, etc.)
Directory.Packages.props                        # Central package version management
```

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/` | No | The SPA (`frontend/`), served from `wwwroot` — see [Frontend](#frontend) |
| GET | `/health` | No | Health check, returns 200 |
| GET | `/api/auth-config` | No | Public OIDC settings for the SPA login (authority, client id, scopes) |
| GET | `/api/messages?page=<n>&pageSize=<n>` | No | List messages (paginated, 1-based) |
| POST | `/api/messages` | Yes (JWT) | Create a message (`{ "text": "…" }`) |
| PUT | `/api/messages/{id}` | Yes (JWT) | Update a message (`{ "text": "…" }`) |
| DELETE | `/api/messages/{id}` | Yes (JWT) | Delete a message |
| GET | `/openapi/v1.json` | No | OpenAPI spec |

The JSON API lives under `/api/` so the SPA can own the site root; `/health` stays at the root
for Kubernetes probes. Unmatched non-API GETs fall back to `index.html` for SPA client routing.

The `Messages` feature is the template's worked example — it exercises `Id<T>`, an
`EntityStore<Message>`, `Page<T>` pagination, the `IHandler` + `.WithValidation(...)` pattern, and
`IAsyncOnStartup` wiring (a welcome message is seeded on first run).

## Build & Test

[mise](https://mise.jdx.dev/) is the single entry point: it pins node and dotnet and runs the
same tasks for you, Claude and the pipeline. Each task delegates to the native build, so plain
`dotnet` / `npm` commands work too.

```bash
mise run ci              # Everything the pipeline's check step runs
mise run backend:test    # dotnet build + dotnet test (unit + in-process API tests)
mise run client:check    # Regenerate the TS client; fail if the committed one drifted
mise run frontend:check  # Frontend lint, tests, production build
mise run api:image       # API tests against the real Docker image (needs Docker)
API_BASE_URL=https://… mise run api:remote   # API tests against a deployed server

dotnet test                          # Unit + API tests
dotnet test -p:RunApiTests=false     # Unit tests only
```

**API tests** (`test/Olve.Template.Api.ApiTests`) are one HTTP suite with a selectable target
(`ApiTarget`): in-process via `WebApplicationFactory` by default, or any running server when
`API_BASE_URL` is set. To mint tokens against a base URL, also set `API_SIGNING_KEY`,
`API_ISSUER` and `API_AUDIENCE` to the server's `Auth:*` settings; without them, tests that need a
token skip. The same suite runs in the pipeline twice: in-process in `check`, and against live
beta in `test-after-beta`.

In-process tests run JIT, so they can't catch what only breaks in the AOT-published binary (a
missing `[JsonSerializable]`, reflection). `mise run api:image` builds the real image and runs the
suite against it; run it when you touch serialization. `test-after-beta` covers the same ground
for every deploy.

To add a dependency (e.g. PostgreSQL), give the in-process host its connection string in
`ApiFactory.ConfigureWebHost` (`builder.UseSetting("ConnectionStrings:Default", …)`), pointing at
a per-run database or container, and pass the same setting as an env var in `api:image`.

## Running

```bash
# Local
dotnet run --project src/Olve.Template.Api

# Kubernetes
helm install olve-template-api helm/
```

## Deployment (GitOps)

This template ships a `.pipelines/` directory, which makes it deploy out of the box via
[**Olve.Pipelines**](https://github.com/OliverVea/Olve.Pipelines) — Oliver's lightweight GitOps CD
service. `.pipelines/config.yaml` is the **single source of truth** for how the app is built and
deployed (the deploy equivalent of `.github/workflows/`): once a pipeline is bound to the repo, the
controller reconciles it to this file and **pushing to `main` redeploys automatically**.

The pipeline shape:

- **Production steps run in parallel** — `build-and-package` (Kaniko build → image tar + Helm chart)
  and `check` (`mise run ci`: backend unit + in-process API tests, the client drift check, frontend
  lint/test/build). A failure fails the group and **gates the deploy** (nothing ships).
- **Processing steps run sequentially** — `deploy-beta` (namespace `apps-beta`) → `test-after-beta`
  → `deploy` (namespace `apps`). **Beta gates prod**: if the beta rollout, its health check, or the
  API test suite run against the live beta Service (in-cluster; token tests skip) fails, prod never
  deploys.
- **Secrets are by name only** (`GITHUB_TOKEN`, `SSH_PRIVATE_KEY`); their values live in the
  pipeline's own k8s secret, never in the repo.
- The step scripts source a shared [`olve-lib.sh`](https://github.com/OliverVea/Olve.Pipelines/blob/main/.pipelines/scripts/olve-lib.sh)
  (Kaniko/SSH/Helm footgun helpers) and only parameterize app-specifics, so they stay tiny. They
  fetch it from `main`; swap that for a tag/SHA to pin.

**Homelab conformance.** The Helm chart renders a **`ClusterIP` Service only — no Ingress**
([`Olve.Homelab`](https://github.com/OliverVea/Olve.Homelab) is the edge chart that owns all Ingress).
Routing is registered by adding the app's host + service to the edge chart's `apps:` list in
`values-{beta,prod}.yaml` — **not** in this chart. The `deploy-beta` health-gate probes the
Tailscale-private host `https://<app>-private.ovea.pro/health` from the homelab node, so that host
must be registered in the edge chart before the gate can pass.

### Per-namespace prerequisites (what bites a fresh deploy)

Building and rolling out is automatic, but a generated app needs a few things provisioned in each
target namespace before the pod actually runs. Each of these surfaced on a real deploy:

- **Edge route** — add an entry to `Olve.Homelab`'s `values-{beta,prod}.yaml` `apps:` list (host
  `<app>-private.ovea.pro`, external-dns target `100.100.117.17`, LE TLS). Without it the health
  gate has nothing to probe.
- **OTLP telemetry auth** — beta's `otel-beta.ovea.pro` is unauthenticated (Tailscale); prod's
  `otel.ovea.pro` needs OAuth2 as the shared `otel` client, whose secret is the
  `authentik-oidc-secrets` key **`otel-client-secret`**. (The chart defaults are correct; just
  ensure that secret exists in `apps`.)
- **Authentik CA** — the chiseled image can't validate `*.ovea.pro` TLS, so the chart mounts the
  shared `authentik-ca` configMap (`authentikCa.enabled`). That configMap must exist in the namespace.

Inspect runs, jobs, and logs with the **`pl` CLI** (`pl pipeline list`, `pl job logs <id>`,
`pl binding status <id>`). The [`ovea-olve-pipelines`](https://github.com/OliverVea/Olve.Pipelines)
skill and the instance's `/docs` (served at
[`pipelines-private.ovea.pro`](https://pipelines-private.ovea.pro), beta at `pipelines-beta.ovea.pro`)
are the authoritative reference for the config schema, promotion gates, and the deploy model — start
there rather than re-deriving it.

## Configuration

Sources in priority order (highest wins):

1. CLI args (`--Port 9090`)
2. User secrets (`dotnet user-secrets set "Key" "value"`)
3. Environment variables
4. `appsettings.{Environment}.json`
5. `appsettings.json`

| Key | Default | Description |
|-----|---------|-------------|
| `Host` | `localhost` | Listen address |
| `Port` | `5000` | Listen port |
| `Auth:Authority` | `https://auth.ovea.pro/...` | OIDC authority (Authentik) |
| `Auth:Audience` | `olve-template-api` | JWT audience |
| `Auth:SigningKey` | _(null)_ | Local HS256 key (bypasses OIDC, for dev) |
| `OpenTelemetry:Endpoint` | `https://otel.ovea.pro` | OTLP endpoint (null = disabled) |
| `Storage:Mode` | `Ephemeral` | `Ephemeral` (in-memory) or `Persistent` (snapshot to disk) |
| `Storage:Directory` | `data` | Directory for `Persistent` snapshots |

### Persistence

The `Messages` feature is backed by an in-memory `EntityStore<Message>`. By default storage is
`Ephemeral` (state is lost on restart). Set `Storage:Mode=Persistent` to have the store load on
startup and save a debounced whole-snapshot JSON to `Storage:Directory` via the BCL-only
`FileSnapshotStore` — both wired in `Messages/MessageEndpoints.cs`.

Everything sits behind the `ISnapshotStore` seam (`Stores/`), so the persistence ladder — in-memory →
file → S3/MinIO → relational — is a one-line swap at registration without touching the store or
handlers. The `Stores/` module is written at library quality for later promotion to
`Olve.Utilities.Hosting`.

## Client Generation

The build writes the OpenAPI document to `artifacts/openapi/api.json` (gitignored). The frontend's
TypeScript client is generated from it with [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview)
and **committed** in `frontend/src/api`, so neither the frontend nor the Docker build needs dotnet:

```bash
dotnet build && npm run --prefix frontend generate-client
```

`mise run client:check` (part of `ci`) regenerates it and fails if it changed, so a stale client
can't ship. There is no C# client: the API tests talk raw HTTP.

## Frontend

`frontend/` is the template's companion UI: a no-framework, **vanilla Web Components** app in
**TypeScript**, consuming the API through its own Kiota-generated client. It ships a
`<message-list>` CRUD view over the backend `Message` feature, proving the client-gen →
component → API loop end to end.

The stance is deliberate (DESIGN §2): standalone custom elements, ES modules, and a shared
`BaseElement` that provides ergonomics only — **explicit `render()`, no automatic
re-rendering**. A component that outgrows this can `npm i lit` and switch its own base to
`LitElement` per-component; auto-rerender is always opt-in, never the baseline.

It's served **same-origin**: the Dockerfile's Node stage builds `frontend/dist` into the app's
`wwwroot`, so the deployed API serves the SPA at `/` and the JSON API at `/api/` (one host, no
CORS). Locally you run it on Vite instead, which proxies `/api` to the backend:

```bash
cd frontend && npm install && npm run dev    # proxies /api to the API (VITE_API_TARGET)
```

See [`frontend/README.md`](frontend/README.md) for the layout, run/build commands, auth for
writes, and how to regenerate the client (including the Kiota-runtime version pin).

## Versioning

The `tools/version.cs` script computes CalVer versions:

```bash
# Local development
dotnet run tools/version.cs
# version=0.0.0-dev+cb9a99b

# CI (pass run number from GitHub Actions)
dotnet run tools/version.cs -- --ci --run-number 42
# version=2026.3.28.42+cb9a99b

# With runtime identifier for artifact naming
dotnet run tools/version.cs -- --ci --run-number 42 --rid linux-x64
# artifact-name=olve-template-api-2026.3.28.42+cb9a99b-linux-x64
```

## CI

The pipeline (`.pipelines/`) is the CI: its `check` step runs `mise run ci`. Anything else that
wants the same gate (a GitHub Actions workflow, a pre-push hook) should run that one command too:

```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]
jobs:
  ci:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: jdx/mise-action@v2
      - run: mise run ci
```

## Architecture & References

Each major component, what it does in the template, and the full set of links (docs, source,
running instance/tooling, and the Claude Code skill that knows the model).

| Component | Role in the template | Docs | GitHub | Instance / tooling | Skill |
|---|---|---|---|---|---|
| **Olve.Utilities** stack (Results, Validation, MinimalApi, Utilities) | Baked-in error handling, validation, result→HTTP mapping, `Id<T>`/`EntityStore<T>` primitives | [docs site](https://olivervea.github.io/Olve.Utilities/) | [OliverVea/Olve.Utilities](https://github.com/OliverVea/Olve.Utilities) | NuGet | *(none yet — gap)* |
| **Olve.Pipelines** | GitOps CD — builds & deploys this repo via `.pipelines/` (see [Deployment](#deployment-gitops)) | in-repo `docs/setup/`, served at `/docs` + `llms.txt` | [OliverVea/Olve.Pipelines](https://github.com/OliverVea/Olve.Pipelines) | [`pipelines-private.ovea.pro`](https://pipelines-private.ovea.pro), beta `pipelines-beta.ovea.pro`, hooks `pipelines-hooks.ovea.pro`; **`pl` CLI** via `GET /download/{asset}` | `ovea-olve-pipelines` |
| **Olve.Homelab** | Edge chart that owns all Ingress; public exposure is registered there, not in this chart | — | [OliverVea/Olve.Homelab](https://github.com/OliverVea/Olve.Homelab) | — | — |
| **TUnit · Rocks · Kiota · mise** | Test framework, AOT mocking, TS client generation, toolchain + tasks | see per-library links below | — | — | — |

Per-library documentation:

- [Olve.MinimalApi](https://olivervea.github.io/Olve.Utilities/src/Olve.MinimalApi/README.html) — Minimal API extensions for result mapping, validation, and JSON conversion
- [Olve.Results](https://olivervea.github.io/Olve.Utilities/src/Olve.Results/README.html) — Functional result types for non-throwing error handling
- [Olve.Validation](https://olivervea.github.io/Olve.Utilities/src/Olve.Validation/README.html) — Fluent input validation built on Olve.Results
- [Olve.Utilities](https://olivervea.github.io/Olve.Utilities/src/Olve.Utilities/README.html) — Meta-package bundling utility libraries including identifiers, collections, and graph types
- [TUnit](https://tunit.dev/docs/intro) — Test framework (not xUnit/NUnit). Uses `await Assert.That(...)` fluent syntax
- [Rocks](https://raw.githubusercontent.com/JasonBock/Rocks/refs/heads/main/docs/Overview.md) — Source-generated mocking library for AOT-compatible test doubles
- [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) — Microsoft's OpenAPI client generator for TypeScript (and other languages)
- [mise](https://mise.jdx.dev/) — Toolchain pinning + task runner (`mise run ci`)
