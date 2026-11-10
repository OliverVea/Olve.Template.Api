#!/usr/bin/env bash
# The AOT check: in-process API tests run JIT, so serialization gaps only a trimmed native binary has
# (a missing JsonSerializable, reflection) show up here or on beta. Builds the image, starts it and
# runs the API tests against it in base-URL mode. Needs Docker.
set -euo pipefail
cd "$(dirname "$0")/.."

image="olve-template-api:api-test"
key="api-image-test-signing-key-that-is-long-enough"
docker build -t "$image" .
container=$(docker run -d -p 127.0.0.1::5000 -e Host=0.0.0.0 \
  -e Auth__SigningKey="$key" -e Auth__Authority=api-test -e Auth__Audience=api-test "$image")
trap 'docker rm -f "$container" >/dev/null' EXIT
url="http://$(docker port "$container" 5000/tcp | head -n1)"
for _ in $(seq 60); do curl -fs "$url/health" >/dev/null && break || sleep 1; done
API_BASE_URL="$url" API_SIGNING_KEY="$key" API_ISSUER=api-test API_AUDIENCE=api-test \
  dotnet test --project src/backend/Olve.Template.Api.ApiTests
