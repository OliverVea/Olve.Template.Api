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
test/Olve.Template.Api.IntegrationTests/        # Integration tests (TUnit + Testcontainers)
clients/Olve.Template.Api.Client/               # Generated C# client (Refitter CLI + Refit)
clients/olve-template-api-client-ts/            # Generated TypeScript client (Kiota)
tools/version.cs                                # CalVer versioning script
helm/                                           # Helm chart for Kubernetes (ClusterIP Service + SLO)
.pipelines/                                     # Olve.Pipelines CD config (build, test, deploy beta→prod)
Dockerfile                                      # Multi-stage build (AOT, chiseled)
Directory.Build.props                           # Shared build properties (TFM, nullable, etc.)
Directory.Packages.props                        # Central package version management
```

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | No | Health check, returns 200 |
| GET | `/messages?page=<n>&pageSize=<n>` | No | List messages (paginated, 1-based) |
| POST | `/messages` | Yes (JWT) | Create a message (`{ "text": "…" }`) |
| PUT | `/messages/{id}` | Yes (JWT) | Update a message (`{ "text": "…" }`) |
| DELETE | `/messages/{id}` | Yes (JWT) | Delete a message |
| GET | `/openapi/v1.json` | No | OpenAPI spec |

The `Messages` feature is the template's worked example — it exercises `Id<T>`, an
`EntityStore<Message>`, `Page<T>` pagination, the `IHandler` + `.WithValidation(...)` pattern, and
`IAsyncOnStartup` wiring (a welcome message is seeded on first run).

## Build & Test

```bash
# Restore and build
dotnet restore
dotnet build

# Unit tests only (default)
dotnet test

# Integration tests only
dotnet test -p:RunIntegrationTests=true -p:RunUnitTests=false

# All tests
dotnet test -p:RunIntegrationTests=true
```

Integration tests run the real service via [Testcontainers](https://dotnet.testcontainers.org/): `AppFixture` builds the `Dockerfile` image, starts a container (waiting on `/health`), and exercises it through the generated Refit client — so the tests cover the AOT-published binary end to end, including JSON serialization. The fixture lifecycle is managed via TUnit's `IAsyncInitializer` + `ClassDataSource` pattern.

To add a dependency (e.g. PostgreSQL):

1. Add the Testcontainers module to `Directory.Packages.props` and the integration test project:
   ```xml
   <!-- Directory.Packages.props -->
   <PackageVersion Include="Testcontainers.PostgreSql" Version="4.11.0" />

   <!-- Integration test .csproj -->
   <PackageReference Include="Testcontainers.PostgreSql" />
   ```

2. In `AppFixture.InitializeAsync`, start the dependency container and pass its connection string to the
   app container as an environment variable (config keys map to `__`-delimited env vars):
   ```csharp
   private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().Build();

   // in InitializeAsync, before building the app container:
   await _pg.StartAsync();
   // ...
   .WithEnvironment("ConnectionStrings__Default", _pg.GetConnectionString())
   ```

Test execution is controlled by MSBuild properties:
- `RunUnitTests=false` skips unit tests
- `RunIntegrationTests=true` enables integration tests (disabled by default)

## Running

```bash
# Local
dotnet run --project src/Olve.Template.Api

# Kubernetes
helm install olve-template-api helm/
```

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

### C# ([Refitter](https://refitter.github.io/))

The `clients/Olve.Template.Api.Client/` project generates a typed [Refit](https://github.com/reactiveui/refit) client from `api.json` at build time — just build the solution, no manual codegen step needed. A build target runs the [Refitter](https://refitter.github.io/) CLI (restored via `dotnet tool restore`) to emit the interface as `Generated/Output.cs`, which Refit's own source generator then turns into the client implementation. (Refit 12's `RestService.For<T>` requires that generated implementation, and Refit's generator can only consume a real source file — not the output of Refitter's source generator — hence the CLI step rather than `Refitter.SourceGenerator`.)

### TypeScript ([Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview))

```bash
dotnet tool restore
dotnet kiota generate -l typescript -d api.json -c OlveTemplateApiClient -o clients/olve-template-api-client-ts/src -n OlveTemplateApi
```

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

This template does not include a CI workflow — the actual workflow should live in the service's deployment repo. Below are examples to copy and adapt.

### Example: PR workflow

```yaml
# .github/workflows/pr.yml
name: PR

on:
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: 10.0.100

jobs:
  build-and-test:
    name: Build and test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-restore --no-build -c Release
      - run: dotnet test --no-restore --no-build -c Release -p:RunIntegrationTests=true -p:RunUnitTests=false
```

### Example: Push to main workflow

```yaml
# .github/workflows/push-main.yml
name: Push to main

on:
  push:
    branches: [main]

env:
  DOTNET_VERSION: 10.0.100

jobs:
  build-and-test:
    name: Build and test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-restore --no-build -c Release
      - run: dotnet test --no-restore --no-build -c Release -p:RunIntegrationTests=true -p:RunUnitTests=false

  version:
    name: Compute version
    needs: build-and-test
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.version.outputs.version }}
      artifact-name: ${{ steps.version.outputs.artifact-name }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Compute version
        id: version
        run: |
          dotnet run tools/version.cs -- --ci --run-number ${{ github.run_number }} \
            | tee -a "$GITHUB_OUTPUT"
```

## References

- [Olve.MinimalApi](https://olivervea.github.io/Olve.Utilities/src/Olve.MinimalApi/README.html) — Minimal API extensions for result mapping, validation, and JSON conversion
- [Olve.Results](https://olivervea.github.io/Olve.Utilities/src/Olve.Results/README.html) — Functional result types for non-throwing error handling
- [Olve.Validation](https://olivervea.github.io/Olve.Utilities/src/Olve.Validation/README.html) — Fluent input validation built on Olve.Results
- [Olve.Utilities](https://olivervea.github.io/Olve.Utilities/src/Olve.Utilities/README.html) — Meta-package bundling utility libraries including identifiers, collections, and graph types
- [TUnit](https://tunit.dev/docs/intro) — Test framework (not xUnit/NUnit). Uses `await Assert.That(...)` fluent syntax
- [Rocks](https://raw.githubusercontent.com/JasonBock/Rocks/refs/heads/main/docs/Overview.md) — Source-generated mocking library for AOT-compatible test doubles
- [Refitter](https://refitter.github.io/articles/refitter-file-format.html) — Source generator for typed C# HTTP clients from OpenAPI specs via Refit
- [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) — Microsoft's OpenAPI client generator for TypeScript (and other languages)
