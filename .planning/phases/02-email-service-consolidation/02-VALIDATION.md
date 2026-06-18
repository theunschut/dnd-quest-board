---
phase: 2
slug: email-service-consolidation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-17
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + FluentAssertions 8.8.0 + NSubstitute 5.3.0 |
| **Config file** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~15 seconds |

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
| 2-01-01 | 01 | 1 | EMAIL-01 | unit | `dotnet test EuphoriaInn.UnitTests --filter EmailSettings` | ❌ W0 | ⬜ pending |
| 2-01-02 | 01 | 1 | EMAIL-02 | unit | `dotnet test EuphoriaInn.UnitTests --filter EmailService` | ❌ W0 | ⬜ pending |
| 2-02-01 | 02 | 1 | CTRL-01, CTRL-02 | unit | `dotnet test EuphoriaInn.UnitTests --filter QuestService` | ❌ W0 | ⬜ pending |
| 2-02-02 | 02 | 2 | CTRL-03 | unit | `dotnet test EuphoriaInn.UnitTests --filter ShopController` | ❌ W0 | ⬜ pending |
| 2-02-03 | 02 | 2 | CTRL-04 | integration | `dotnet test EuphoriaInn.IntegrationTests --filter Finalize` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` — stubs for EMAIL-01, EMAIL-02
- [ ] `EuphoriaInn.UnitTests/Services/QuestServiceTests.cs` — stubs for CTRL-01, CTRL-02
- [ ] `EuphoriaInn.UnitTests/Controllers/ShopControllerTests.cs` — stubs for CTRL-03
- [ ] `EuphoriaInn.IntegrationTests/QuestFinalizeTests.cs` — stubs for CTRL-04

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Email delivered to selected players after finalize | CTRL-01 | Requires live SMTP | Log in as DM, finalize quest with players, verify inbox receipt |
| Notification email contains real AppUrl | EMAIL-03 | Requires live SMTP | Check email body for correct application URL |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
