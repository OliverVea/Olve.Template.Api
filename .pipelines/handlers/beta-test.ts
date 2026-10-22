import { implement } from "@olve/pipelines";
import { betaTest } from "../pipeline";
import { installMise, miseRun } from "./lib/mise";
import { checkSpa, serviceUrl, waitHealthy } from "./lib/smoke";

// The suite writes messages, so it runs against beta only. Tests that need a token skip:
// beta validates Authentik tokens, not a test signing key.
export default implement(betaTest, async ({ source, release }, ctx) => {
  const url = serviceUrl(release);
  const tree = await ctx.files.fetch(source.tree);
  await installMise(ctx, tree);
  await waitHealthy(ctx, url);
  await checkSpa(ctx, url);
  await miseRun(ctx, tree, "api:remote", { API_BASE_URL: url });
  ctx.log("API tests passed against beta", { url });
});
