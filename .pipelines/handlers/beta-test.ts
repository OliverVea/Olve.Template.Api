import { implement } from "@olve/pipelines";
import { betaTest } from "../pipeline";
import { betaTestToken } from "./lib/auth";
import { installMise, miseRun } from "./lib/mise";
import { checkSpa, serviceUrl, waitHealthy } from "./lib/smoke";

// The suite writes (and deletes) messages, so it runs against beta only.
export default implement(betaTest, async ({ source, release }, ctx) => {
  const url = serviceUrl(release);
  const tree = await ctx.files.fetch(source.tree);
  await installMise(ctx, tree);
  await waitHealthy(ctx, url);
  await checkSpa(ctx, url);
  const token = await betaTestToken(ctx, ctx.secrets.API_TEST_CLIENT_SECRET);
  await miseRun(ctx, tree, "api:remote", { API_BASE_URL: url, API_TOKEN: token });
  ctx.log("API tests passed against beta", { url });
});
