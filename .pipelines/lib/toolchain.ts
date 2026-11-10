import type { Ctx } from '@olivervea/pipelines';

/** An exact SDK tag: a node's cached `10.0-noble` can be months old, and its AOT compiler then fails restore on a known vulnerability (NU1903). Same as the Dockerfile's. */
export const DOTNET_SDK_IMAGE = 'mcr.microsoft.com/dotnet/sdk:10.0.403-noble';

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
