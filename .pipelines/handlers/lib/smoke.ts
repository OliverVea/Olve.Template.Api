import { httpHealthCheck, type Ctx, type HelmRelease, type Infer } from "@olve/pipelines";

function check(ok: boolean, what: string): asserts ok {
  if (!ok) throw new Error(`smoke test: ${what}`);
}

// Read-only (plus one rejected write), so it is safe against prod.
export async function smokeTest(ctx: Ctx, { release, namespace }: Infer<typeof HelmRelease>) {
  const url = `http://${release}.${namespace}.svc.cluster.local`;
  await httpHealthCheck(ctx, `${url}/health`, { attempts: 6, delaySeconds: 5 });

  const page = await ctx.http.getJson<{ items?: unknown; pageNumber?: number; totalCount?: number }>(`${url}/api/messages?page=1&pageSize=5`);
  check(Array.isArray(page.items) && page.pageNumber === 1 && typeof page.totalCount === "number", "/api/messages did not answer a page");

  const auth = await ctx.http.getJson<{ authority?: string; clientId?: string }>(`${url}/api/auth-config`);
  check(auth.authority?.startsWith("https://") === true && !!auth.clientId, "/api/auth-config has no authority or client id");

  const html = await (await ctx.http.expectOk(`${url}/`)).text();
  const bundle = html.match(/src="(\/assets\/[^"]+\.js)"/)?.[1];
  check(bundle !== undefined, "/ does not reference the SPA bundle");
  const js = await ctx.http.expectOk(`${url}${bundle}`);
  check(js.headers.get("content-type")?.includes("javascript") ?? false, `${bundle} is not served as JavaScript`);

  const write = await ctx.http.fetch(`${url}/api/messages`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text: "smoke test" }),
  });
  check(write.status === 401, `an anonymous POST /api/messages answered ${write.status}, expected 401`);

  ctx.log("smoke test passed", { url, authority: auth.authority });
}
