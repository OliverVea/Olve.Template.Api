// artifacts/spec/openapi.json → artifacts/clients/ts (gitignored). baseUrl: false, the frontend configures it.
export default {
  input: "../../artifacts/spec/openapi.json",
  output: { path: "../../artifacts/clients/ts", tsConfigPath: null },
  plugins: [{ name: "@hey-api/client-fetch", baseUrl: false }],
};
