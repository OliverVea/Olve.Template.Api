import { afterEach, describe, expect, it, vi } from "vitest";
import { showUpdateBanner, UpdateBanner } from "./update-banner.js";

afterEach(() => {
  document.body.innerHTML = "";
});

describe("<update-banner>", () => {
  it("says a new version is available, and Reload applies it", () => {
    const apply = vi.fn();

    showUpdateBanner(apply);

    const banner = document.querySelector<UpdateBanner>(UpdateBanner.tagName)!;
    expect(banner.getAttribute("role")).toBe("status");
    expect(banner.shadowRoot!.textContent).toContain("A new version is available.");
    banner.shadowRoot!.querySelector("button")!.click();
    expect(apply).toHaveBeenCalledOnce();
  });

  it("is shown once however often it is asked for", () => {
    showUpdateBanner(vi.fn());
    showUpdateBanner(vi.fn());

    expect(document.querySelectorAll(UpdateBanner.tagName)).toHaveLength(1);
  });
});
