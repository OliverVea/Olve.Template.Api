# Olve.Template.Api — Architecture

> Technical decisions, stated or agreed by the owner. The next milestones are in [roadmap.md](roadmap.md).
> The repo setup comes from the `olve-api` template; how to build it is in [CONTRIBUTING.md](../../CONTRIBUTING.md).

## Stack

| Area | Decision |
| --- | --- |
| Sign-in | A bundled Dex per service (GitHub upstream, under `/dex` on the service's host), as in Olve.Pipelines: an allow-list, and machine tokens in place of client credentials. The pipeline's `auth` can point at an existing Dex instead (`bundled: false`). |
