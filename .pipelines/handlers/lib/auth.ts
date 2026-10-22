import type { Ctx } from "@olve/pipelines";

const BETA_TOKEN_URL = "https://auth-beta.ovea.pro/application/o/token/";
const BETA_TEST_CLIENT_ID = "olve-template-api-tests";

// The olve-template-api-aud scope gives the token the API's own iss and aud.
export async function betaTestToken(ctx: Ctx, clientSecret: string): Promise<string> {
  const response = await ctx.http.fetch(BETA_TOKEN_URL, {
    method: "POST",
    body: new URLSearchParams({
      grant_type: "client_credentials",
      client_id: BETA_TEST_CLIENT_ID,
      client_secret: clientSecret,
      scope: "openid olve-template-api-aud",
    }),
  });
  if (!response.ok) throw new Error(`beta Authentik token request for ${BETA_TEST_CLIENT_ID} answered ${response.status}: ${await response.text()}`);
  const { access_token } = (await response.json()) as { access_token?: string };
  if (!access_token) throw new Error(`beta Authentik answered no access_token for ${BETA_TEST_CLIENT_ID}`);
  return access_token;
}
