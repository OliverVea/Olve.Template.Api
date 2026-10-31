import type { Ctx } from "@olivervea/pipelines";

export interface TokenClient {
  tokenUrl: string;
  clientId: string;
  scope: string;
}

export async function clientCredentialsToken(ctx: Ctx, client: TokenClient, clientSecret: string): Promise<string> {
  const response = await ctx.http.fetch(client.tokenUrl, {
    method: "POST",
    body: new URLSearchParams({
      grant_type: "client_credentials",
      client_id: client.clientId,
      client_secret: clientSecret,
      scope: client.scope,
    }),
  });
  if (!response.ok) throw new Error(`token request for ${client.clientId} answered ${response.status}: ${await response.text()}`);
  const { access_token } = (await response.json()) as { access_token?: string };
  if (!access_token) throw new Error(`no access_token for ${client.clientId}`);
  return access_token;
}
