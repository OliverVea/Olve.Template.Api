import { beforeAll, describe, expect, it, vi } from "vitest";
import { createClient } from "../api-client.js";
import { MessageList } from "./message-list.js";

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });

const emptyPage = { items: [], totalCount: 0, hasNextPage: false };

/** A real client over a faked fetch: `answer` gets each request, GET defaults to an empty page. */
function fakeClient(answer?: (request: Request) => Response | undefined) {
  const requests: Request[] = [];
  const send = vi.fn(async (input: RequestInfo | URL) => {
    const request = input as Request;
    requests.push(request.clone());
    return answer?.(request) ?? json(request.method === "GET" ? emptyPage : {});
  });
  const client = createClient("http://api.test", { fetch: send as typeof fetch });
  return { client, requests, gets: () => requests.filter((r) => r.method === "GET").length };
}

beforeAll(() => customElements.define(MessageList.tagName, MessageList));

function make(): MessageList {
  return document.createElement(MessageList.tagName) as MessageList;
}

describe("<message-list>", () => {
  it("renders the messages the client returns, with a count", async () => {
    const { client } = fakeClient(() =>
      json({
        items: [
          { id: "aaaaaaaa-0000", text: "hello" },
          { id: "bbbbbbbb-1111", text: "world" },
        ],
        totalCount: 2,
        hasNextPage: false,
      }),
    );

    const el = make();
    el.client = client;
    await el.load();

    const texts = [...el.shadowRoot!.querySelectorAll(".text")].map((n) => n.textContent);
    expect(texts).toEqual(["hello", "world"]);
    expect(el.shadowRoot!.querySelector(".count")?.textContent).toContain("2 messages");
  });

  it("shows the empty state when there are no messages", async () => {
    const el = make();
    el.client = fakeClient().client;
    await el.load();
    expect(el.shadowRoot!.querySelector(".muted")?.textContent).toMatch(/No messages yet/);
  });

  it("surfaces an auth-specific error on 401", async () => {
    const el = make();
    el.client = fakeClient(() => new Response(null, { status: 401 })).client;
    await el.load();
    expect(el.shadowRoot!.querySelector(".error")?.textContent).toMatch(/Authentication required/);
  });

  it("surfaces the contract's problem message", async () => {
    const { client } = fakeClient((r) =>
      r.method === "POST" ? json([{ message: "'text' cannot be empty." }], 400) : undefined,
    );
    const el = make();
    el.client = client;
    await el.load();

    await el.create("x");

    expect(el.shadowRoot!.querySelector(".error")?.textContent).toContain("cannot be empty");
  });

  it("create() posts the body then reloads", async () => {
    const { client, requests, gets } = fakeClient();
    const el = make();
    el.client = client;
    await el.load();
    const before = gets();

    await el.create("new message");

    const post = requests.find((r) => r.method === "POST");
    expect(await post?.json()).toEqual({ text: "new message" });
    expect(gets()).toBe(before + 1); // reloaded after the write
  });

  it("deleteMessage() deletes by id then reloads", async () => {
    const { client, requests, gets } = fakeClient();
    const el = make();
    el.client = client;
    await el.load();
    const before = gets();

    await el.deleteMessage("aaaaaaaa-0000");

    const del = requests.find((r) => r.method === "DELETE");
    expect(del?.url).toContain("/api/messages/aaaaaaaa-0000");
    expect(gets()).toBe(before + 1);
  });
});
