# TODO

## Template Infrastructure

- [x] Extract `EchoService` to `src/Olve.Short/Echo/EchoService.cs`, refactor `/echo` endpoint to a lambda
- [x] Split tests into `test/Olve.Short.UnitTests/` (unit tests for EchoService) and `test/Olve.Short.IntegrationTests/` (existing WebApplicationFactory tests)
- [ ] Add `tools/version.cs` — CalVer versioning (`{Year}.{Month}.{Day}.{RunNumber}+{GitHash}`), copied from Olve.Trains
- [ ] Add CI workflow (`.github/workflows/ci.yml`) — build, run UT+IT on Ubuntu, version on push to main
- [ ] Set up C# client generation with Refitter (source gen from `api.json`) in `clients/Olve.Short.Client/`
- [ ] Set up TS client generation with Kiota (dotnet tool from `api.json`) in `clients/olve-short-client-ts/`
- [ ] Add `.editorconfig`
- [ ] Add `CLAUDE.md` with build/test commands, structure, conventions
- [ ] Update `README.md` and `.slnx` to reflect new structure

## Future

- [ ] Create monorepo `Olve.Services` and migrate this template into `services/Olve.Short/`
- [ ] Rename this repo to `Olve.BackendServiceTemplate` or similar
- [ ] Turn into a `dotnet new` solution template once the foundation is solid
- [ ] Re-enable AOT and trimming once Olve packages support it (OliverVea/Olve.Utilities#59, OliverVea/Olve.Utilities#60)
