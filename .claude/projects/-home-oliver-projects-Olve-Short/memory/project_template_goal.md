---
name: Solution template goal
description: Project should eventually become a personal dotnet new template with Olve packages baked in
type: project
---

Goal is to turn this into a `dotnet new` solution template for Oliver's own projects. Olve packages (MinimalApi, Results, Validation, Utilities) should be baked in since it's personal use only.

**Why:** Standardize the foundation across personal .NET API projects.

**How to apply:** When making foundational choices (logging, config, OpenAPI), consider how they'll work as template defaults. Keep the setup opinionated rather than parameterized.
