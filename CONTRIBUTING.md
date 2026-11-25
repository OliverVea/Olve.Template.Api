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
tools/template-parity.ts                What the olve-api template has that this repo lacks (Template parity)
.template.json                          The template version this repo synced with, and its opt-outs
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
`--full` limits a test run to 15 minutes and dumps a test host that is silent for 5, naming the hung
tests.

### Configuration

Sources, highest first: command-line args (`--Port 9090`), user secrets (`dotnet user-secrets set "Key"
"value"`), environment variables, `appsettings.{Environment}.json`, `appsettings.json`.

| Key | Default | Description |
|-----|---------|-------------|
| `Host` | `localhost` | Listen address |
| `Port` | `5000` | Listen port |
| `Auth:Authority` | — | OIDC issuer (deployed: the bundled Dex, `https://<host>/dex`) |
| `Auth:Audience` | — | JWT audience (the Dex client id) |
| `Auth:FrontendAuthority`, `Auth:FrontendAudience` | — | The SPA's issuer and client id when they differ; also trusted |
| `Auth:Allow:{Subjects,Users,Orgs,Emails}:<n>` | — | Who may sign in (GitHub logins, orgs or `org:team`, verified emails, `sub`); empty = any valid token |
| `Auth:Tokens:<name>` | — | sha256 (hex) of a machine token (`olve-template-api_…`), which acts as `<name>` past the allow-list |
| `Auth:Disable` | `false` | Every endpoint anonymous (the chart's `auth.mode: none`) |
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

## The SPA's files

`StaticCaching` (`Configuration/StaticCaching.cs`) sets every SPA file's `Cache-Control`. The image's
files all carry a 1970 mtime, so without it a browser keeps a stale `index.html` for years, and an ETag
(mtime XOR length) can't tell two builds' pages apart: those files ignore the browser's validators and
always answer 200.

| Path | `Cache-Control` |
|------|-----------------|
| `/assets/*` (Vite's content-hashed bundle) | `public, max-age=31536000, immutable` |
| `/icons/*` | `public, max-age=86400` |
| `/sw.js`, `/manifest.webmanifest` | `no-store`: no cache on the way (browser, proxy, CDN) holds back an update |
| Everything else (`/`, `index.html`, client-side routes) | `no-cache` |
<!--#if (pwa) -->

## The installable app (PWA)

The SPA is an installable web app: `src/frontend/public/manifest.webmanifest` (name, short name, theme
colours, `display: standalone`), the icons in `public/icons/` (`icon.svg`, 192 and 512 px PNGs, a
maskable 512, the 180 px `apple-touch-icon.png` for iOS; replace them with the service's own), and a
service worker at `/sw.js`.

- **The worker.** The build writes `dist/sw.js` (`vite-plugin-sw.ts`): every built file precached under
  a cache named for the build's version, a hash of the build. It answers the precached files from that
  cache and page loads from the network, with the cached `index.html` as the offline fallback. It never
  touches `/api`, `/dex`, `/callback`, `/health` or `/openapi`, and caches nothing at runtime. On
  activate it deletes older versions' caches. `bun run dev` has no worker.
- **Updates.** A deploy changes `sw.js`; the browser checks it on each load, when the page is shown
  again and hourly. The new worker installs beside the running one and waits; the page shows "A new
  version is available. Reload", and only Reload lets the new worker take over and reloads the page,
  once (`src/frontend/src/pwa/register.ts`).
- **A service's own handlers** (`push`, `notificationclick`, …) go in `src/frontend/src/sw/extensions.js`,
  which the build appends to the shell (`src/sw/shell.js`); leave the shell as it is.

`PwaTests` and `SpaCachingTests` check the served files and their headers against a target that serves
the SPA (`tools/api-image.sh`, beta); in-process they skip. The worker and the update flow have unit
tests in `src/frontend/src/pwa/`.
<!--#endif -->

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
   Service with beta's machine token (`BETA_TOKEN`), that beta's Dex answers discovery with the issuer
   the SPA is given, and the SPA.
3. `deploy-prod` (`apps`) → `prod-test`: read-only smoke test (`/health`, `/api/messages`,
   `/api/auth-config`, the SPA, an anonymous write answering 401).

Any failure stops the run before prod; `deployTo=beta` stops after beta. Secrets are by name only
(`GITHUB_TOKEN`, `SSH_PRIVATE_KEY`, `BETA_TOKEN`, `DEX_GITHUB_SECRET_BETA`, `DEX_GITHUB_SECRET_PROD`, set with
`pl secret set`). The pipeline
follows the SDK's `beta` tag with no lockfile.

A fresh deploy needs, per namespace:

- **A route.** The chart has no Ingress: add the app to `Olve.Homelab`'s `values-{beta,prod}.yaml`
  `apps:` list (host `<app>-private.ovea.pro`, external-dns target `100.100.117.17`). The pipeline's
  checks use the in-cluster Service, so they don't need it.
- **Sign-in.** The pipeline's `auth` config (per target: `target.<stage>.auth`), the same shape as
  Olve.Pipelines': `mode: dex`, `issuer: https://<host>/dex`, and `allow` (the chart refuses dex without
  one, since GitHub lets anyone sign in). The chart then runs Dex beside the app, which needs:
  - a GitHub OAuth app per stage with callback URL `https://<host>/dex/callback`: its Client ID in the
    `dexBeta` / `dexProd` config (`clientId`, and `clientSecret: { secret: DEX_GITHUB_SECRET_BETA }` /
    `_PROD`), its client secret as that pipeline secret. The chart renders the Secret Dex reads; each
    deploy reads only its own stage's;
  - a `/dex` route on the app's host in `Olve.Homelab`, to the `olve-template-api-dex` Service, port 5556.
  `bundled: false` uses an existing Dex at `issuer` instead, and needs neither (the two `DEX_GITHUB_SECRET_*`
  secrets must still be set, to anything, since the deploys declare them): that Dex must list
  `clientId` (default `olve-template-api`) as a public static client with `https://<host>/callback` as a
  redirect URI, and load GitHub groups if `allow.orgs` is set. beta-test then checks discovery at `issuer`.
- **Beta's machine token.** A random `olve-template-api_<hex>` as the `BETA_TOKEN` secret, and its sha256
  (hex) in the beta target's config: `set: { config.Auth__Tokens__beta-test: <hex> }`. Per instance, never
  in the repo: a generated service would otherwise accept the template's token.

## Versioning

```bash
dotnet run tools/version.cs                                    # 0.0.0-dev+cb9a99b
dotnet run tools/version.cs -- --ci --run-number 42            # 2026.3.28.42+cb9a99b
dotnet run tools/version.cs -- --ci --run-number 42 --rid linux-x64
```

## Template parity

This repo comes from the `olve-api` template. `.template.json` records the template version it last
synced with and the template features it opts out of. `tools/template-parity.ts` compares the repo with
the template's feature manifest (`template-features.json` on the template's `main`) and lists what is
missing, adopted and opted out. It is a report: `tools/check.sh --full` runs it and never fails on it.

```bash
bun tools/template-parity.ts                    # against the template's main
bun tools/template-parity.ts --manifest <path>  # against a template checkout's template-features.json
bun tools/template-parity.ts --strict           # exit 1 when a feature is missing
```

A feature counts as adopted when its check passes. Opting out is the owner's decision, recorded with
its reason:

```json
{
  "template": "olve-api",
  "version": "2026.11.14.23",
  "features": {
    "pwa": "declined: no browser UI yet",
    "frontend-stack": "n/a: Lit by design"
  }
}
```

`declined` is a choice that may change; `n/a` means the feature doesn't fit this service. `adopted` marks
a `manual` feature, one no file check can see. Features the template introduced after `version` are
tagged "new since"; after taking them over (by hand, from the template's diff), raise `version` to the
template version compared against.
<!--#if (false) -->

### Adding a feature to the manifest

`template-features.json` lists the template's cross-cutting features, the ones a service should keep in
step with. It stays in the template repo (`dotnet new` leaves it out); children fetch it from `main`.
A change that adds such a feature to the template adds its entry in the same commit:

| Field | Meaning |
| --- | --- |
| `id` | Stable kebab-case id; children's `.template.json` refer to it, so it is never renamed |
| `description` | One line |
| `since` | The first template version (`yyyy.M.d.<run>`) that ships it; the date `yyyy.M.d` it lands on when the run isn't known yet; `null` while the template doesn't ship it |
| `origin` | The child it came from, when it flowed up |
| `check` | How to detect it in a repo (below) |

Checks are one of a fixed set, so no feature needs code. Globs match the files git sees (tracked and
untracked, not ignored), relative to the repo root; patterns are JavaScript regular expressions.

| `kind` | Passes when |
| --- | --- |
| `file` (`glob`) | A file matches |
| `contains` (`glob`, `pattern`) | A matching file contains the pattern |
| `absent` (`glob`, `pattern`) | No matching file contains the pattern |
| `package` (`name`, `atLeast`?) | `Directory.Packages.props`, a `.csproj` or a `package.json` references the package, at that version or newer |
| `manual` | Never: only an `adopted` declaration counts |
| `all` / `any` (`checks`) | Every / some nested check passes |

Write checks for the layout every child shares (`src/**/*.cs`, `src/deploy/helm/…`), never with the
template's own name in a path. Try a new check against the children before landing it
(`--repo <checkout> --manifest template-features.json`). A feature the template should have but doesn't
yet (`typed-ids`) is listed with `"since": null`: the template's own report shows it missing until it
is built.

### Features that flow up

A child that builds something every service should have proposes it to the owner. Once agreed it goes
in `docs/design/roadmap.md` with its origin. When the template ships it, its manifest entry carries
`"origin": "<child>"`: the origin child's check already passes, and every other child sees it as
missing. Where the design puts it in a package (Olve.Utilities), the check is a `package` check on that
version, so children converge by bumping it.

The pack project stamps the package version into the `.template.json` a new service gets (`0.0.0-dev`
in this repo); a service generated with `--pwa false` declines `pwa` there. An option that leaves a
feature out declines it the same way, with a `//#if` in `.template.json`.
<!--#endif -->
