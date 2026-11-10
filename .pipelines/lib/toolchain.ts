import type { Ctx } from '@olivervea/pipelines';

// Leftover MSBuild nodes and the compiler server would hold the step's output pipe open after dotnet exits.
export const DOTNET_ENV = {
  MSBUILDDISABLENODEREUSE: '1',
  UseSharedCompilation: 'false',
  DOTNET_CLI_TELEMETRY_OPTOUT: '1',
};

/** Bun (the contract and the frontend) in the .NET SDK image, pinned to the version the repo's lockfiles were made with. */
export async function installBun(ctx: Ctx) {
  await ctx.sh('apt-get update -qq && apt-get install -y -qq unzip >/dev/null');
  await ctx.sh(
    'curl -fsSL https://bun.sh/install | BUN_INSTALL=/usr/local bash -s bun-v1.4.3',
  );
}
