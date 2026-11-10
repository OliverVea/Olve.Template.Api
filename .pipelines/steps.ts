import {
  checkSpa,
  HelmRelease,
  implement,
  serviceUrl,
  Source,
  step,
} from '@olivervea/pipelines';
import { AUTH, BETA_TOKEN, TARGET } from './config';
//#if (false)
import { NUGET_API_KEY } from './config';
//#endif
import { checkDex, smokeTest, waitHealthy } from './lib/smoke';
import { DOTNET_ENV, DOTNET_SDK_IMAGE, installBun } from './lib/toolchain';

export const test = implement(
  step({
    name: 'test',
    description:
      "Runs `tools/check.sh --full`: the contract and TS client, formatting, the frontend's lint and build, the pipeline's types, the .NET build, then the unit and in-process API tests and the frontend's tests.",
    image: DOTNET_SDK_IMAGE,
    input: { source: Source },
    timeoutSeconds: 1800,
  }),
  async ({ source }, ctx) => {
    const tree = await ctx.files.fetch(source.tree);
    await installBun(ctx);
    await ctx.exec(['bash', 'tools/check.sh', '--full'], { cwd: tree, env: DOTNET_ENV });
    ctx.log('checks passed');
  },
);

const API_TESTS = 'src/backend/Olve.Template.Api.ApiTests';

// The suite writes (and deletes) messages, so it runs against beta only.
export const betaTest = implement(
  step({
    name: 'beta-test',
    description:
      "Runs the whole API test suite against beta in-cluster, with a machine token, and checks the SPA is served and beta's Dex answers discovery with the SPA's issuer.",
    image: DOTNET_SDK_IMAGE,
    input: { source: Source, release: HelmRelease },
    secrets: [BETA_TOKEN],
    config: [AUTH, TARGET],
    timeoutSeconds: 900,
  }),
  async ({ source, release }, ctx) => {
    const url = release.url ?? serviceUrl(release);
    const tree = await ctx.files.fetch(source.tree);
    await installBun(ctx);
    await waitHealthy(ctx, url);
    await checkSpa(ctx, url);
    const auth = ctx.config.target.beta.auth ?? ctx.config.auth;
    if (auth.mode === 'dex') await checkDex(ctx, release, url, auth.bundled);
    await ctx.exec(['dotnet', 'test', '--project', API_TESTS], {
      cwd: tree,
      env: { ...DOTNET_ENV, API_BASE_URL: url, API_TOKEN: ctx.secrets.BETA_TOKEN },
    });
    ctx.log('API tests passed against beta', { url });
  },
);

export const prodTest = implement(
  step({
    name: 'prod-test',
    description:
      'Read-only smoke test of prod: health, the messages API, the auth config, the SPA and that writes need a token.',
    image: 'alpine:3.22',
    input: { release: HelmRelease },
    timeoutSeconds: 180,
  }),
  ({ release }, ctx) => smokeTest(ctx, release),
);
//#if (false)
// The template repo only: dotnet new drops everything up to #endif.
// Runs after prod-test, so only a template whose own service reached prod is published.
export const publishTemplate = implement(
  step({
    name: 'publish-template',
    description:
      'Packs the repo as the `dotnet new` template package Olve.Template.Api, versioned by the run (yyyy.M.d.<run>), and pushes it to nuget.org.',
    image: DOTNET_SDK_IMAGE,
    input: { source: Source },
    secrets: [NUGET_API_KEY],
    timeoutSeconds: 600,
  }),
  async ({ source }, ctx) => {
    const tree = await ctx.files.fetch(source.tree);
    const version = ctx.run.version;
    await ctx.exec(
      [
        'dotnet',
        'pack',
        'tools/pack/template-pack.csproj',
        `-p:PackageVersion=${version}`,
        '-o',
        'out',
      ],
      { cwd: tree, env: DOTNET_ENV },
    );
    // The key stays in the environment, out of the logged command line.
    await ctx.sh(
      'dotnet nuget push out/*.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate',
      { cwd: tree, env: DOTNET_ENV },
    );
    ctx.log('published', {
      url: `https://www.nuget.org/packages/Olve.Template.Api/${version}`,
    });
  },
);
//#endif
