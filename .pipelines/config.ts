import { config, helmTarget, secret, z } from "@olivervea/pipelines";

export const GITHUB_TOKEN = secret("GITHUB_TOKEN", "GitHub read token used to fetch the repo tarball.");
export const SSH_PRIVATE_KEY = secret("SSH_PRIVATE_KEY", "SSH key for ssh deploy targets: pulling the image and running helm on the host.");
export const API_TEST_CLIENT_SECRET = secret("API_TEST_CLIENT_SECRET", "Client secret of the machine client beta-test gets its token with.");

export const IMAGE = config("image", "Where the app image is pushed.", z.object({
  repository: z.string().describe("Registry repository, e.g. registry.example.com/olve-template-api."),
  registryMap: z.record(z.string(), z.string()).optional().describe("Pull-through caches for the base images (registry → mirror prefix); unset = none."),
}));

export const TARGET = config("target", "Where beta and prod are deployed: inCluster or ssh.", z.object({
  beta: helmTarget({ release: "olve-template-api" }),
  prod: helmTarget({ release: "olve-template-api" }),
}));

export const BETA_CLIENT = config("betaClient", "The machine client beta-test gets its API token with.", z.object({
  tokenUrl: z.url(),
  clientId: z.string(),
  scope: z.string().describe("Must give the token the API's own issuer and audience."),
}));
