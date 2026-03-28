# Olve.Template.Api

A .NET 10 minimal API service template.

## Project Structure

```
├── src/Olve.Template.Api/                      # API application
│   ├── Program.cs                       # Endpoints, configuration, logging
│   ├── Echo/EchoService.cs              # Echo service
│   ├── Dockerfile                       # Multi-stage build (aspnet chiseled)
│   └── appsettings.json                 # Default configuration
├── test/Olve.Template.Api.UnitTests/           # Unit tests (TUnit)
├── test/Olve.Template.Api.IntegrationTests/    # Integration tests (TUnit + WebApplicationFactory)
├── clients/Olve.Template.Api.Client/           # Generated C# HTTP client (Refitter)
├── clients/olve-template-api-client-ts/        # Generated TypeScript HTTP client (Kiota)
├── tools/version.cs                     # CalVer versioning script
├── helm/olve-template-api/                     # Helm chart for Kubernetes
├── api.json                             # OpenAPI spec (generated on build)
└── compose.yaml                         # Docker Compose for local development
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check, returns 200 |
| GET | `/echo?message=` | Returns the message, 400 if empty |
| GET | `/openapi/v1.json` | OpenAPI spec |

## Configuration

Sources in priority order (highest wins):

1. CLI args (`--Port 9090`)
2. `appsettings.local.json` (gitignored)
3. Environment variables
4. `appsettings.json`

| Key | Default | Description |
|-----|---------|-------------|
| `Host` | `localhost` | Listen address |
| `Port` | `5000` | Listen port |
| `Logging:LogLevel:Default` | `Information` | Default log level |

## Running

```bash
# Local
dotnet run --project src/Olve.Template.Api

# Docker
docker compose up --build

# Kubernetes
helm install olve-template-api helm/olve-template-api
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
