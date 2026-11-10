import { config, helmTarget, secret, z } from '@olivervea/pipelines';

export const GITHUB_TOKEN = secret(
  'GITHUB_TOKEN',
  'GitHub read token used to fetch the repo tarball.',
);
export const SSH_PRIVATE_KEY = secret(
  'SSH_PRIVATE_KEY',
  'SSH key for ssh deploy targets: pulling the image and running helm on the host.',
);
export const BETA_TOKEN = secret(
  'BETA_TOKEN',
  'Machine token of beta (its Auth:Tokens:beta-test): beta-test runs the API tests with it.',
);
//#if (false)
// The template repo only: dotnet new drops everything up to #endif.
export const NUGET_API_KEY = secret(
  'NUGET_API_KEY',
  'nuget.org API key that publish-template pushes the Olve.Template.Api package with.',
);
//#endif

export const IMAGE = config(
  'image',
  'Where the app image is pushed.',
  z.object({
    repository: z
      .string()
      .describe('Registry repository, e.g. registry.example.com/olve-template-api.'),
    registryMap: z
      .record(z.string(), z.string())
      .optional()
      .describe(
        'Pull-through caches for the base images (registry → mirror prefix); unset = none.',
      ),
  }),
);

// The same shape as Olve.Pipelines' own `auth`: the chart maps it onto Auth__* (src/deploy/helm/values.yaml).
const Auth = z.discriminatedUnion('mode', [
  z.object({
    mode: z
      .literal('none')
      .describe(
        'Every endpoint is anonymous. Only for local or trusted-network instances.',
      ),
  }),
  z.object({
    mode: z.literal('dex').describe("The chart's bundled Dex, with a GitHub upstream."),
    issuer: z
      .url()
      .describe('https://<host>/dex: the edge routes /dex to the Dex Service.'),
    clientId: z.string().default('olve-template-api'),
    frontendClientId: z.string().optional(),
    allow: z
      .object({
        subjects: z
          .array(z.string())
          .default([])
          .describe('Stable subject ids (sub), matched exactly.'),
        users: z
          .array(z.string())
          .default([])
          .describe("GitHub logins, matched against Dex's preferred_username claim."),
        orgs: z
          .array(z.string())
          .default([])
          .describe('GitHub orgs or org:team, matched against the groups claim.'),
        emails: z.array(z.string()).default([]),
      })
      .prefault({})
      .describe('Who may use the service; the chart refuses dex mode without an entry.'),
  }),
  z.object({
    mode: z.literal('authentik'),
    authority: z.url(),
    audience: z.string(),
    frontendAuthority: z.url().optional(),
    frontendAudience: z.string().optional(),
  }),
]);

export const AUTH = config('auth', 'How users sign in to the deployed service.', Auth);

const stageOverrides = {
  auth: Auth.optional().describe('Replaces `auth` for this target.'),
};
export const TARGET = config(
  'target',
  'Where beta and prod are deployed: inCluster or ssh.',
  z.object({
    beta: helmTarget({ release: 'olve-template-api' }, stageOverrides),
    prod: helmTarget({ release: 'olve-template-api' }, stageOverrides),
  }),
);
