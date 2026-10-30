// Every push goes beta, then prod. config.yaml beside it is the paused v1 deployer's rollback path.
import { pipeline, secret, step, steps, HelmRelease, Source } from "@olivervea/pipelines";
import * as M from "./models";

export const GITHUB_TOKEN = secret("GITHUB_TOKEN", "GitHub read token used to fetch the repo tarball.");
export const SSH_PRIVATE_KEY = secret("SSH_PRIVATE_KEY", "SSH key for importing the image and running helm on the homelab host.");
export const API_TEST_CLIENT_SECRET = secret("API_TEST_CLIENT_SECRET", "Client secret of beta Authentik's olve-template-api-tests machine client.");

// The chart's SLO needs the sloth CRD, which the cluster doesn't have.
const CHART_SET = { "slo.enabled": "false" };

export const source = steps.gitHubSource({
  name: "source",
  description: "Fetches the pushed commit from GitHub.",
  token: GITHUB_TOKEN,
});

export const build = steps.imageBuild({
  name: "build",
  description: "Builds the app image (API + SPA) with kaniko.",
  repository: "olve-template-api",
  timeoutSeconds: 1200,
});

export const test = step({
  name: "test",
  description: "Runs `mise run ci`: backend unit and API tests, the contract and TS client, frontend lint, tests and build.",
  image: "mcr.microsoft.com/dotnet/sdk:10.0-noble",
  input: { source: Source },
  timeoutSeconds: 1800,
});

export const deployBeta = steps.helmDeployOverSsh({
  name: "deploy-beta",
  description: "Deploys the image to apps-beta with Helm and checks its health.",
  host: "oliver@bulwark-m2",
  sshKey: SSH_PRIVATE_KEY,
  release: "olve-template-api",
  namespace: "apps-beta",
  chart: "helm",
  values: ["values-beta.yaml"],
  set: CHART_SET,
  healthUrl: "http://olve-template-api.apps-beta.svc.cluster.local/health",
  timeoutSeconds: 900,
});

export const betaTest = step({
  name: "beta-test",
  description: "Runs the whole API test suite (`mise run api:remote`) against beta in-cluster, with a token from beta Authentik, and checks the SPA is served.",
  image: "mcr.microsoft.com/dotnet/sdk:10.0-noble",
  input: { source: Source, release: HelmRelease },
  secrets: [API_TEST_CLIENT_SECRET],
  timeoutSeconds: 900,
});

export const deployProd = steps.helmDeployOverSsh({
  name: "deploy-prod",
  description: "Deploys the image to apps with Helm and checks its health.",
  host: "oliver@bulwark-m2",
  sshKey: SSH_PRIVATE_KEY,
  release: "olve-template-api",
  namespace: "apps",
  chart: "helm",
  set: CHART_SET,
  healthUrl: "http://olve-template-api.apps.svc.cluster.local/health",
  timeoutSeconds: 900,
});

export const prodTest = step({
  name: "prod-test",
  description: "Read-only smoke test of prod: health, the messages API, the auth config, the SPA and that writes need a token.",
  image: "alpine:3.22",
  input: { release: HelmRelease },
  timeoutSeconds: 180,
});

export default pipeline({
  name: "Olve.Template.Api",
  description: "Build with kaniko and run `mise run ci`, deploy to apps-beta with Helm and run the API test suite against it, then deploy the same image to apps and smoke-test that read-only. deployTo=beta stops after beta.",
  input: M.DeployRequest,
})
  .onPush(e => ({ repo: e.repository.full_name, sha: e.after, deployTo: "prod" }))
  .stage([source, from => ({ repo: from.input.repo, sha: from.input.sha })])
  .stage(build, test)
  .stage(deployBeta)
  .stage(betaTest)
  .when({ deployTo: "prod" }, p => p
    .stage(deployProd)
    .stage([prodTest, from => ({ release: from["deploy-prod"] })]));
