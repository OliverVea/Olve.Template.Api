#!/bin/sh
# Beta confirmation (processing step between deploy-beta and deploy): run the API test suite
# (test/Olve.Template.Api.ApiTests) in base-URL mode against the live beta Service, in-cluster (no
# ingress or Tailscale dependency). This tests the very image being promoted, including its AOT
# build. Uses the same entry point as local runs (`mise run api:remote`). Tests that need a token
# skip: beta validates Authentik tokens, not a test key. A failure stops prod.
set -e

mkdir -p /tmp
wget --no-check-certificate -qO /tmp/olve-lib.sh \
  https://raw.githubusercontent.com/OliverVea/Olve.Pipelines/main/.pipelines/scripts/olve-lib.sh
. /tmp/olve-lib.sh

REPO=OliverVea/Olve.Template.Api
BRANCH=main

olve_fetch_repo "$REPO" "$BRANCH" /src
cd /src

curl -fsSL https://mise.run | MISE_INSTALL_PATH=/usr/local/bin/mise sh
mise trust --yes
mise install

export API_BASE_URL=http://olve-template-api.apps-beta.svc.cluster.local
echo "Waiting for $API_BASE_URL/health..."
for i in $(seq 30); do curl -fs "$API_BASE_URL/health" >/dev/null && break; sleep 2; done
mise run api:remote

echo "Beta confirmed"
