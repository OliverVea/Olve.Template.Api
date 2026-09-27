#!/bin/sh
# Production gate: runs `mise run ci`, the exact command a developer (or Claude) runs locally. It
# pins the toolchain (node + dotnet from mise.toml), builds and tests the backend (unit + in-process
# API tests), checks the committed TS client against the API, and lints/tests/builds the frontend.
# Runs in PARALLEL with build-and-package; a failure fails the production job group, which gates
# the processing cascade (deploy never runs). Produces no deploy artifacts (no version.txt), so the
# deploy scripts ignore its bundle dir.
#
# Code comes from the GitHub tarball, the same way build.sh fetches it (the runner has no git
# checkout). The tarball is `git archive`-equivalent: source only, no .git dir.
set -e

# Fetch the shared helper library (see build.sh for why fetch-to-file + --no-check-certificate
# and why /tmp must be created first).
mkdir -p /tmp
wget --no-check-certificate -qO /tmp/olve-lib.sh \
  https://raw.githubusercontent.com/OliverVea/Olve.Pipelines/main/.pipelines/scripts/olve-lib.sh
. /tmp/olve-lib.sh

REPO=OliverVea/Olve.Template.Api
BRANCH=main

olve_fetch_repo "$REPO" "$BRANCH" /src
cd /src

# mise: single static binary; installs the pinned node/dotnet on first use.
curl -fsSL https://mise.run | MISE_INSTALL_PATH=/usr/local/bin/mise sh
mise trust --yes
mise install
mise run ci

echo "Checks passed"
