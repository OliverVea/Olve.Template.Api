#!/usr/bin/env bash
# What the pipeline's test step checks: the contract, formatting, the frontend, the pipeline
# definition and the .NET build.
#
#   tools/check.sh            every check
#   tools/check.sh --full     every check and the tests (what the pipeline runs)
#   tools/check.sh --fix      format first, then every check
#   tools/check.sh --changed  only the checks that what changed against the upstream branch (and
#                             uncommitted work) can break: docs need none; the contract, the
#                             toolchain and anything unlisted need every check
#   tools/check.sh --hook     --changed with output on stderr and exit 2 on a failure, the only
#                             code that makes Claude Code's PreToolUse hook block the push
set -euo pipefail
cd "$(dirname "$0")/.."

mode="${1:-}"
case "$mode" in
  "" | --full | --fix | --changed | --hook) ;;
  *) echo "usage: tools/check.sh [--full|--fix|--changed|--hook]" >&2; exit 2 ;;
esac
if [[ "$mode" == "--hook" ]]; then
  exec 1>&2
  trap 'exit 2' ERR
fi

declare -A want=([backend]=1 [frontend]=1 [pipeline]=1)
if [[ "$mode" == "--changed" || "$mode" == "--hook" ]]; then
  base=$(git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}' 2>/dev/null || echo origin/main)
  want=()
  while IFS= read -r f; do
    case "$f" in
      "" | docs/* | *.md | .claude/*) ;;
      src/backend/*) want[backend]=1 ;;
      src/frontend/*) want[frontend]=1 ;; # DELETE-WITH-THE-FRONTEND
      .pipelines/*) want[pipeline]=1 ;;
      *) want=([backend]=1 [frontend]=1 [pipeline]=1) ;;
    esac
  done < <({ git diff --name-only "$base" --; git ls-files --others --exclude-standard; } | sort -u)
  if [[ ${#want[@]} == 0 ]]; then
    echo "Nothing to check against $base (docs only)."
    exit 0
  fi
  echo "Changed against $base: ${!want[*]}"
fi
has() { [[ -n "${want[$1]:-}" ]]; }

step() { printf '\n== %s\n' "$*"; }
deps() { [[ -d node_modules ]] || bun install --frozen-lockfile; }
# The pipeline follows the SDK's beta tag with no lockfile.
pipeline_deps() { [[ -d node_modules ]] || bun install; }

if [[ "$mode" == "--fix" ]]; then
  step "format"
  (cd src/frontend && deps && bun run format >/dev/null) # DELETE-WITH-THE-FRONTEND
  (cd .pipelines && pipeline_deps && bunx prettier --write '*.ts' 'lib/*.ts' >/dev/null)
  dotnet format Olve.Template.Api.slnx
fi

step "contract"
(cd src/spec && deps && bun run build >/dev/null)

if has pipeline; then
  step "pipeline"
  (cd .pipelines && pipeline_deps && bunx prettier --check '*.ts' 'lib/*.ts' && bunx tsc --noEmit -p .)
  if command -v pl >/dev/null; then pl dev validate --dir .pipelines; fi
fi

# DELETE-WITH-THE-FRONTEND: this step.
if has frontend; then
  step "frontend"
  (cd src/frontend && deps && bun run lint && bun run build)
fi

if has backend; then
  step "backend"
  dotnet format whitespace Olve.Template.Api.slnx --verify-no-changes
  dotnet build Olve.Template.Api.slnx -v q -nologo
fi

if [[ "$mode" == "--full" ]]; then
  step "tests"
  # A hung test fails the run by name instead of stalling the pipeline step: a test host silent
  # for 5 minutes is dumped with the tests still running, and the session ends after 15.
  dotnet test --no-build --timeout 15m --hangdump --hangdump-timeout 5m
  (cd src/frontend && bun run test) # DELETE-WITH-THE-FRONTEND
fi

printf '\nAll checks passed.\n'
