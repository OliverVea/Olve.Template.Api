# Olve.Template.Api

A .NET 10 minimal API service template.

## Project Structure

```
├── src/Olve.Template.Api/                  # API application
│   ├── Program.cs                          # Endpoints, configuration, logging
│   ├── Configuration/                      # Auth, telemetry, JSON, host config
│   ├── Message/                            # Message service (example feature)
│   ├── Health/                             # Health check endpoints
│   ├── Dockerfile                          # Multi-stage build (AOT, chiseled)
│   └── appsettings.json                    # Default configuration
├── test/Olve.Template.Api.UnitTests/       # Unit tests (TUnit)
├── test/Olve.Template.Api.IntegrationTests/# Integration tests (TUnit + WebApplicationFactory)
├── clients/Olve.Template.Api.Client/       # Generated C# HTTP client (Refitter)
├── clients/olve-template-api-client-ts/    # Generated TypeScript HTTP client (Kiota)
├── tools/version.cs                        # CalVer versioning script
├── helm/                                   # Helm chart for Kubernetes
├── Directory.Build.props                   # Shared build properties
├── Directory.Packages.props                # Central package version management
└── api.json                                # OpenAPI spec (generated on build)
```

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | No | Health check, returns 200 |
| GET | `/message` | No | Retrieve stored message |
| POST | `/message?message=<text>` | Yes (JWT) | Store a message |
| GET | `/openapi/v1.json` | No | OpenAPI spec |

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

## Running

```bash
# Local
dotnet run --project src/Olve.Template.Api

# Kubernetes
helm install olve-template-api helm/
```

## Testing

```bash
# Unit tests only (default)
dotnet test

# Integration tests only
dotnet test -p:RunIntegrationTests=true -p:RunUnitTests=false

# All tests
dotnet test -p:RunIntegrationTests=true
```

Integration tests use `WebApplicationFactory` — no Docker required.

## Client Libraries

### C# ([Refitter](https://refitter.github.io/))

The `clients/Olve.Template.Api.Client/` project uses the [Refitter source generator](https://www.nuget.org/packages/Refitter.SourceGenerator) to produce a typed [Refit](https://github.com/reactiveui/refit) interface from `api.json` at build time. Just build the solution — no manual codegen step needed.

### TypeScript ([Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview))

The `clients/olve-template-api-client-ts/` directory contains a Kiota-generated TypeScript client. To regenerate after API changes:

```bash
dotnet tool restore
dotnet kiota generate -l typescript -d api.json -c OlveTemplateApiClient -o clients/olve-template-api-client-ts/src -n OlveTemplateApi
```

## References

- [Olve.MinimalApi](https://olivervea.github.io/Olve.Utilities/src/Olve.MinimalApi/README.html) — Minimal API extensions for result mapping, validation, and JSON conversion
- [Olve.Results](https://olivervea.github.io/Olve.Utilities/src/Olve.Results/README.html) — Functional result types for non-throwing error handling
- [Olve.Validation](https://olivervea.github.io/Olve.Utilities/src/Olve.Validation/README.html) — Fluent input validation built on Olve.Results
- [Olve.Utilities](https://olivervea.github.io/Olve.Utilities/src/Olve.Utilities/README.html) — Meta-package bundling utility libraries including identifiers, collections, and graph types
