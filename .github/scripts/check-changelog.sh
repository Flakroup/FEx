#!/usr/bin/env bash
# Usage: check-changelog.sh <base-sha> <head-sha>   (PR_LABELS: comma-separated label names)
# Rules: shipped code changes need a NEW line under [Unreleased]; released history is append-only.
set -euo pipefail
base=$1 head=$2

if [[ ",${PR_LABELS:-}," == *",no-changelog,"* ]]; then
  echo "Label no-changelog set: skipping."
  exit 0
fi

files=$(git -c core.quotePath=false diff --no-renames --name-only "$base...$head")
shipped=$(grep -E '^(src/|samples/|build/|DevConfigs$|Directory\.Build\.(props|targets)$|\.github/(workflows|scripts)/|build\.ps1$|FEx\.slnx$|GitVersion\.yml$|global\.json$|\.config/dotnet-tools\.json$|\.gitmodules$)' <<<"$files" || true)
changelog_changed=$(grep -cx 'CHANGELOG.md' <<<"$files" || true)

unreleased() { awk '/^## \[Unreleased\]/ { f = 1; next } /^## \[/ { f = 0 } f'; }
released() { awk '/^## \[[0-9]/ { f = 1 } f'; }

if [[ "$changelog_changed" -gt 0 ]]; then
  if ! head_log=$(git show "$head:CHANGELOG.md" 2>/dev/null); then
    echo "::error::CHANGELOG.md was deleted; it must stay."
    exit 1
  fi
  if base_log=$(git show "$base:CHANGELOG.md" 2>/dev/null); then
    block=$(released <<<"$base_log")
    if [[ -n "$block" && "$head_log" != *"$block"* ]]; then
      echo "::error::Released sections of CHANGELOG.md are append-only; only edit under '## [Unreleased]'."
      exit 1
    fi
  else
    base_log=""
  fi
  added=$(grep -vxFf <(unreleased <<<"$base_log") <(unreleased <<<"$head_log") | grep -v '^[[:space:]]*$' || true)
else
  added=""
fi

if [[ -z "$shipped" ]]; then
  echo "No shipped paths changed: no entry needed."
  exit 0
fi
if [[ -z "$added" ]]; then
  echo "$shipped"
  echo "::error::Add a new line under '## [Unreleased]' in CHANGELOG.md, or label the PR 'no-changelog' if it needs none."
  exit 1
fi
echo "CHANGELOG.md entry found."
