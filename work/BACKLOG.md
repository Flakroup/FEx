# FEx Backlog

**Last Updated**: 2026-03-13

---

## Backlog

### SPEC-004: Warning Cleanup

**Priority**: Low
**Estimate**: 3-5 days

Systematically fix ~53 build warnings by layer (bottom-up).

**Categories**:
- IDISP: IDisposable patterns
- VSTHRD: Threading/async
- REFL: Reflection
- CA: Code Analysis
- SI: StrongInject
- CS: Compiler suggestions

**Strategy**: Fix by layer, Agnostics first, then Core, then higher.

---

### SPEC-007: Sample Apps Validation

**Priority**: Medium
**Estimate**: 1-2 hours

Run all three sample apps and verify:
- [ ] FEx.Sample.WebAPI starts and serves requests
- [ ] FEx.Sample.WPF starts with correct UI
- [ ] FEx.Sample.Avalonia starts without threading errors

---

### SPEC-008: FlakEssentials Legacy Archive

**Priority**: Low
**Estimate**: 1 day
**Repo**: `gitlab.com/flakroup/flakessentials` (standalone)

Remaining legacy projects (KeyVault, SQLite, IE, TelerikEx, RadTreeViewEx, Rest, AvalonEdit, WpfEx icon system) live in standalone FlakEssentials repo. When ready:
- Replace FEx ProjectReferences with NuGet package references
- Each project can be extracted to its own repo if needed
- Add ARCHIVED.md explaining final state
