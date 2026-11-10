import {
  checkSpa,
  httpHealthCheck,
  serviceUrl,
  type Ctx,
  type HelmRelease,
  type Infer,
} from '@olivervea/pipelines';

function check(ok: boolean, what: string): asserts ok {
  if (!ok) throw new Error(`smoke test: ${what}`);
}

export const waitHealthy = (ctx: Ctx, url: string) =>
  httpHealthCheck(ctx, `${url}/health`, { attempts: 6, delaySeconds: 5 });

// Read-only (plus one rejected write), so it is safe against prod.
export async function smokeTest(ctx: Ctx, release: Infer<typeof HelmRelease>) {
  const url = release.url ?? serviceUrl(release);
  await waitHealthy(ctx, url);

  const page = await ctx.http.getJson<{
    items?: unknown;
    pageNumber?: number;
    totalCount?: number;
  }>(`${url}/api/messages?page=1&pageSize=5`);
  check(
    Array.isArray(page.items) &&
      page.pageNumber === 1 &&
      typeof page.totalCount === 'number',
    '/api/messages did not answer a page',
  );

  const auth = await ctx.http.getJson<{ authority?: string; clientId?: string }>(
    `${url}/api/auth-config`,
  );
  check(
    auth.authority?.startsWith('https://') === true && !!auth.clientId,
    '/api/auth-config has no authority or client id',
  );

  await checkSpa(ctx, url);

  const write = await ctx.http.fetch(`${url}/api/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text: 'smoke test' }),
  });
  check(
    write.status === 401,
    `an anonymous POST /api/messages answered ${write.status}, expected 401`,
  );

  ctx.log('smoke test passed', { url, authority: auth.authority });
}

// The bundled Dex answers discovery in-cluster with the issuer the SPA is given: catches Dex down, or the
// chart's issuer and the API's authority drifting apart. It can't see the edge's /dex route or GitHub.
export async function checkBundledDex(
  ctx: Ctx,
  release: Infer<typeof HelmRelease>,
  url: string,
) {
  const auth = await ctx.http.getJson<{ authority?: string }>(`${url}/api/auth-config`);
  if (!auth.authority?.endsWith('/dex')) {
    ctx.log('no bundled Dex to check', { authority: auth.authority });
    return;
  }
  const dex = `http://${release.release}-dex.${release.namespace}.svc.cluster.local:5556`;
  const discovery = await ctx.http.getJson<{ issuer?: string }>(
    `${dex}/dex/.well-known/openid-configuration`,
  );
  check(
    discovery.issuer === auth.authority,
    `Dex's issuer is ${discovery.issuer}, but /api/auth-config gives the SPA ${auth.authority}`,
  );
  ctx.log('bundled Dex answers discovery', { issuer: discovery.issuer });
}
