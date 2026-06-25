---
phase: 14
slug: calendar
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 14 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| **Config file** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -x` |
| **Full suite command** | `dotnet test EuphoriaInn.IntegrationTests` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -x`
- **After every plan wave:** Run `dotnet test EuphoriaInn.IntegrationTests`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 14-01-01 | 01 | 0 | CAL-01 through CAL-05 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar"` | ❌ W0 | ⬜ pending |
| 14-02-01 | 02 | 1 | CAL-01 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_RendersAgendaList"` | ❌ W0 | ⬜ pending |
| 14-02-02 | 02 | 1 | CAL-02 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_AgendaEntryContainsDayLabelAndTime"` | ❌ W0 | ⬜ pending |
| 14-02-03 | 02 | 1 | CAL-03 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_DesktopUserAgent_DoesNotRenderAgendaList"` | ❌ W0 | ⬜ pending |
| 14-02-04 | 02 | 1 | CAL-04 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_AgendaEntryLinksToDetails"` | ❌ W0 | ⬜ pending |
| 14-02-05 | 02 | 1 | CAL-01 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_LoadsMobileCssLink"` | ❌ W0 | ⬜ pending |
| 14-03-01 | 03 | 1 | CAL-05 | — | N/A | integration | `dotnet test --filter "FullyQualifiedName~MobileCalendar_MobileUserAgent_CalendarPartialRendersVoteButtons"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] New test methods in `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — stubs for CAL-01 through CAL-05 (6 test methods total, all starting RED)
- [ ] CAL-05 requirement text added to `.planning/REQUIREMENTS.md`

*Existing test infrastructure (`MobileViewsTests.cs`, `TestDataHelper.cs`, `WebApplicationFactoryBase`) fully covers all Phase 14 tests — no new test files or config needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Vote button active-state highlighting (pre-checked radio) | CAL-05 | Bootstrap `.btn-check:checked` CSS can only be confirmed visually | Open Quest Details on a mobile UA for a signed-up user; verify the current vote button appears filled/highlighted |
| Tap navigation feel (44px touch target) | CAL-04 | Min-height enforced by CSS; real touch feel requires device/DevTools | Open Calendar agenda on mobile DevTools; tap quest entry; verify navigation and target size |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
