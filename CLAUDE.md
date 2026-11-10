# CLAUDE.md

Olve.Template.Api: a spec-first .NET API service, from the `olve-api` template.
<!--#if (false) -->
This repo is that template: `dotnet new olve-api` copies it, so every rule here also steers the services
generated from it. Text only the template repo needs sits between `#if (false)` markers.
<!--#endif -->

See [CONTRIBUTING.md](CONTRIBUTING.md) for the layout, build, configuration, contract, tests and deploy.
The design is in [docs/design/](docs/design/): [architecture.md](docs/design/architecture.md) (tech
decisions), [roadmap.md](docs/design/roadmap.md) (what's next).

## How to work here

- **Ask more, assume less.** Ask before deciding design, scope or cost for the owner, one decision at a
  time with a recommendation first. A short "sure" answers only the question it replies to; a correction
  changes that one thing. Mechanical work inside agreed scope just gets done.
- **Record only what the owner stated or agreed** in `docs/design/`. Proposals stay in chat until agreed.
  Record each decision right away and commit it. A new answer that contradicts a row replaces that row.
- **The contract holds only what's built**, and a contract change is shown to the owner before the code
  behind it. UI work starts with a mock the owner approves: load the `ovea-ui-mocks` skill.
- **Never make a check pass by changing what it checks against**: test expectations, recorded fixtures,
  the contract's existing responses. If one is wrong, say so and ask.
- **After three failed attempts at the same problem, stop** and report what was tried.
- **Secrets stay out of the transcript**: never print a credential file, a Kubernetes Secret (`-o yaml`,
  `describe`) or a secret value. List keys, compare values by sha256, pipe values straight into the
  consumer (`pl secret set … --from-file`).
- **Commits:** one commit per change. Fix-ups, review answers and the docs for a change are squashed into
  it before pushing; pushed history is never rewritten.
- **Docs describe the current state**: no history, no "no longer"; the history is the commit messages.
- **Comments are rare**: one short line for a non-obvious why or a gotcha, nothing that restates the code.
- **Steering stays current:** a change to how an agent works here updates this file, `docs/STANDARDS.md`
  or the skill it touches in the same commit.
- Follow [`docs/STANDARDS.md`](docs/STANDARDS.md) (MUST/SHOULD rules for API behaviour, code and tests).

## Commands

```bash
dotnet build                                            # Build (compiles src/spec/ first; needs bun)
dotnet test                                             # Unit + API tests (API tests in-process)
dotnet run --project src/backend/Olve.Template.Api      # Run on localhost:5000
cd src/frontend && bun run dev                          # The SPA on Vite, proxying /api to localhost:5000
cd src/spec && bun run build                            # The contract alone → artifacts/
tools/check.sh [--full|--fix|--changed]                 # What the pipeline checks; --fix formats first
tools/api-image.sh                                      # API tests against the Docker image (the AOT check)
```

A `PreToolUse` hook runs `tools/check.sh --hook` before every `git push`: the checks for what changed
against the upstream branch, blocking the push on a failure.

## Deploy

`.pipelines/pipeline.ts` on Olve.Pipelines (pipeline "Olve.Template.Api"): every push to `main` builds,
runs `tools/check.sh --full`, deploys beta, runs the API tests there, then deploys prod and smoke-tests it
read-only. The chart is ClusterIP-only; routes are registered in `Olve.Homelab`. Load the
`ovea-olve-pipelines` skill before changing `.pipelines/` or running any `pl` command. Start `pl wait` in the
background and keep working; when reporting a push, name the run that carries it and its state.

## Conventions

- .NET 10 Native AOT, C# with file-scoped namespaces, nullable, warnings as errors.
- Package versions only in `Directory.Packages.props`.
- Inclusive vocabulary in code, docs and names: allowlist/denylist, `main` as the default branch,
  primary/replica.
- Local config via `dotnet user-secrets`, not appsettings files.
- The API is the TypeSpec contract in `src/spec/`: the build compiles it into `artifacts/` (gitignored: the
  C# surface, the OpenAPI document, the Hey API client). Nothing generated is committed. An API change is a
  change to the contract and the `[OperationHandler]` class behind the operation.
- Olve.Results everywhere; expected failures are declared contract responses, never exceptions.
- API tests are raw HTTP behaviour tests that pass against any target (`ApiTarget`); no C# client.
- Telemetry needs no credentials: JSON logs on stdout reach OpenObserve through the node collector. OTLP
  export is plain OTLP (endpoint, protocol, headers from a Secret), off by default.

## References

- [Olve.* packages](https://olivervea.github.io/Olve.Utilities/): Olve.Results, Olve.Validation, Olve.TypeSpec
  (the generated API surface; skill `olve-typespec`), Olve.Utilities, Olve.Results.TUnit
- [Olve.Pipelines](https://github.com/OliverVea/Olve.Pipelines): the deploy model for `.pipelines/` (skill
  `ovea-olve-pipelines`, instance `pipelines-private.ovea.pro`)
- [Olve.Homelab](https://github.com/OliverVea/Olve.Homelab): the edge chart that owns every Ingress
- [TUnit](https://tunit.dev/docs/intro) (`await Assert.That(...)`, not xUnit/NUnit),
  [Rocks](https://github.com/JasonBock/Rocks) (source-generated mocks)
- [TypeSpec](https://typespec.io), [Hey API](https://heyapi.dev), [bun](https://bun.sh)
