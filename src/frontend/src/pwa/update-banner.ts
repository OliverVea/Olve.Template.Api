import { BaseElement } from "../base-element.js";

/**
 * `<update-banner>` — "A new version is available." with a Reload button, pinned to the bottom of
 * the window. {@link showUpdateBanner} adds it when a new build is ready; Reload calls `apply`.
 */
export class UpdateBanner extends BaseElement {
  static readonly tagName = "update-banner";

  apply: () => void = () => {};

  connectedCallback(): void {
    this.render();
  }

  protected styles(): string {
    return `
      :host {
        --edge: color-mix(in oklab, currentColor 14%, transparent);
        position: fixed;
        left: 50%;
        bottom: max(1rem, env(safe-area-inset-bottom));
        transform: translateX(-50%);
        width: min(30rem, calc(100% - 2rem));
        box-sizing: border-box;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.8rem;
        padding: 0.6rem 0.6rem 0.6rem 1rem;
        border: 1px solid var(--edge);
        border-radius: 8px;
        background: Canvas;
        color: CanvasText;
        box-shadow: 0 6px 24px color-mix(in oklab, CanvasText 18%, transparent);
      }
      button {
        font: inherit;
        cursor: pointer;
        border: 1px solid var(--edge);
        border-radius: 6px;
        background: transparent;
        color: inherit;
        padding: 0.35rem 0.8rem;
      }
      button:hover {
        background: color-mix(in oklab, currentColor 8%, transparent);
      }
    `;
  }

  protected template(): string {
    return `<span>A new version is available.</span><button type="button">Reload</button>`;
  }

  protected afterRender(): void {
    this.query<HTMLButtonElement>("button")?.addEventListener("click", () => this.apply());
  }
}

/** Shows the banner (once); Reload calls `apply`. */
export function showUpdateBanner(apply: () => void): void {
  if (!customElements.get(UpdateBanner.tagName))
    customElements.define(UpdateBanner.tagName, UpdateBanner);
  if (document.querySelector(UpdateBanner.tagName)) return;
  const banner = document.createElement(UpdateBanner.tagName) as UpdateBanner;
  banner.setAttribute("role", "status");
  banner.apply = apply;
  document.body.append(banner);
}
