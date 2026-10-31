import { checkSpa, HelmRelease, implement, serviceUrl, Source, step } from "@olivervea/pipelines";
import { API_TEST_CLIENT_SECRET, BETA_CLIENT } from "./config";
import { clientCredentialsToken } from "./lib/auth";
import { installMise, miseRun } from "./lib/mise";
import { smokeTest, waitHealthy } from "./lib/smoke";

export const test = implement(step({
  name: "test",
  description: "Runs `mise run ci`: backend unit and API tests, the contract and TS client, frontend lint, tests and build.",
  image: "mcr.microsoft.com/dotnet/sdk:10.0-noble",
  input: { source: Source },
  timeoutSeconds: 1800,
}), async ({ source }, ctx) => {
  const tree = await ctx.files.fetch(source.tree);
  await installMise(ctx, tree);
  await miseRun(ctx, tree, "ci");
  ctx.log("checks passed");
});

// The suite writes (and deletes) messages, so it runs against beta only.
export const betaTest = implement(step({
  name: "beta-test",
  description: "Runs the whole API test suite (`mise run api:remote`) against beta in-cluster, with a machine token, and checks the SPA is served.",
  image: "mcr.microsoft.com/dotnet/sdk:10.0-noble",
  input: { source: Source, release: HelmRelease },
  secrets: [API_TEST_CLIENT_SECRET],
  config: [BETA_CLIENT],
  timeoutSeconds: 900,
}), async ({ source, release }, ctx) => {
  const url = release.url ?? serviceUrl(release);
  const tree = await ctx.files.fetch(source.tree);
  await installMise(ctx, tree);
  await waitHealthy(ctx, url);
  await checkSpa(ctx, url);
  const token = await clientCredentialsToken(ctx, ctx.config.betaClient, ctx.secrets.API_TEST_CLIENT_SECRET);
  await miseRun(ctx, tree, "api:remote", { API_BASE_URL: url, API_TOKEN: token });
  ctx.log("API tests passed against beta", { url });
});

export const prodTest = implement(step({
  name: "prod-test",
  description: "Read-only smoke test of prod: health, the messages API, the auth config, the SPA and that writes need a token.",
  image: "alpine:3.22",
  input: { release: HelmRelease },
  timeoutSeconds: 180,
}), ({ release }, ctx) => smokeTest(ctx, release));
