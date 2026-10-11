# 0003. NuGet vulnerability auditing runs on every build

Status: accepted

## Context

NuGet can audit packages for known vulnerabilities during restore (`NuGetAudit`). It is tempting to switch the audit off on build servers (`NuGetAudit=false` when `IsServerBuild` is set) to save time and to avoid builds that fail because of an advisory published after the code was written. Doing so would hide exactly the findings the audit exists to show: a library that ships to many consumers must not publish a vulnerable dependency unnoticed.

## Decision

Auditing is always on, locally and in CI, for direct and transitive packages. `DevConfigs/Directory.Build.props` sets `NuGetAudit` to `true` and `NuGetAuditMode` to `all`. Because warnings are treated as errors repository-wide, an NU190x advisory fails the build.

The build scripts do not override these properties for restore, build or pack. A test in `test/FEx.Building.Tests/BuildSettingsTests.cs` pins that none of those command settings carry a `NuGetAudit` property.

An advisory is resolved by updating the package, or, when no fixed version exists, by an explicit and justified suppression.

## Consequences

- A new advisory can turn a green branch red without any code change. The fix is a package update, tracked like any other work.
- Local builds and CI behave the same, so a vulnerable dependency is found before a pull request, not after.
- The audit needs network access to the NuGet vulnerability feed during restore.
