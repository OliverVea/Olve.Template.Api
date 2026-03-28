---
name: Olve.Results future direction
description: Plans for Olve.Results — static mapper interface, client/server result specializations
type: project
---

Keeping Olve.Results but planning improvements:
- Static mapper interface for Result → HTTP (or other format) mapping
- Error category specializations: `ClientErrorResult` (400-level) and `ServerErrorResult` (500-level) so the mapper knows the HTTP status from the result type
- Base ResultProblem type stays as-is

**Why:** Current friction with AOT, serialization, and deserialization stems from the boundary layer, not the core type. Error categorization lets the mapper produce correct HTTP status codes without guessing.

**How to apply:** Don't work around Olve.Results — the fixes belong in the library itself. Current workarounds (ProblemDto, NoWarn) are temporary.
