import { type Client, createClient as createApiClient, createConfig } from "@api/client";
import type { Problem } from "@api/index";

/** Supplies the current bearer token (async — it may refresh), or nullish for anonymous. */
export type TokenSource = () => Promise<string | null | undefined>;

/** Called on a 401: refresh the credential and return a new token, or null to give up. */
export type UnauthorizedHandler = () => Promise<string | null>;

export type ApiClient = Client;

/** A non-2xx answer: its status and the contract's problems. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problems: Problem[],
  ) {
    super(problems[0]?.message ?? `Request failed (${status}).`);
  }
}

/** The body of a generated SDK call's result, or an {@link ApiError}. */
export async function unwrap<T>(
  call: Promise<{ data?: T; error?: unknown; response?: Response }>,
): Promise<T> {
  const { data, error, response } = await call;
  if (error !== undefined || !response?.ok) {
    throw new ApiError(response?.status ?? 0, Array.isArray(error) ? error : []);
  }
  return data as T;
}

/**
 * Build the client. `getToken` attaches `Authorization: Bearer <token>` when a token is available,
 * and nothing otherwise, so anonymous `GET /api/messages` keeps working. `onUnauthorized` adds a
 * 401 → refresh → retry-once safety net (for the rare case a token is revoked or expires between
 * the proactive refresh and the request). Omit both for a purely anonymous client.
 */
export function createClient(
  baseUrl: string,
  opts: {
    getToken?: TokenSource;
    onUnauthorized?: UnauthorizedHandler;
    fetch?: typeof fetch;
  } = {},
): ApiClient {
  const { getToken, onUnauthorized, fetch: send = (...args) => fetch(...args) } = opts;

  const authFetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const request = new Request(input, init);
    const replay = onUnauthorized ? request.clone() : null;
    const token = await getToken?.();
    if (token) request.headers.set("Authorization", `Bearer ${token}`);

    const response = await send(request);
    if (response.status !== 401 || !replay || !onUnauthorized) return response;

    const fresh = await onUnauthorized();
    if (!fresh) return response;
    replay.headers.set("Authorization", `Bearer ${fresh}`);
    return send(replay);
  };

  return createApiClient(createConfig({ baseUrl, fetch: authFetch }));
}
