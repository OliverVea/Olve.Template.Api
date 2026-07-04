import { createClient } from "./api-client.js";
import { MessageList } from "./components/message-list.js";

const TOKEN_KEY = "olve-template-api.token";

// Optional bearer token for authenticated writes, persisted across reloads. GET is
// anonymous, so this stays empty for read-only use.
let token: string | null = localStorage.getItem(TOKEN_KEY);
const tokenInput = document.querySelector<HTMLInputElement>("#token");
if (tokenInput) {
  tokenInput.value = token ?? "";
  tokenInput.addEventListener("change", () => {
    token = tokenInput.value.trim() || null;
    if (token) localStorage.setItem(TOKEN_KEY, token);
    else localStorage.removeItem(TOKEN_KEY);
  });
}

// In dev, same-origin → Vite proxies /messages to the backend (see vite.config.ts).
// In a build, set VITE_API_BASE_URL to point at the API host.
const baseUrl = import.meta.env.VITE_API_BASE_URL ?? window.location.origin;
const client = createClient(baseUrl, () => token);

customElements.define(MessageList.tagName, MessageList);

const list = document.querySelector<MessageList>(MessageList.tagName);
if (list) list.client = client;
