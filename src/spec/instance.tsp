import "@typespec/http";

using Http;

namespace Service;

/**
 * The public OIDC settings the SPA needs to start an Authorization Code + PKCE login. Served at
 * runtime because one image deploys to several OIDC environments; a browser holds no secret.
 */
model FrontendAuthConfig {
  /** OIDC issuer of the SPA's public client. */
  authority: string | null;

  /** The public client id the browser authorizes as. */
  clientId: string | null;

  /** Space-delimited scopes; `offline_access` is what yields a refresh token. */
  scopes: string;
}

@route("/health")
interface Health {
  /** Liveness. */
  @get get(): OkResponse;
}

@route("/api/auth-config")
interface AuthConfig {
  /** The login settings. */
  @get get(): FrontendAuthConfig;
}
