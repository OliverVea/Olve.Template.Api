import { implement } from "@olve/pipelines";
import { test } from "../pipeline";

// Leftover MSBuild nodes and the compiler server would hold the step's output pipe open after dotnet exits.
const ENV = { MSBUILDDISABLENODEREUSE: "1", UseSharedCompilation: "false" };

export default implement(test, async ({ source }, ctx) => {
  const tree = await ctx.files.fetch(source.tree);

  await ctx.sh("curl -fsSL https://mise.run | MISE_INSTALL_PATH=/usr/local/bin/mise sh");
  await ctx.exec(["mise", "trust", "--yes"], { cwd: tree });
  await ctx.exec(["mise", "install"], { cwd: tree });
  await ctx.exec(["mise", "run", "ci"], { cwd: tree, env: ENV });

  ctx.log("checks passed");
});
