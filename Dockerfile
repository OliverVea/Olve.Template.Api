# The contract: the C# surface, the OpenAPI document and the TS client, all under /repo/artifacts.
FROM oven/bun:1.4.3 AS spec
WORKDIR /repo/src/spec
COPY src/spec/ ./
RUN bun install --frozen-lockfile
RUN bun run build

# The SPA (src/frontend/dist); it ships as static files in the app's wwwroot.
# DELETE-WITH-THE-FRONTEND: this stage and the wwwroot COPY below.
FROM oven/bun:1.4.3 AS frontend
WORKDIR /repo/src/frontend
COPY src/frontend/package.json src/frontend/bun.lock ./
RUN bun install --frozen-lockfile
COPY src/frontend/ ./
COPY --from=spec /repo/artifacts/clients ../../artifacts/clients
RUN bun run build

# An exact SDK tag, the same as the pipeline's (.pipelines/lib/toolchain.ts).
FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/backend/Olve.Template.Api/Olve.Template.Api.csproj src/backend/Olve.Template.Api/
RUN dotnet restore src/backend/Olve.Template.Api -r linux-x64

COPY src/backend/Olve.Template.Api/ src/backend/Olve.Template.Api/
COPY src/spec/main.tsp src/spec/
COPY --from=spec /repo/artifacts artifacts
RUN dotnet publish src/backend/Olve.Template.Api -c Release -r linux-x64 -o /app -p:SkipSpecGen=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
# Static SPA served at / by UseStaticFiles + the index fallback (see Program.cs).
COPY --from=frontend /repo/src/frontend/dist ./wwwroot

ENTRYPOINT ["./Olve.Template.Api"]
