# Olve.Short

A URL shortener service built with .NET 10 minimal APIs.

## Project Structure

```
├── src/Olve.Short/          # API application
│   ├── Program.cs           # Endpoints, configuration, logging
│   ├── Dockerfile           # Multi-stage build (aspnet chiseled)
│   └── appsettings.json     # Default configuration
├── test/Olve.Short.Tests/   # Integration tests (TUnit + WebApplicationFactory)
├── helm/olve-short/         # Helm chart for Kubernetes
├── api.json                 # OpenAPI spec (generated on build)
└── compose.yaml             # Docker Compose for local development
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
dotnet run --project src/Olve.Short

# Docker
docker compose up --build

# Kubernetes
helm install olve-short helm/olve-short
```

## Testing

```bash
dotnet test
```

Integration tests use `WebApplicationFactory` — no Docker required.

## References

- [Olve.MinimalApi](https://olivervea.github.io/Olve.Utilities/src/Olve.MinimalApi/README.html) — Minimal API extensions for result mapping, validation, and JSON conversion
- [Olve.Results](https://olivervea.github.io/Olve.Utilities/src/Olve.Results/README.html) — Functional result types for non-throwing error handling
- [Olve.Validation](https://olivervea.github.io/Olve.Utilities/src/Olve.Validation/README.html) — Fluent input validation built on Olve.Results
- [Olve.Utilities](https://olivervea.github.io/Olve.Utilities/src/Olve.Utilities/README.html) — Meta-package bundling utility libraries including identifiers, collections, and graph types
