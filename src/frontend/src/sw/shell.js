// The app shell's service worker. The build (vite-plugin-sw.ts) writes dist/sw.js as VERSION and
// PRECACHE, then this file, then extensions.js: add a service's own handlers there, not here.
//
// - Install: precaches the build's files under a cache named for its version.
// - Activate: deletes the caches of older versions. A new version waits until the page asks it to
//   take over (SKIP_WAITING, sent when the user accepts "new version, reload").
// - Fetch: a precached file comes from the cache; a page load goes to the network, falling back to
//   the cached index.html offline. Nothing else is touched or cached: not the API, not sign-in
//   (the bundled Dex under /dex, the OIDC callback), not the server's own routes.

const CACHE_PREFIX = "olve-template-api-shell-";
const CACHE = `${CACHE_PREFIX}${VERSION}`;
const INDEX = "/index.html";
const BYPASS = ["/api", "/dex", "/callback", "/health", "/openapi"];

const bypassed = (path) =>
  BYPASS.some((prefix) => path === prefix || path.startsWith(`${prefix}/`));

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(CACHE)
      .then((cache) =>
        cache.addAll(PRECACHE.map((path) => new Request(path, { cache: "reload" }))),
      ),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      for (const key of await caches.keys())
        if (key.startsWith(CACHE_PREFIX) && key !== CACHE) await caches.delete(key);
      await self.clients.claim();
    })(),
  );
});

self.addEventListener("message", (event) => {
  if (event.data?.type === "SKIP_WAITING") self.skipWaiting();
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.origin !== self.location.origin || bypassed(url.pathname))
    return;

  if (request.mode === "navigate") {
    event.respondWith(
      fetch(request).catch(
        async () => (await (await caches.open(CACHE)).match(INDEX)) ?? Response.error(),
      ),
    );
    return;
  }

  if (!PRECACHE.includes(url.pathname)) return;
  event.respondWith(
    caches
      .open(CACHE)
      .then((cache) => cache.match(request))
      .then((cached) => cached ?? fetch(request)),
  );
});
