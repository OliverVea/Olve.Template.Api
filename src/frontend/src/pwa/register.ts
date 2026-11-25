export interface RegisterOptions {
  /** A new build is ready: offer it, and call `apply` when the user accepts. */
  onUpdate: (apply: () => void) => void;
  container?: ServiceWorkerContainer;
  reload?: () => void;
  /** How often an open page asks the server for a new build, besides each time it is shown again. */
  checkEveryMs?: number;
}

const SKIP_WAITING = { type: "SKIP_WAITING" } as const;

/**
 * Registers /sw.js and runs the update flow. A new build installs beside the running one and waits;
 * the page offers it, and only when the user accepts does the new worker take over and the page
 * reload, once. A page that had no worker when it loaded (a first visit) is never offered or
 * reloaded: the worker's first takeover is not an update.
 */
export async function registerServiceWorker(
  options: RegisterOptions,
): Promise<ServiceWorkerRegistration> {
  const container = options.container ?? navigator.serviceWorker;
  const reload = options.reload ?? (() => window.location.reload());
  const hadController = container.controller !== null;
  let accepted = false;
  let reloaded = false;
  let offered = false;

  const reloadOnce = () => {
    if (reloaded) return;
    reloaded = true;
    reload();
  };
  const offer = (apply: () => void) => {
    if (offered) return;
    offered = true;
    options.onUpdate(apply);
  };

  container.addEventListener("controllerchange", () => {
    if (accepted) reloadOnce();
    // Another tab accepted the update: this page still runs the old build.
    else if (hadController) offer(reloadOnce);
  });

  const registration = await container.register("/sw.js", { scope: "/", updateViaCache: "none" });

  const offerWaiting = () => {
    if (!registration.waiting || !container.controller) return;
    offer(() => {
      const waiting = registration.waiting;
      // Nothing waits any more when another tab has already let the new build take over.
      if (!waiting) return reloadOnce();
      accepted = true;
      waiting.postMessage(SKIP_WAITING);
    });
  };

  offerWaiting();
  registration.addEventListener("updatefound", () => {
    const installing = registration.installing;
    installing?.addEventListener("statechange", () => {
      if (installing.state === "installed") offerWaiting();
    });
  });

  const check = () => void registration.update().catch(() => {});
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") check();
  });
  setInterval(check, options.checkEveryMs ?? 60 * 60 * 1000);

  return registration;
}
