# Olve.Template.Api

<!-- What this service is and who it's for, in a sentence or two. -->

The design lives in [docs/design/](docs/design/); building it is in [CONTRIBUTING.md](CONTRIBUTING.md).

## Use

On the tailnet: [olve-template-api-private.ovea.pro](https://olve-template-api-private.ovea.pro) (prod)
and [olve-template-api-beta.ovea.pro](https://olve-template-api-beta.ovea.pro) (beta), once the routes
are registered in `Olve.Homelab`. Every push to `main` deploys beta, then prod, through the
Olve.Pipelines pipeline "Olve.Template.Api".

Started from the `olve-api` `dotnet new` template (OliverVea's API template on GitHub and NuGet).
