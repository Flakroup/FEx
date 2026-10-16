# 0004. Versioning and the stable-release gate

Status: accepted

## Context

FEx publishes a pre-release package to nuget.org on every merge to `develop` and a stable package from `main`. Versions come from GitVersion in continuous-deployment mode, derived from git tags.

An early build defect packed a `develop` snapshot as the stable version 0.1.1 and left a `v0.1.1-alpha` tag behind. That tag anchored every later pre-release at `0.1.1-alpha.N`, and all of them sort below the published stable 0.1.1, so no consumer could resolve a newer pre-release. A published nuget.org version cannot be replaced, and unlisting it does not remove it from version resolution for everyone.

## Decision

- `GitVersion.yml` sets `next-version` as a floor for the version base where no tag reaches. It is currently `0.3.0`; the file explains why. Moving the base forward puts new pre-releases above the phantom version, which deleting the tag would not.
- A stable version can only be produced from the release branch. The NUKE target `AssertStableOnlyFromReleaseBranch` fails a server build that would pack a stable version from any branch other than `main` or `master`.
- Fields that GitVersion 6 removed (`NuGetVersion*`) are not used; the build reads the supported variables only.
- Publishing pushes with `--skip-duplicate`, so re-running a partly failed publish does not fail on packages that are already on the feed.
- CI splits the work into a build job, which compiles, runs coverage and packs, and a short publish job, which only pushes and tags. A failed push is retried by re-running the publish job from the retained artifact, without rebuilding.
- The phantom stable 0.1.1 stays listed. It is harmless once the floor is above it.

## Consequences

- Version numbers are determined by tags and the floor, never typed by hand.
- Raising the floor is an explicit change to `GitVersion.yml`, made when a new line starts.
- A stable release is possible only from `main`, and only after the gates in [ADR 0005](0005-stable-release-criterion.md) pass.
- Publishing is repeatable and idempotent.
