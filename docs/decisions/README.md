# Architecture decision records

Each record states the context of a decision, the decision itself and its consequences. A record is never rewritten to hide a change of mind: a decision that is reversed gets a new record that supersedes the old one.

| No. | Decision | Status |
|---|---|---|
| [0001](0001-api-surface-served-by-external-mcp-server.md) | The API surface is served by an external MCP server | Accepted |
| [0002](0002-library-neutral-json-serializer.md) | JSON serialization goes through a library-neutral interface | Accepted |
| [0003](0003-nuget-audit-runs-on-every-build.md) | NuGet vulnerability auditing runs on every build | Accepted |
| [0004](0004-versioning-and-stable-release-gate.md) | Versioning and the stable-release gate | Accepted |
| [0005](0005-stable-release-criterion.md) | Criterion for a stable release | Accepted |
| [0006](0006-devconfigs-shared-layer-holds-configuration-only.md) | The shared DevConfigs settings layer holds configuration only | Accepted |

See also the [architecture overview](../architecture.md) and the [namespace mapping](../migration/namespace-mapping.md).
