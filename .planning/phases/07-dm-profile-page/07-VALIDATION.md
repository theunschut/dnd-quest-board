---
phase: 7
slug: dm-profile-page
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
---

# Phase 7 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 2.2.2 (unit) + xUnit v3 via `Microsoft.AspNetCore.Mvc.Testing` (integration) |
| **Config file** | none — uses SDK test discovery |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests --no-build -x` |
| **Full suite command** | `dotnet test --no-build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.UnitTests --no-build -x`
- **After every plan wave:** Run `dotnet test --no-build`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 7-01-01 | 01 | 0 | DMPRO-01 | — | N/A | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "DungeonMasterController" -x` | ❌ W0 | ⬜ pending |
| 7-01-02 | 01 | 0 | DMPRO-02 | — | N/A | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "DungeonMasterController" -x` | ❌ W0 | ⬜ pending |
| 7-01-03 | 01 | 0 | DMPRO-03 | — | DM cannot edit another DM's profile without Admin role | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "DungeonMasterController" -x` | ❌ W0 | ⬜ pending |
| 7-01-04 | 01 | 0 | DMPRO-04 | — | N/A | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "PlayersController" -x` | ❌ W0 | ⬜ pending |
| 7-01-05 | 01 | 0 | DMPRO-05 | — | N/A | Manual | n/a — verified by schema inspection | ❌ manual | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.IntegrationTests/Controllers/DungeonMasterControllerIntegrationTests.cs` — stubs for DMPRO-01, DMPRO-02, DMPRO-03
- [ ] `EuphoriaInn.IntegrationTests/Controllers/PlayersControllerIntegrationTests.cs` — stubs for DMPRO-04
- [ ] No framework install needed — existing test infrastructure is sufficient

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| EF migration adds DungeonMasterProfiles and DungeonMasterProfileImages tables | DMPRO-05 | Schema creation verified at runtime, not via unit test | Run `dotnet run --project EuphoriaInn.Service`, confirm app starts without migration errors, inspect DB schema for both new tables |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
