---
phase: 3
slug: code-quality-dead-code
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 |
| **Config file** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj`, `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.UnitTests`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 3-01-01 | 01 | 1 | QUAL-01 | build | `dotnet build` | ✅ | ⬜ pending |
| 3-01-02 | 01 | 1 | QUAL-02 | build+grep | `dotnet build && grep -r "UpdateQuestPropertiesAsync" --include="*.cs" EuphoriaInn.Domain EuphoriaInn.Repository` | ✅ | ⬜ pending |
| 3-01-03 | 01 | 1 | QUAL-03 | build+grep | `dotnet build && grep -r "SignupRole == 1" --include="*.cs" EuphoriaInn.Domain` | ✅ | ⬜ pending |
| 3-01-04 | 01 | 1 | QUAL-04 | build+grep | `dotnet build && grep -r "IsSameDateTime" --include="*.cs" EuphoriaInn.Domain` | ✅ | ⬜ pending |
| 3-01-05 | 01 | 1 | QUAL-05 | build+fs | `dotnet build && ls EuphoriaInn.Service/ViewModels/CharacterViewModels/` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| `SecurityConfiguration.cs` deleted and `Security` key absent from appsettings.json | QUAL-01 | File deletion and config key removal require manual verification | Check `ls EuphoriaInn.Domain/` for absence of SecurityConfiguration.cs; check appsettings.json for absence of `"Security":` key |
| App still starts and authentication works | QUAL-01 | Runtime behavior post-deletion | Run `dotnet run --project EuphoriaInn.Service` and verify login works |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
