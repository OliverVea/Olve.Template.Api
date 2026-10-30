# The contract: the C# surface, the OpenAPI document and the TS client, all under /repo/artifacts.
FROM node:24-alpine AS spec
WORKDIR /repo/src/spec
COPY src/spec/ ./
RUN npm ci
RUN npm run build

# Build the SPA (frontend/dist) in a Node stage; it ships as static files in the app's wwwroot.
# Delete this stage (and the wwwroot COPY below) for a headless service.
FROM node:24-alpine AS frontend
WORKDIR /repo/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
COPY --from=spec /repo/artifacts/clients ../artifacts/clients
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
RUN apt-get update && apt-get install -y clang zlib1g-dev
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Olve.Template.Api/Olve.Template.Api.csproj src/Olve.Template.Api/
RUN dotnet restore src/Olve.Template.Api -r linux-x64

COPY src/Olve.Template.Api/ src/Olve.Template.Api/
COPY src/spec/main.tsp src/spec/
COPY --from=spec /repo/artifacts artifacts
RUN dotnet publish src/Olve.Template.Api -c Release -r linux-x64 -o /app -p:SkipSpecGen=true

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
# Static SPA served at / by UseStaticFiles + the index fallback (see Program.cs).
COPY --from=frontend /repo/frontend/dist ./wwwroot

ENTRYPOINT ["./Olve.Template.Api"]
