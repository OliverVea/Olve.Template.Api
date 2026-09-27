# Standards

Rules for code and behaviour in this repo. MUST = required; SHOULD = default unless there's a
reason. Back a rule with a test where possible.

## API behaviour

- The JSON API lives under `/api/`. `/health` is anonymous and says only up/down; anything
  diagnostic lives under `/api/` behind auth. Whether `/health` is reachable publicly is decided
  in networking (the `Olve.Homelab` edge), not in code.
- Every endpoint requires authentication unless it opts out with `.AllowAnonymous()` (the
  fallback policy); an anonymous endpoint SHOULD say why in a comment when it isn't obvious.
- `GET` MUST NOT have side effects beyond logging, metrics and tracing.
- `PUT` and `DELETE` MUST be idempotent in effect: repeating one leaves the same state.
- Expected failures MUST be `Result` problems (Olve.Results), never exceptions. They reach the
  client as a `ResultProblem[]` body via Olve.MinimalApi's `.WithResultMapping()`.
- A missing resource (unknown id or key) MUST be a `NotFoundProblem`, which answers 404; any other
  problem answers 400. An endpoint that can answer 404 MUST declare it with
  `.Produces<ResultProblem[]>(StatusCodes.Status404NotFound)`.
- Request bodies MUST be validated at the edge with `.WithValidation<TRequest, TValidator>()`,
  so a handler only ever sees valid input.
- Timestamps MUST be UTC ISO-8601; IDs are opaque strings to clients.
- Secrets and tokens MUST NOT appear in responses, logs or traces.

## Code

- JSON serialization MUST be source-generated (`AppJsonContext`); no reflection-based
  serialization.
- Generated output (the OpenAPI document, build outputs) goes to the gitignored `artifacts/`,
  never into source folders. The one exception is the frontend's Kiota client
  (`frontend/src/api`), which is committed so the frontend and the Docker build need no dotnet;
  `mise run client:check` fails CI when it drifts from the API.
- An API change and its client/frontend support land together.

## Tests

- `mise run ci` is the gate: what passes locally passes in the pipeline, and the reverse.
- API tests (`test/Olve.Template.Api.ApiTests`) are behaviour tests over raw HTTP. They MUST run
  unchanged against any target (in-process, the Docker image, live beta), so they:
  - assert only on data they create themselves, never on counts or seeded data;
  - get a token through `ApiTarget.CreateAuthenticatedClient()`, which skips the test where
    none can be minted (beta), rather than failing.
- Wire shapes in API tests are hand-written records, not the service's types, so a renamed or
  retyped field fails a test instead of silently following.
- Unit tests mock with Rocks (source-generated), not reflection-based mocking libraries.
