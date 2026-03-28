# CI Setup

This template does not include a CI workflow — the actual workflow should live in the service's deployment repo. Below are examples to copy and adapt.

## Example: PR workflow

```yaml
# .github/workflows/pr.yml
name: PR

on:
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: 10.0.100

jobs:
  build-and-test:
    name: Build and test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-restore --no-build -c Release
      - run: dotnet test --no-restore --no-build -c Release -p:RunIntegrationTests=true -p:RunUnitTests=false
```

## Example: Push to main workflow

```yaml
# .github/workflows/push-main.yml
name: Push to main

on:
  push:
    branches: [main]

env:
  DOTNET_VERSION: 10.0.100

jobs:
  build-and-test:
    name: Build and test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-restore --no-build -c Release
      - run: dotnet test --no-restore --no-build -c Release -p:RunIntegrationTests=true -p:RunUnitTests=false

  version:
    name: Compute version
    needs: build-and-test
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.version.outputs.version }}
      artifact-name: ${{ steps.version.outputs.artifact-name }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - name: Compute version
        id: version
        run: |
          dotnet run tools/version.cs -- --ci --run-number ${{ github.run_number }} \
            | tee -a "$GITHUB_OUTPUT"
```

## Versioning

The `tools/version.cs` script computes CalVer versions:

```bash
# Local development
dotnet run tools/version.cs
# version=0.0.0-dev+cb9a99b

# CI (pass run number from GitHub Actions)
dotnet run tools/version.cs -- --ci --run-number 42
# version=2026.3.28.42+cb9a99b

# With runtime identifier for artifact naming
dotnet run tools/version.cs -- --ci --run-number 42 --rid linux-x64
# artifact-name=olve-short-2026.3.28.42+cb9a99b-linux-x64
```

## Test commands

```bash
# Unit tests only (default)
dotnet test

# Integration tests only
dotnet test -p:RunIntegrationTests=true -p:RunUnitTests=false

# All tests
dotnet test -p:RunIntegrationTests=true
```
