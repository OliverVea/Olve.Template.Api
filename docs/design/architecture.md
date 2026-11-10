# Olve.Template.Api — Architecture

> Technical decisions, stated or agreed by the owner. The next milestones are in [roadmap.md](roadmap.md).
> The repo setup comes from Olve.Template.Api; how to build it is in [CONTRIBUTING.md](../../CONTRIBUTING.md).

## Stack

| Area | Decision |
| --- | --- |
| Sign-in | A bundled Dex per service (GitHub upstream, under `/dex` on the service's host), as in Olve.Pipelines: the same `auth` config shape (`none`, `dex`, `authentik`), an allow-list, and hashed machine tokens in place of client credentials. |
