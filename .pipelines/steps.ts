import {
  checkSpa,
  HelmRelease,
  implement,
  serviceUrl,
  Source,
  step,
} from '@olivervea/pipelines';
import { API_TEST_CLIENT_SECRET, BETA_CLIENT } from './config';
import { clientCredentialsToken } from './lib/auth';
import { smokeTest, waitHealthy } from './lib/smoke';
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
      'Runs the whole API test suite against beta in-cluster, with a machine token, and checks the SPA is served.',
    image: DOTNET_SDK_IMAGE,
    input: { source: Source, release: HelmRelease },
    secrets: [API_TEST_CLIENT_SECRET],
    config: [BETA_CLIENT],
    timeoutSeconds: 900,
  }),
  async ({ source, release }, ctx) => {
    const url = release.url ?? serviceUrl(release);
    const tree = await ctx.files.fetch(source.tree);
    await installBun(ctx);
    await waitHealthy(ctx, url);
    await checkSpa(ctx, url);
    const token = await clientCredentialsToken(
      ctx,
      ctx.config.betaClient,
      ctx.secrets.API_TEST_CLIENT_SECRET,
    );
    await ctx.exec(['dotnet', 'test', '--project', API_TESTS], {
      cwd: tree,
      env: { ...DOTNET_ENV, API_BASE_URL: url, API_TOKEN: token },
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
