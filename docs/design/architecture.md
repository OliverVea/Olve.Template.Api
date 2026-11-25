# Olve.Template.Api — Architecture

> Technical decisions, stated or agreed by the owner. The next milestones are in [roadmap.md](roadmap.md).
> The repo setup comes from the `olve-api` template; how to build it is in [CONTRIBUTING.md](../../CONTRIBUTING.md).

## Stack

| Area | Decision |
| --- | --- |
| Installable app | The template's `--pwa` option (on by default) makes the SPA a PWA: a manifest, icons, a service worker that caches the app shell and offers each new build. Web Push is not the template's: it belongs to a service, Olve.Push. |
| Template parity | The template lists its cross-cutting features, each with a check, in `template-features.json`; each service records the template version it synced with and its opt-outs (with a reason) in `.template.json`; `tools/template-parity.ts` reports the gaps and never gates. Features that flow up from a service carry their origin. |
| Sign-in | A bundled Dex per service (GitHub upstream, under `/dex` on the service's host), as in Olve.Pipelines: an allow-list, and machine tokens in place of client credentials. The pipeline's `auth` can point at an existing Dex instead (`bundled: false`). |
