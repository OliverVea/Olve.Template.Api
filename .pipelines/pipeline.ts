// Every push goes beta, then prod. config.yaml beside it is the paused v1 deployer's rollback path.
import { pipeline, secret, step, steps, HelmRelease, Source } from "@olve/pipelines";
import * as M from "./models";

export const GITHUB_TOKEN = secret("GITHUB_TOKEN", "GitHub read token used to fetch the repo tarball.");
export const SSH_PRIVATE_KEY = secret("SSH_PRIVATE_KEY", "SSH key for importing the image and running helm on the homelab host.");

export const source = steps.gitHubSource({
  name: "source",
  description: "Fetches the pushed commit from GitHub.",
  token: GITHUB_TOKEN,
});

export const MIRROR = "registry.ovea.pro/olve-pipelines-v2/mirror";

export const mirrorBaseImages = step({
  name: "mirror-base-images",
  description: "Copies the Dockerfile's base images into registry.ovea.pro; kaniko under gVisor stalls pulling from mcr.microsoft.com.",
  image: "gcr.io/go-containerregistry/crane:debug",
  timeoutSeconds: 900,
});

export const build = steps.imageBuild({
  name: "build",
  description: "Builds the app image (API + SPA) with kaniko.",
  repository: "olve-template-api",
  registryMap: { "mcr.microsoft.com": `${MIRROR}/mcr.microsoft.com`, "index.docker.io": `${MIRROR}/docker.io` },
  timeoutSeconds: 1200,
});

export const test = step({
  name: "test",
  description: "Runs `mise run ci`: backend unit and API tests, the TS client drift check, frontend lint, tests and build.",
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
  set: { "slo.enabled": "false" },
  healthUrl: "http://olve-template-api.apps-beta.svc.cluster.local/health",
  timeoutSeconds: 900,
});

export const betaTest = step({
  name: "beta-test",
  description: "Smoke-tests beta in-cluster: health, the messages API, the auth config, the SPA and that writes need a token.",
  image: "alpine:3.22",
  input: { release: HelmRelease },
  timeoutSeconds: 180,
});

export const deployProd = steps.helmDeployOverSsh({
  name: "deploy-prod",
  description: "Deploys the image to apps with Helm and checks its health.",
  host: "oliver@bulwark-m2",
  sshKey: SSH_PRIVATE_KEY,
  release: "olve-template-api",
  namespace: "apps",
  chart: "helm",
  set: { "slo.enabled": "false" },
  healthUrl: "http://olve-template-api.apps.svc.cluster.local/health",
  timeoutSeconds: 900,
});

export const prodTest = step({
  name: "prod-test",
  description: "Runs the beta smoke test against prod.",
  image: "alpine:3.22",
  input: { release: HelmRelease },
  timeoutSeconds: 180,
});

export default pipeline({
  name: "Olve.Template.Api",
  description: "Build with kaniko and run `mise run ci`, deploy to apps-beta with Helm and smoke-test it, then deploy the same image to apps and smoke-test that. deployTo=beta stops after beta.",
  input: M.DeployRequest,
})
  .onPush(e => ({ repo: e.repository.full_name, sha: e.after, deployTo: "prod" }))
  .stage([source, from => ({ repo: from.input.repo, sha: from.input.sha })], mirrorBaseImages)
  .stage(build, test)
  .stage(deployBeta)
  .stage(betaTest)
  .when({ deployTo: "prod" }, p => p
    .stage(deployProd)
    .stage([prodTest, from => ({ release: from["deploy-prod"] })]));
