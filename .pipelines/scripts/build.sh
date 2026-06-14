#!/bin/sh
# Build the app image with Kaniko and stage the deploy artifacts (image tar, helm
# chart, version) into /output so the deploy step can pick them up from the bundle.
set -e

REPO=OliverVea/Olve.Template.Api
BRANCH=main
VERSION=$(date +%Y%m%d-%H%M%S)

CTX=/kaniko/build-context
mkdir -p "$CTX"
cd "$CTX"

# Fetch the repo tarball. The Kaniko debug image ships busybox wget, which needs
# --no-check-certificate against the GitHub API.
wget --no-check-certificate -q --header="Authorization: token $GITHUB_TOKEN" \
  -O repo.tar.gz "https://api.github.com/repos/$REPO/tarball/$BRANCH"
tar xzf repo.tar.gz --strip-components=1
rm repo.tar.gz

# Carry the helm chart and version forward as build artifacts.
cp -r "$CTX/helm" /output/helm
echo "$VERSION" > /output/version.txt

# Build to a tar (no registry); the deploy step imports it onto the host.
/kaniko/executor \
  --context="$CTX" \
  --dockerfile="$CTX/src/Olve.Template.Api/Dockerfile" \
  --no-push \
  --tar-path=/output/image.tar \
  --destination="olve-template-api:$VERSION"

echo "Build complete: olve-template-api:$VERSION"
