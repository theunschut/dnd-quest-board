---
phase: 13
slug: core-player-views
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 13 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 |
| **Config file** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~Mobile"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~Mobile"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 13-01-01 | 01 | 1 | HOME-01 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-01-02 | 01 | 1 | HOME-02 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-01-03 | 01 | 1 | HOME-03 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-01-04 | 01 | 1 | HOME-04 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-02-01 | 02 | 1 | QVIEW-01 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-02-02 | 02 | 1 | QVIEW-02 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |
| 13-03-01 | 03 | 1 | QVIEW-03 | — | N/A | Integration | `dotnet test --filter "FullyQualifiedName~MobileViewsTests"` | ❌ Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — stubs for HOME-01 through QVIEW-03 (all 7 requirements)

*Template from existing `MobileLayoutTests.cs` pattern: `IClassFixture<WebApplicationFactoryBase>`, `TryAddWithoutValidation("User-Agent", ...)`, `html.Should().Contain(...)` assertions.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Vote buttons are ≥44px tall in rendered browser | QVIEW-01 | CSS min-height only verifiable in a real browser rendering context | Open Quest Details on mobile DevTools emulation; inspect vote button computed height |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
