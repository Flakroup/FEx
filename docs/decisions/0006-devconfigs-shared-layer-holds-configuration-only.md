# 0006. The shared DevConfigs settings layer holds configuration only

Status: accepted

## Context

`DevConfigs` is a git submodule shared by FEx and the repositories that use it. Among other things it carries a ReSharper team-shared settings layer, `FEx.sln.DotSettings`, which each solution's own `.DotSettings` file injects. A shared layer is published to every consumer, so anything personal that lands in it is published too. IDE settings files accumulate personal state easily: recently used paths, window state, absolute file paths, migration markers that tools write back.

## Decision

The shared layer contains configuration only. It holds no personal IDE state and no absolute paths, and it has no duplicate `x:Key` entries.

This is enforced mechanically rather than by review: `DevConfigs/tools/validate_dotsettings.py` checks the file, it has its own tests, and the `DevConfigs` repository runs it in its `validate.yml` workflow. The validator tolerates the `SettingsMigration/IsMigratorApplied` markers and the byte-order mark that ReSharper's command-line tools write back, since those are tool output and not personal data.

A rule that a person has to remember is not a rule: when the shared layer needs a new constraint, it is added to the validator.

## Consequences

- A change to the shared layer that adds a path or personal state fails validation before it can be merged.
- Personal IDE preferences go in the user layer, not in `DevConfigs`.
- Every consumer of the submodule gets the same inspection and formatting settings.
