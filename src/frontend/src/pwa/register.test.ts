import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { registerServiceWorker } from "./register.js";

class FakeWorker extends EventTarget {
  state: ServiceWorkerState = "installing";
  readonly postMessage = vi.fn();
  become(state: ServiceWorkerState) {
    this.state = state;
    this.dispatchEvent(new Event("statechange"));
  }
}

class FakeRegistration extends EventTarget {
  installing: FakeWorker | null = null;
  waiting: FakeWorker | null = null;
  readonly update = vi.fn(async () => this);
  /** The browser found a new sw.js and starts installing it. */
  found(): FakeWorker {
    const worker = new FakeWorker();
    this.installing = worker;
    this.dispatchEvent(new Event("updatefound"));
    return worker;
  }
  installed(worker: FakeWorker) {
    this.installing = null;
    this.waiting = worker;
    worker.become("installed");
  }
}

class FakeContainer extends EventTarget {
  readonly registration = new FakeRegistration();
  readonly register = vi.fn(async () => this.registration);
  constructor(public controller: object | null) {
    super();
  }
  takeOver() {
    this.controller = {};
    this.registration.waiting = null;
    this.dispatchEvent(new Event("controllerchange"));
  }
}

function start(controller: object | null) {
  const container = new FakeContainer(controller);
  const reload = vi.fn();
  const onUpdate = vi.fn<(apply: () => void) => void>();
  const registered = registerServiceWorker({
    container: container as unknown as ServiceWorkerContainer,
    reload,
    onUpdate,
  });
  return { container, reload, onUpdate, registered };
}

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

describe("registerServiceWorker", () => {
  it("registers /sw.js for the whole site, bypassing the HTTP cache", async () => {
    const { container, registered } = start(null);
    await registered;

    expect(container.register).toHaveBeenCalledWith("/sw.js", {
      scope: "/",
      updateViaCache: "none",
    });
  });

  it("on a first visit neither offers an update nor reloads when the worker takes over", async () => {
    const { container, reload, onUpdate, registered } = start(null);
    await registered;

    const worker = container.registration.found();
    container.registration.installed(worker);
    container.takeOver();

    expect(onUpdate).not.toHaveBeenCalled();
    expect(reload).not.toHaveBeenCalled();
  });

  it("offers a new build once it has installed, and reloads only when accepted", async () => {
    const { container, reload, onUpdate, registered } = start({});
    await registered;

    const worker = container.registration.found();
    expect(onUpdate).not.toHaveBeenCalled();
    container.registration.installed(worker);
    expect(onUpdate).toHaveBeenCalledOnce();
    expect(reload).not.toHaveBeenCalled();

    onUpdate.mock.calls[0][0]();
    expect(worker.postMessage).toHaveBeenCalledWith({ type: "SKIP_WAITING" });
    container.takeOver();

    expect(reload).toHaveBeenCalledOnce();
  });

  it("offers a build that was already waiting when the page loaded", async () => {
    const container = new FakeContainer({});
    container.registration.waiting = new FakeWorker();
    const onUpdate = vi.fn();
    await registerServiceWorker({
      container: container as unknown as ServiceWorkerContainer,
      reload: vi.fn(),
      onUpdate,
    });

    expect(onUpdate).toHaveBeenCalledOnce();
  });

  it("reloads at most once, however often the controller changes", async () => {
    const { container, reload, onUpdate, registered } = start({});
    await registered;
    container.registration.installed(container.registration.found());

    onUpdate.mock.calls[0][0]();
    container.takeOver();
    container.takeOver();
    container.registration.installed(container.registration.found());
    container.takeOver();

    expect(reload).toHaveBeenCalledOnce();
    expect(onUpdate).toHaveBeenCalledOnce();
  });

  it("offers a reload when another tab accepted the update", async () => {
    const { container, reload, onUpdate, registered } = start({});
    await registered;

    container.takeOver();
    expect(reload).not.toHaveBeenCalled();
    expect(onUpdate).toHaveBeenCalledOnce();

    onUpdate.mock.calls[0][0]();
    expect(reload).toHaveBeenCalledOnce();
  });

  it("reloads when Reload is clicked after another tab already took the update", async () => {
    const { container, reload, onUpdate, registered } = start({});
    await registered;
    const worker = container.registration.found();
    container.registration.installed(worker);

    container.takeOver(); // the other tab's Reload
    onUpdate.mock.calls[0][0]();

    expect(reload).toHaveBeenCalledOnce();
    expect(onUpdate).toHaveBeenCalledOnce();
  });

  it("asks the server for a new build every hour and when the page is shown again", async () => {
    const { container, registered } = start({});
    await registered;

    vi.advanceTimersByTime(60 * 60 * 1000);
    expect(container.registration.update).toHaveBeenCalledOnce();

    document.dispatchEvent(new Event("visibilitychange"));
    expect(container.registration.update).toHaveBeenCalledTimes(2);
  });
});
