# Standards

Rules for code and behaviour in this repo. MUST = required; SHOULD = default unless there's a
reason. Back a rule with a test where possible.

## API behaviour

- The JSON API lives under `/api/`. `/health` is anonymous and says only up/down; anything
  diagnostic lives under `/api/` behind auth. Whether `/health` is reachable publicly is decided
  in networking (the `Olve.Homelab` edge), not in code.
- Every endpoint MUST be a contract operation in `src/spec/`, the SPA's static files aside.
- Every endpoint requires authentication unless `MapApi` opens it with `.AllowAnonymous()` (the
  fallback policy); an anonymous endpoint SHOULD say why in a comment when it isn't obvious.
- `GET` MUST NOT have side effects beyond logging, metrics and tracing.
- `PUT` and `DELETE` MUST be idempotent in effect: repeating one leaves the same state.
- A `POST` that creates SHOULD accept an `Idempotency-Key` header: a retry with the same key answers
  what the first request did instead of creating again.
- Expected failures MUST be declared responses of the operation, never exceptions. They reach the
  client as a `Problem[]` body (`src/spec/errors.tsp`).
- A missing resource (unknown id or key) MUST answer the declared 404; any other problem answers
  400.
- Request bodies MUST be constrained in the contract (`@minLength`, `@maxLength`, …), so a handler
  only sees input that meets them; a rule the contract can't state goes in a validator the handler
  calls first.
- Timestamps MUST be UTC ISO-8601; IDs are opaque strings to clients.
- Secrets and tokens MUST NOT appear in responses, logs or traces.

## Code

- JSON serialization MUST be source-generated (the contract's generated context, `AppJsonContext`
  for persistence); no reflection-based serialization.
- Generated output (the C# surface, the OpenAPI document, the TS client, build outputs) goes to the
  gitignored `artifacts/`, never into source folders.
- An API change and its client/frontend support land together.
- One C# type per file, named after it (generated code aside).
- IDs in hand-written code are `Id<T>`; a raw `Guid` appears only at the generated contract's boundary.
- A `Result` is read with `TryPickProblems` (or `Pick`), never `.Value!`.
- Real time comes only from an injected `TimeProvider`; `BannedSymbols.txt` fails the build on
  `DateTime.UtcNow`, `DateTimeOffset.UtcNow` and `Thread.Sleep`.
- Explicit wiring (one line per registration or subscription) over generic infrastructure, until three
  real cases share the shape. Prefer the plain design, even if it gives up a feature.

## Tests

- `tools/check.sh --full` is the gate: the pipeline runs it, so what passes locally passes there, and the reverse.
- API tests (`src/backend/Olve.Template.Api.ApiTests`) are behaviour tests over raw HTTP. They MUST run
  unchanged against any target (in-process, the Docker image, live beta), so they:
  - assert only on data they create themselves, never on counts or seeded data;
  - get a token through `ApiTarget.CreateAuthenticatedClient()`, which skips the test where
    none can be minted (beta), rather than failing.
- Wire shapes in API tests are hand-written records, not the service's types, so a renamed or
  retyped field fails a test instead of silently following.
- Unit tests mock with Rocks (source-generated), not reflection-based mocking libraries.
- User-facing text is behaviour: a rejection test asserts the problem's message. Near-identical tests
  are data rows of one test.
- No fixed sleeps: wait on a signal or a fake `TimeProvider`. A flake is a defect to fix, never retried.
- Tests that run on every push make no paid calls (LLMs, paid APIs). The pipeline runs the whole suite
  against beta and a read-only smoke test against prod.
