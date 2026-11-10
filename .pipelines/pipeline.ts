// Every push goes beta, then prod. Where they are is the instance's config (config.ts).
import { pipeline, steps, type ConfigRef } from '@olivervea/pipelines';
import { AUTH, GITHUB_TOKEN, IMAGE, SSH_PRIVATE_KEY, TARGET } from './config';
import * as M from './models';
import { betaTest, prodTest, test } from './steps';
//#if (false)
import { publishTemplate } from './steps';
//#endif

// The chart's `auth` value: the pipeline's `auth`, replaced by the target's own when it sets one.
const deploy = (target: { auth: ConfigRef }) =>
  ({
    chart: 'src/deploy/helm',
    inlineValues: [{ auth: AUTH }, { auth: target.auth }],
    healthPath: '/health',
    timeoutSeconds: 900,
  }) as const;
const describeDeploy = (stage: string) =>
  `Deploys this run's image to the ${stage} target with Helm and checks its health.`;

const source = steps.gitHubSource({
  name: 'source',
  description: 'Fetches the pushed commit from GitHub.',
  token: GITHUB_TOKEN,
});

const build = steps.imageBuild({
  name: 'build',
  description: 'Builds the app image (API + SPA) with kaniko and pushes it.',
  repository: IMAGE.repository,
  push: true,
  registryMap: IMAGE.registryMap,
  timeoutSeconds: 1200,
});

let app = pipeline({
  name: 'Olve.Template.Api',
  description:
    'Build with kaniko and run `tools/check.sh --full`, deploy to beta with Helm and run the API test suite against it, then deploy the same image to prod and smoke-test that read-only. deployTo=beta stops after beta.',
  input: M.DeployRequest,
})
  .onPush((e) => ({ repo: e.repository.full_name, sha: e.after, deployTo: 'prod' }))
  .stage([source, (from) => ({ repo: from.input.repo, sha: from.input.sha })])
  .stage(build, test)
  .select(TARGET.beta, {
    ssh: (p, beta) =>
      p.stage(
        steps.helmDeployOverSsh({
          ...deploy(beta),
          name: 'deploy-beta',
          description: describeDeploy('beta'),
          target: beta,
          sshKey: SSH_PRIVATE_KEY,
        }),
      ),
    inCluster: (p, beta) =>
      p.stage(
        steps.helmDeployInCluster({
          ...deploy(beta),
          name: 'deploy-beta',
          description: describeDeploy('beta'),
          target: beta,
        }),
      ),
  })
  .stage(betaTest)
  .when({ deployTo: 'prod' }, (p) =>
    p
      .select(TARGET.prod, {
        ssh: (q, prod) =>
          q.stage(
            steps.helmDeployOverSsh({
              ...deploy(prod),
              name: 'deploy-prod',
              description: describeDeploy('prod'),
              target: prod,
              sshKey: SSH_PRIVATE_KEY,
            }),
          ),
        inCluster: (q, prod) =>
          q.stage(
            steps.helmDeployInCluster({
              ...deploy(prod),
              name: 'deploy-prod',
              description: describeDeploy('prod'),
              target: prod,
            }),
          ),
      })
      .stage([prodTest, (from) => ({ release: from['deploy-prod'] })]),
  );
//#if (false)
// The template repo only: dotnet new drops everything up to #endif. Publishes the template package.
app = app.when({ deployTo: 'prod' }, (p) => p.stage(publishTemplate));
//#endif

export default app;
