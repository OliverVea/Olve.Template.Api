import { describe, expect, it, vi } from "vitest";
import extensions from "../sw/extensions.js?raw";
import shell from "../sw/shell.js?raw";
import { type BuiltFile, serviceWorkerSource } from "./service-worker-source.js";

const ORIGIN = "https://app.test";
const PREFIX = "olve-template-api-shell-";

const file = (path: string, content = path): BuiltFile => ({
  path,
  content: new TextEncoder().encode(content),
});

const build = (index = "<html>v1</html>", bundle = "assets/index-AAAA.js") => [
  file("index.html", index),
  file(bundle),
  file("manifest.webmanifest"),
  file("icons/icon-192.png"),
  file("assets/index-AAAA.js.map"),
  file("sw.js", "an old worker"),
];

/** The browser's CacheStorage, shared by the workers of successive builds like the real one. */
class FakeCaches {
  readonly stores = new Map<string, Map<string, Response>>();
  async open(name: string) {
    const store = this.stores.get(name) ?? new Map<string, Response>();
    this.stores.set(name, store);
    const urlOf = (r: string | { url: string }) =>
      new URL(typeof r === "string" ? r : r.url, ORIGIN).href;
    return {
      addAll: async (requests: { url: string }[]) => {
        for (const r of requests) store.set(urlOf(r), new Response(`cached ${r.url}`));
      },
      match: async (r: string | { url: string }) => store.get(urlOf(r))?.clone(),
    };
  }
  async keys() {
    return [...this.stores.keys()];
  }
  async delete(name: string) {
    return this.stores.delete(name);
  }
}

type Handler = (event: unknown) => void;

/** Runs a generated sw.js in a fake service-worker global and drives its events. */
function runWorker(
  source: string,
  caches: FakeCaches,
  network: (url: string) => Promise<Response>,
) {
  const handlers = new Map<string, Handler>();
  const scope = {
    location: new URL(`${ORIGIN}/sw.js`),
    clients: { claim: vi.fn(async () => {}) },
    skipWaiting: vi.fn(),
    addEventListener: (type: string, handler: Handler) => handlers.set(type, handler),
  };
  const fetch = vi.fn((request: { url: string }) => network(request.url));
  const FakeRequest = function (this: object, url: string, init: object) {
    Object.assign(this, init, { url: new URL(url, ORIGIN).href });
  };
  new Function("self", "caches", "fetch", "Request", "Response", source)(
    scope,
    caches,
    fetch,
    FakeRequest,
    Response,
  );

  const extend = async (type: string, extra: object = {}) => {
    const waits: Promise<unknown>[] = [];
    handlers.get(type)?.({ waitUntil: (p: Promise<unknown>) => waits.push(p), ...extra });
    await Promise.all(waits);
  };
  return {
    scope,
    fetch,
    install: () => extend("install"),
    activate: () => extend("activate"),
    message: (data: unknown) => extend("message", { data }),
    /** What the worker answers for a request, or undefined when it leaves it to the browser. */
    request: async (path: string, init: { method?: string; mode?: string } = {}) => {
      let answer: Promise<Response> | undefined;
      const request = { url: new URL(path, ORIGIN).href, method: "GET", mode: "cors", ...init };
      handlers.get("fetch")?.({
        request,
        respondWith: (r: Promise<Response>) => {
          answer = r;
        },
      });
      return answer ? await answer : undefined;
    },
  };
}

const online = async (url: string) => new Response(`network ${new URL(url).pathname}`);
const offline = async () => {
  throw new TypeError("Failed to fetch");
};

describe("the generated service worker", () => {
  it("precaches the built files, not itself or source maps", async () => {
    const worker = await serviceWorkerSource(build(), shell, extensions);

    expect(worker.precache).toEqual([
      "/assets/index-AAAA.js",
      "/icons/icon-192.png",
      "/index.html",
      "/manifest.webmanifest",
    ]);
  });

  it("has the same version for the same build, a new one when any file changes", async () => {
    const v1 = (await serviceWorkerSource(build(), shell, extensions)).version;

    expect((await serviceWorkerSource(build(), shell, extensions)).version).toBe(v1);
    expect(
      (await serviceWorkerSource(build("<html>v2</html>"), shell, extensions)).version,
    ).not.toBe(v1);
    expect((await serviceWorkerSource(build(), shell, `${extensions}\n// more`)).version).not.toBe(
      v1,
    );
  });

  it("installs the build under a cache named for its version", async () => {
    const caches = new FakeCaches();
    const worker = await serviceWorkerSource(build(), shell, extensions);
    const sw = runWorker(worker.source, caches, online);

    await sw.install();

    const store = caches.stores.get(`${PREFIX}${worker.version}`);
    expect([...(store?.keys() ?? [])].map((url) => new URL(url).pathname).sort()).toEqual(
      worker.precache,
    );
  });

  it("serves a precached file from the cache", async () => {
    const caches = new FakeCaches();
    const sw = runWorker(
      (await serviceWorkerSource(build(), shell, extensions)).source,
      caches,
      online,
    );
    await sw.install();

    const response = await sw.request("/assets/index-AAAA.js");

    expect(await response?.text()).toBe(`cached ${ORIGIN}/assets/index-AAAA.js`);
    expect(sw.fetch).not.toHaveBeenCalled();
  });

  it.each([
    ["GET", "/api/messages", "cors"],
    ["GET", "/api/auth-config", "cors"],
    ["POST", "/api/messages", "cors"],
    ["GET", "/callback?code=abc&state=xyz", "navigate"],
    ["GET", "/dex/auth?client_id=x", "navigate"],
    ["GET", "/dex/.well-known/openid-configuration", "cors"],
    ["GET", "/health", "cors"],
    ["GET", "/openapi/v1.json", "cors"],
    ["GET", "https://elsewhere.test/script.js", "cors"],
  ])("never answers %s %s (%s): the browser goes to the network", async (method, path, mode) => {
    const caches = new FakeCaches();
    const sw = runWorker(
      (await serviceWorkerSource(build(), shell, extensions)).source,
      caches,
      online,
    );
    await sw.install();

    expect(await sw.request(path, { method, mode })).toBeUndefined();
    const cached = [...caches.stores.values()].flatMap((store) => [...store.keys()]);
    expect(cached.some((url) => url.includes("/api") || url.includes("/dex"))).toBe(false);
  });

  it("loads pages from the network, and the cached index.html offline", async () => {
    const caches = new FakeCaches();
    const source = (await serviceWorkerSource(build(), shell, extensions)).source;
    const sw = runWorker(source, caches, online);
    await sw.install();

    const page = await sw.request("/messages/42", { mode: "navigate" });
    expect(await page?.text()).toBe("network /messages/42");

    const down = runWorker(source, caches, offline);
    const fallback = await down.request("/messages/42", { mode: "navigate" });
    expect(await fallback?.text()).toBe(`cached ${ORIGIN}/index.html`);
  });

  it("a new build replaces the old: its cache is installed and the old one deleted", async () => {
    const caches = new FakeCaches();
    caches.stores.set("another-cache", new Map());
    const v1 = await serviceWorkerSource(build(), shell, extensions);
    const old = runWorker(v1.source, caches, online);
    await old.install();
    await old.activate();

    const v2 = await serviceWorkerSource(
      build("<html>v2</html>", "assets/index-BBBB.js"),
      shell,
      extensions,
    );
    const next = runWorker(v2.source, caches, online);
    await next.install();
    expect([...caches.stores.keys()]).toContain(`${PREFIX}${v1.version}`); // the old page still runs

    await next.activate();

    expect([...caches.stores.keys()].sort()).toEqual(
      ["another-cache", `${PREFIX}${v2.version}`].sort(),
    );
    expect(next.scope.clients.claim).toHaveBeenCalled();
    const bundle = await next.request("/assets/index-BBBB.js");
    expect(await bundle?.text()).toBe(`cached ${ORIGIN}/assets/index-BBBB.js`);
  });

  it("waits for the page before taking over", async () => {
    const sw = runWorker(
      (await serviceWorkerSource(build(), shell, extensions)).source,
      new FakeCaches(),
      online,
    );
    await sw.install();
    expect(sw.scope.skipWaiting).not.toHaveBeenCalled();

    await sw.message({ type: "SKIP_WAITING" });

    expect(sw.scope.skipWaiting).toHaveBeenCalledOnce();
  });
});
