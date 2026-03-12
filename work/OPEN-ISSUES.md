# FEx Open Issues

**Last Updated**: 2026-03-13

---

## Active Issues

### ISSUE-002: Build warnings (~53)

**Severity**: Low
**Scope**: Entire solution

Breakdown by analyzer:
- IDISP (IDisposable) - missing dispose calls, unsealed classes
- VSTHRD (Threading) - using `Dispatcher.Invoke` instead of `JoinableTaskFactory`
- CA (Code Analysis) - naming, design suggestions
- SI (StrongInject) - registration warnings
- REFL (Reflection) - reflection usage patterns
- xUnit1051 - missing `CancellationToken` in tests

**Strategy**: Fix bottom-up by layer after feature work is complete.
