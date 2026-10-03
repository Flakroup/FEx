#!/usr/bin/env bash
# Usage: check-changelog.sh <base-sha> <head-sha>   (PR_LABELS: comma-separated label names)
# Rules: shipped code changes need a NEW bullet under [Unreleased]; released history is append-only.
# CHANGELOG content is compared against the merge base, so a PR branched before a release cut is judged fairly.
set -euo pipefail
base=$1 head=$2

if [[ ",${PR_LABELS:-}," == *",no-changelog,"* ]]; then
  echo "Label no-changelog set: skipping."
  exit 0
fi

files=$(git -c core.quotePath=false diff --no-renames --name-only "$base...$head")
shipped=$(grep -E '^(src/|samples/|build/|DevConfigs$|Directory\.Build\.(props|targets)$|\.github/(workflows|scripts)/|build\.ps1$|FEx\.slnx$|GitVersion\.yml$|global\.json$|\.config/dotnet-tools\.json$|\.gitmodules$)' <<<"$files" || true)

# Normalized views: no CR, no trailing whitespace.
norm() { tr -d '\r' | sed 's/[[:space:]]*$//'; }
unreleased() { awk 'tolower($0) ~ /^##[ \t]+\[unreleased\]/ { f = 1; next } /^## \[/ { f = 0 } f'; }
released_from() { awk -v h="$1" '$0 == h { f = 1 } f'; }
first_release() { grep -m1 -E '^## \[v?[0-9]' || true; }

added=""
has_heading=1
if grep -qx 'CHANGELOG.md' <<<"$files"; then
  if ! git cat-file -e "$head:CHANGELOG.md" 2>/dev/null; then
    echo "::error::CHANGELOG.md was deleted; it must stay."
    exit 1
  fi
  head_log=$(git show "$head:CHANGELOG.md" | norm)
  mb=$(git merge-base "$base" "$head")
  base_log=$(git show "$mb:CHANGELOG.md" 2>/dev/null | norm || true)
  heading=$(first_release <<<"$base_log")
  if [[ -n "$heading" ]]; then
    if [[ "$(released_from "$heading" <<<"$base_log")" != "$(released_from "$heading" <<<"$head_log")" ]]; then
      echo "::error::Released sections of CHANGELOG.md are append-only; only edit under '## [Unreleased]'."
      exit 1
    fi
  fi
  grep -qiE '^##[[:space:]]+\[unreleased\]' <<<"$head_log" || has_heading=0
  # An entry is a bullet that is new in [Unreleased] and not copied from released history.
  added=$(grep -E '^[-*] ' <(unreleased <<<"$head_log") \
    | grep -vxFf <(unreleased <<<"$base_log"; released_from "$heading" <<<"$base_log"; echo) || true)
fi

if [[ -z "$shipped" ]]; then
  echo "No shipped paths changed: no entry needed."
  exit 0
fi
if [[ "$has_heading" -eq 0 ]]; then
  echo "::error::CHANGELOG.md has no '## [Unreleased]' section; add it with your entry."
  exit 1
fi
if [[ -z "$added" ]]; then
  echo "$shipped"
  echo "::error::Add a new '- ' bullet under '## [Unreleased]' in CHANGELOG.md, or label the PR 'no-changelog' if it needs none."
  exit 1
fi
echo "CHANGELOG.md entry found."
