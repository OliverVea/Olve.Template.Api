# TODO

- [ ] Add OpenAPI spec generation from the API
- [ ] Set up structured logging
- [ ] Set up configuration (appsettings, environment-based config)
- [ ] Turn the project into a `dotnet new` solution template once the foundation is solid
- [ ] Fix AOT analysis warnings in `Olve.MinimalApi` (IL3053) — currently suppressed in `Olve.Short.csproj`. Once the package is AOT-compatible, remove the `<NoWarn>IL3053</NoWarn>` suppression.
