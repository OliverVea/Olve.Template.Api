import {
  AnonymousAuthenticationProvider,
  type AuthenticationProvider,
  type RequestInformation,
} from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import {
  createOlveTemplateApiClient,
  type OlveTemplateApiClient,
} from "./api/olveTemplateApiClient.js";

/** Supplies the current bearer token, or `null`/`undefined` for anonymous requests. */
export type TokenSource = () => string | null | undefined;

/**
 * Attaches `Authorization: Bearer <token>` when a token is available, and nothing
 * otherwise — so anonymous `GET /messages` keeps working while authenticated writes
 * (POST/PUT/DELETE) get a token the moment one is provided. A whole real auth flow (OIDC,
 * refresh) would live behind this same seam.
 */
class BearerTokenAuthenticationProvider implements AuthenticationProvider {
  constructor(private readonly getToken: TokenSource) {}

  public authenticateRequest = async (request: RequestInformation): Promise<void> => {
    const token = this.getToken();
    if (token) {
      request.headers.tryAdd("Authorization", `Bearer ${token}`);
    }
  };
}

/**
 * Build the Kiota client. Pass `getToken` to enable authenticated writes; omit it for a
 * purely anonymous client. The generated `createOlveTemplateApiClient` registers the JSON
 * (and text/form/multipart) serializers on the adapter itself.
 */
export function createClient(baseUrl: string, getToken?: TokenSource): OlveTemplateApiClient {
  const authProvider: AuthenticationProvider = getToken
    ? new BearerTokenAuthenticationProvider(getToken)
    : new AnonymousAuthenticationProvider();

  const adapter = new FetchRequestAdapter(authProvider);
  adapter.baseUrl = baseUrl;
  return createOlveTemplateApiClient(adapter);
}
