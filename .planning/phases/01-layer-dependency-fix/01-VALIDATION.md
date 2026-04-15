---
phase: 1
slug: layer-dependency-fix
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-15
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| **Config file** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~15 seconds (build) / ~30 seconds (full suite) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` — verify 0 errors before proceeding
- **After every plan wave:** Run `dotnet test` — all 16 unit tests must pass
- **Before `/gsd:verify-work`:** `dotnet build` green + `dotnet test` 16/16 passing

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 1-01-01 | 01 | 1 | ARCH-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 1-01-02 | 01 | 1 | ARCH-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 1-02-01 | 02 | 2 | ARCH-02, ARCH-03 | build | `dotnet build EuphoriaInn.Domain/` | ✅ | ⬜ pending |
| 1-02-02 | 02 | 2 | ARCH-04 | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

No new test files needed — this phase is a structural refactor with no new business behavior. Existing 16 unit tests cover pure domain model behavior and will continue passing throughout. Primary validation is `dotnet build` green at each step.

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| No assembly scanning in Program.cs | ARCH-04 | Code review — no automated assertion for absence of pattern | Inspect Program.cs AutoMapper registration: must use `AddProfile<T>()` explicitly, not `AddAutoMapper(typeof(SomeAnchor))` or `AppDomain.CurrentDomain.GetAssemblies()` |
| Domain.csproj has no ProjectReference to Repository | ARCH-02 | csproj inspection | `grep -r "EuphoriaInn.Repository" EuphoriaInn.Domain/EuphoriaInn.Domain.csproj` must return no results |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
