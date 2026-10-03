#!/usr/bin/env bash
# Usage: check-changelog.sh <base-sha> <head-sha>   (PR_LABELS: comma-separated label names)
# Fails when shipped code changes without a CHANGELOG.md entry under [Unreleased].
set -euo pipefail
base=$1 head=$2

if [[ ",${PR_LABELS:-}," == *",no-changelog,"* ]]; then
  echo "Label no-changelog set: skipping."
  exit 0
fi

files=$(git diff --name-only "$base...$head")
shipped=$(grep -E '^(src/|samples/|build/|DevConfigs$|Directory\.Build\.(props|targets)$|\.github/workflows/|build\.ps1$|FEx\.slnx$)' <<<"$files" || true)

if grep -qx 'CHANGELOG.md' <<<"$files"; then
  # Added lines must sit above the first released "## [x.y.z]" heading.
  first_release=$(git show "$head:CHANGELOG.md" | grep -n -m1 -E '^## \[[0-9]' | cut -d: -f1 || true)
  if [[ -n "$first_release" ]]; then
    below=$(git diff -U0 "$base...$head" -- CHANGELOG.md \
      | awk -v r="$first_release" '/^@@/ { split($3, a, ","); s = substr(a[1], 2) + 0; n = (a[2] == "" ? 1 : a[2] + 0); if (n > 0 && s + n - 1 >= r) print }')
    if [[ -n "$below" ]]; then
      echo "::error::CHANGELOG.md was edited below the first released heading (line $first_release); add the entry under '## [Unreleased]'."
      exit 1
    fi
  fi
  echo "CHANGELOG.md updated."
  exit 0
fi

if [[ -n "$shipped" ]]; then
  echo "$shipped"
  echo "::error::Add a CHANGELOG.md entry under '## [Unreleased]', or label the PR 'no-changelog' if it needs none."
  exit 1
fi
echo "No shipped paths changed: no entry needed."
