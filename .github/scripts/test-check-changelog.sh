#!/usr/bin/env bash
# Builds a throwaway repo and asserts the exit code of check-changelog.sh per scenario.
set -uo pipefail
script=$(cd "$(dirname "$0")" && pwd)/check-changelog.sh
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
mkdir "$tmp/repo" && cd "$tmp/repo"
git init -q . && git config user.email t@t && git config user.name t
git config advice.addEmbeddedRepo false
mkdir src test docs
echo a > src/a.cs; echo a > test/a.cs
cat > CHANGELOG.md <<'L'
# Changelog

## [Unreleased]

### Added

- existing entry

## [0.3.0] - 2026-01-01

- old one
- old two
L
git init -q DevConfigs && git -C DevConfigs -c user.email=t@t -c user.name=t commit -q --allow-empty -m s1
git add -A && git commit -qm base && git branch -q base
fail=0
# case <name> <expected-exit> <labels> <mutation...>
case_() {
  local name=$1 want=$2 labels=$3; shift 3
  git checkout -q -B work base
  ( "$@" ) >/dev/null 2>&1
  git add -A; git commit -qm work --allow-empty
  PR_LABELS=$labels bash "$script" base work >"$tmp/out" 2>&1; local got=$?
  if [[ $got -eq $want ]]; then echo "ok   $name (exit $got)"; else echo "FAIL $name: want $want got $got"; cat "$tmp/out"; fail=1; fi
}
add_entry() { sed -i 's/^- existing entry$/&\n- new entry/' CHANGELOG.md; }
src() { echo x >> src/a.cs; }
case_ src-no-changelog            1 ""            src
case_ src-with-entry              0 ""            bash -c "$(declare -f add_entry src); src; add_entry"
case_ test-only                   0 ""            bash -c 'echo x >> test/a.cs'
case_ docs-md-only                0 ""            bash -c 'echo x > docs/a.md'
case_ src-with-label              0 no-changelog  src
case_ other-label-no-effect       1 foo,bar       src
case_ unicode-path                1 ""            bash -c 'echo x > "src/café.cs"'
case_ rename-out-of-src           1 ""            git mv src test/moved
case_ devconfigs-pointer          1 ""            git -C DevConfigs -c user.email=t@t -c user.name=t commit -q --allow-empty -m s2
case_ workflow-only               1 ""            bash -c 'mkdir -p .github/workflows; echo x > .github/workflows/x.yml'
case_ gate-script-only            1 ""            bash -c 'mkdir -p .github/scripts; echo x > .github/scripts/x.sh'
case_ gitversion-only             1 ""            bash -c 'echo x > GitVersion.yml'
case_ entry-added-below-release   1 ""            bash -c "$(declare -f src); src; echo '- late' >> CHANGELOG.md"
case_ released-line-deleted       1 ""            bash -c "$(declare -f src); src; sed -i '/old one/d' CHANGELOG.md"
case_ unreleased-line-deleted     1 ""            bash -c "$(declare -f src); src; sed -i '/existing entry/d' CHANGELOG.md"
case_ changelog-deleted           1 ""            bash -c "$(declare -f src); src; git rm -q CHANGELOG.md"
case_ release-cut-above-history   0 ""            bash -c "$(declare -f src); src; sed -i 's/^## \[0.3.0\]/- cut entry\n\n## [0.4.0] - 2026-02-01\n\n&/' CHANGELOG.md"
exit $fail
