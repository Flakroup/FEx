#!/usr/bin/env bash
# Asserts exit code AND message of check-changelog.sh per scenario. Every case starts from a fresh copy
# of one fixture repo, and each rule has a case only that rule can fail.
set -uo pipefail
script=$(cd "$(dirname "$0")" && pwd)/check-changelog.sh
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
tpl=$tmp/tpl
mkdir "$tpl" && cd "$tpl"
git init -q . && git config user.email t@t && git config user.name t && git config advice.addEmbeddedRepo false
mkdir src test docs
echo a > src/a.cs; echo a > test/a.cs
git init -q DevConfigs && git -C DevConfigs -c user.email=t@t -c user.name=t commit -q --allow-empty -m s1
cat > CHANGELOG.md <<'L'
# Changelog

## [Unreleased]

### Added

- existing entry

## [0.3.0] - 2026-01-01

- old one
- old two
L
git add -A && git commit -qm base && git branch -q base
fail=0
# case_ <name> <expected-exit> <expected-message-substring> <labels> <mutation-fn>
# Mutation functions run inside the case repo; `on_base` mutations are applied to the base branch first.
case_() {
  local name=$1 want=$2 msg=$3 labels=$4 mut=$5 basemut=${6:-} premut=${7:-}
  rm -rf "$tmp/r" && cp -a "$tpl" "$tmp/r" && cd "$tmp/r"
  if [[ -n "$premut" ]]; then git checkout -q base; ( "$premut" ) >/dev/null 2>&1; git add -A; git commit -qm pre; fi
  git checkout -q -B work base
  ( "$mut" ) >/dev/null 2>&1
  git add -A; git commit -qm work --allow-empty
  local b=base
  if [[ -n "$basemut" ]]; then
    git checkout -q base; ( "$basemut" ) >/dev/null 2>&1; git add -A; git commit -qm advance; git checkout -q work
  fi
  PR_LABELS=$labels bash "$script" "$b" work >"$tmp/out" 2>&1; local got=$?
  if [[ $got -eq $want ]] && grep -qF -- "$msg" "$tmp/out"; then echo "ok   $name (exit $got)"
  else echo "FAIL $name: want $want '$msg', got $got"; cat "$tmp/out"; fail=1; fi
}
src()        { echo x >> src/a.cs; }
entry()      { sed -i 's/^- existing entry$/&\n- new entry/' CHANGELOG.md; }
m_src_only() { src; }
m_src_entry() { src; entry; }
m_test()     { echo x >> test/a.cs; }
m_docs()     { echo x > docs/a.md; }
m_unicode()  { echo x > "src/café.cs"; }
m_rename()   { git mv src test/moved; }
m_devcfg()   { git -C DevConfigs -c user.email=t@t -c user.name=t commit -q --allow-empty -m s2; }
m_workflow() { mkdir -p .github/workflows; echo x > .github/workflows/x.yml; }
m_script()   { mkdir -p .github/scripts; echo x > .github/scripts/x.sh; }
m_gitver()   { echo x > GitVersion.yml; }
m_global()   { echo x > global.json; }
m_released_eof()    { src; entry; echo '- rewritten history' >> CHANGELOG.md; }
m_released_del()    { src; entry; sed -i '/old one/d' CHANGELOG.md; }
m_released_docs()   { echo '- late' >> CHANGELOG.md; }
m_unreleased_del()  { src; sed -i '/existing entry/d' CHANGELOG.md; }
m_cl_deleted_docs() { m_docs; git rm -q CHANGELOG.md; }
m_trailing_space()  { src; sed -i 's/^- existing entry$/& /' CHANGELOG.md; }
m_heading_only()    { src; sed -i 's/^- existing entry$/&\n\n### Fixed/' CHANGELOG.md; }
m_copy_released()   { src; sed -i 's/^- existing entry$/&\n- old one/' CHANGELOG.md; }
m_lower_heading()   { src; entry; sed -i 's/^## \[Unreleased\]/## [unreleased]/' CHANGELOG.md; }
m_no_unreleased()   { src; sed -i '/^## \[Unreleased\]/d' CHANGELOG.md; }
p_vprefix()         { sed -i 's/^## \[0.3.0\]/## [v0.3.0]/' CHANGELOG.md; }
m_cut()             { src; entry; sed -i 's/^## \[0.3.0\]/## [0.4.0] - 2026-02-01\n\n&/' CHANGELOG.md; }
b_cut()             { sed -i 's/^## \[0.3.0\]/## [0.4.0] - 2026-02-01\n\n- cut\n\n&/' CHANGELOG.md; }
E="Add a new '- ' bullet"; A="append-only"; D="was deleted"; N="no '## [Unreleased]'"
case_ src-no-changelog          1 "$E" ""             m_src_only
case_ src-with-entry            0 "entry found" ""    m_src_entry
case_ test-only                 0 "no entry needed" "" m_test
case_ docs-md-only              0 "no entry needed" "" m_docs
case_ src-with-label            0 "no-changelog" no-changelog m_src_only
case_ other-label-no-effect     1 "$E" "foo,bar"      m_src_only
case_ unicode-path              1 "$E" ""             m_unicode
case_ rename-out-of-src         1 "$E" ""             m_rename
case_ devconfigs-pointer        1 "$E" ""             m_devcfg
case_ workflow-only             1 "$E" ""             m_workflow
case_ gate-script-only          1 "$E" ""             m_script
case_ gitversion-only           1 "$E" ""             m_gitver
case_ global-json-only          1 "$E" ""             m_global
case_ released-edit-at-eof      1 "$A" ""             m_released_eof
case_ released-line-deleted     1 "$A" ""             m_released_del
case_ released-edit-docs-only   1 "$A" ""             m_released_docs
case_ unreleased-line-deleted   1 "$E" ""             m_unreleased_del
case_ changelog-deleted-docs    1 "$D" ""             m_cl_deleted_docs
case_ trailing-space-edit       1 "$E" ""             m_trailing_space
case_ heading-only              1 "$E" ""             m_heading_only
case_ copy-of-released-line     1 "$E" ""             m_copy_released
case_ lowercase-heading-entry   0 "entry found" ""    m_lower_heading
case_ no-unreleased-section     1 "$N" ""             m_no_unreleased
case_ v-prefixed-release-edit   1 "$A" ""             m_released_del "" p_vprefix
case_ release-cut-above-history 0 "entry found" ""    m_cut
case_ branched-before-cut       0 "entry found" ""    m_src_entry b_cut
exit $fail
