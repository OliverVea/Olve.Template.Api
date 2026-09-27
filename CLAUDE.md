# CLAUDE.md

See [README.md](README.md) for project structure, endpoints, configuration, CI examples, and client generation.

## Commands

```bash
dotnet build                                                # Build
dotnet test                                                 # Unit + API tests (API tests in-process)
dotnet test -p:RunApiTests=false                            # Unit tests only
dotnet run --project src/Olve.Template.Api                  # Run locally
```

`mise run ci` (or `npx mise run ci` without mise installed) is the pipeline gate: backend tests,
client drift check, frontend. `mise run api:image` runs the API tests against the Docker image
(the AOT check; needs Docker), `API_BASE_URL=… mise run api:remote` against any running server.

## Conventions

- .NET 10, C# with file-scoped namespaces, nullable enabled, implicit usings
- Package versions managed centrally in `Directory.Packages.props` — do not add `Version` attributes in csproj files
- Local config via `dotnet user-secrets`, not appsettings files
- OpenAPI spec is generated on build into `artifacts/openapi/api.json` (gitignored) by `Microsoft.Extensions.ApiDescription.Server`; generated output never goes in source folders, except the committed Kiota client in `frontend/src/api` (kept honest by `mise run client:check`)
- API tests are raw HTTP behaviour tests that must pass against any target (`ApiTarget`); no C# client

## Standards

Follow [`docs/STANDARDS.md`](docs/STANDARDS.md) (MUST/SHOULD rules for API behaviour, code and tests).

## Decisions are dated leanings

Notes under `docs/` record *dated leanings*, not binding decisions. Use them to orient when in
doubt, but before relying on one that predates the current work, confirm with Oliver that it
still holds. When a leaning changes, rewrite the entry and its date.

## Deployment (GitOps)

This repo deploys via **Olve.Pipelines** — the `.pipelines/` directory is the live deploy config
(single source of truth; pushing to `main` redeploys). Build + `check` (`mise run ci`) run in
parallel and gate `deploy-beta` → `test-after-beta` (API tests against beta) → `deploy` (beta gates
prod). The Helm chart is **ClusterIP-only**; public exposure is registered in the `Olve.Homelab`
edge chart, not here. **Invoke the `ovea-olve-pipelines` skill** for
the authoritative model (config schema, secrets, promotion gates) before changing `.pipelines/` —
don't re-derive it. See [README.md](README.md#deployment-gitops) for the full write-up.

**Homelab deploy gotchas** (each bit us on a real deploy; all validated against Olve.Pipelines):

- **OTLP OAuth (prod only).** `otel-beta.ovea.pro` is **unauthenticated** (Tailscale) — beta clears
  `OpenTelemetry__OAuth2__TokenUrl`/`ClientId` or the app crashes at startup. Prod OTLP auths as the
  shared **`otel`** client, so its secret is the `authentik-oidc-secrets` key **`otel-client-secret`**
  (NOT `<app>-client-secret` — a different client → `invalid_grant`).
- **Authentik CA.** The chiseled base image can't validate `*.ovea.pro` TLS, so outbound HTTPS
  (prod OTLP OAuth, JWKS, OpenBao) fails with "SSL connection could not be established". Mount the
  shared `authentik-ca` configMap via `authentikCa.enabled` (`auth-prod-ca.crt`/`auth-beta-ca.crt`).
- **Routing.** No route exists until the app is added to `Olve.Homelab`'s `values-{beta,prod}.yaml`
  `apps:` list. Private/Tailscale host is `<app>-private.ovea.pro` (external-dns → `100.100.117.17`);
  the `deploy-beta` health gate probes it from the homelab node over SSH.
- **Telemetry auth is opt-in, never fatal** — empty OAuth2 config disables it (see `TelemetryConfiguration`).

## References

- [Olve.* packages](https://olivervea.github.io/Olve.Utilities/) ([GitHub](https://github.com/OliverVea/Olve.Utilities)) — index of all Olve packages
  - [Olve.Results](https://olivervea.github.io/Olve.Utilities/src/Olve.Results/README.html) — non-throwing result types for error handling
  - [Olve.Validation](https://olivervea.github.io/Olve.Utilities/src/Olve.Validation/README.html) — input validation built on Olve.Results
  - [Olve.MinimalApi](https://olivervea.github.io/Olve.Utilities/src/Olve.MinimalApi/README.html) — result-to-HTTP mapping for minimal APIs
  - [Olve.Utilities](https://olivervea.github.io/Olve.Utilities/src/Olve.Utilities/README.html) — identifiers, collections, graph types
  - [Olve.Results.TUnit](https://olivervea.github.io/Olve.Utilities/src/Olve.Results.TUnit/README.html) — TUnit assertions for Result types (`Succeeded()`, `Failed()`, etc.)
- [Olve.Pipelines](https://github.com/OliverVea/Olve.Pipelines) — GitOps CD service; deploy model for this repo's `.pipelines/`. Skill: `ovea-olve-pipelines`. Instances: `pipelines-private.ovea.pro` (prod), `pipelines-beta.ovea.pro` (beta)
- [Olve.Homelab](https://github.com/OliverVea/Olve.Homelab) — edge chart that owns all Ingress; register an app's public host + service here, not in the app chart
- [mise](https://mise.jdx.dev/) — toolchain pinning + task runner (`mise run ci`)
- [TUnit](https://tunit.dev/docs/intro) — test framework, uses `await Assert.That(...)` fluent syntax (not xUnit/NUnit)
- [Rocks](https://raw.githubusercontent.com/JasonBock/Rocks/refs/heads/main/docs/Overview.md) — source-generated mocking (AOT-compatible)
- [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) — TypeScript client gen from OpenAPI
