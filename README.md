# Olve.Template.Api

A `dotnet new` template for a .NET 10 spec-first API service: a TypeSpec contract that generates the
server's C# surface, the OpenAPI document and a TypeScript client; JWT/OIDC auth; a vanilla Web
Components SPA; a Helm chart; and an Olve.Pipelines pipeline that deploys beta, then prod.

```bash
dotnet new install .
dotnet new olve-api -n "MyCompany.MyService"
```

The design lives in [docs/design/](docs/design/); building it is in [CONTRIBUTING.md](CONTRIBUTING.md).

## What a new service starts with

- The contract in `src/spec/` and a worked example, `Messages`: `Id<T>`, an `EntityStore<Message>`,
  page pagination, one `[OperationHandler]` class per operation, a welcome message seeded on startup.
- Native AOT, a chiseled image, JSON logs on stdout; OTLP export off until configured.
- API tests over raw HTTP that run in-process, against the Docker image or against beta.
- `tools/check.sh`: the pipeline's checks, run locally and before every push.

## References

| Component | Role | Links |
|---|---|---|
| Olve.Utilities (Results, Validation, TypeSpec, Utilities) | Error handling, validation, the generated API surface, `Id<T>`/`EntityStore<T>` | [docs](https://olivervea.github.io/Olve.Utilities/), [GitHub](https://github.com/OliverVea/Olve.Utilities) |
| Olve.Pipelines | Builds and deploys this repo from `.pipelines/`; skill `ovea-olve-pipelines` | [GitHub](https://github.com/OliverVea/Olve.Pipelines), [instance](https://pipelines-v2-private.ovea.pro/docs) |
| Olve.Homelab | The edge chart that owns every Ingress | [GitHub](https://github.com/OliverVea/Olve.Homelab) |
| TypeSpec, Hey API | The contract language; the TypeScript client generator | [typespec.io](https://typespec.io), [heyapi.dev](https://heyapi.dev) |
| TUnit, Rocks | Tests (`await Assert.That(...)`); source-generated, AOT-safe mocks | [tunit.dev](https://tunit.dev/docs/intro), [Rocks](https://github.com/JasonBock/Rocks) |
| bun | The contract's and the frontend's JS tooling | [bun.sh](https://bun.sh) |
