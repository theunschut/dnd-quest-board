---
phase: 12
slug: mobile-infrastructure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + Microsoft.AspNetCore.Mvc.Testing 8.0.11 |
| **Config file** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests --filter "Category=Mobile"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.IntegrationTests --filter "Category=Mobile"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 12-01-01 | 01 | 1 | INFRA-01 | — | Mobile UA sets HttpContext.Items["IsMobile"]=true | unit | `dotnet test --filter "MobileDetectionMiddleware"` | ❌ W0 | ⬜ pending |
| 12-01-02 | 01 | 1 | INFRA-03 | — | PopulateValues writes to context.Values, not ExpandViewLocations | unit | `dotnet test --filter "MobileViewLocationExpander"` | ❌ W0 | ⬜ pending |
| 12-02-01 | 02 | 1 | INFRA-02 | — | Mobile UA resolves View.Mobile.cshtml; desktop resolves View.cshtml | integration | `dotnet test --filter "MobileViewResolution"` | ❌ W0 | ⬜ pending |
| 12-02-02 | 02 | 1 | INFRA-04 | — | Mobile response contains offcanvas nav element | integration | `dotnet test --filter "MobileLayoutOffcanvas"` | ❌ W0 | ⬜ pending |
| 12-02-03 | 02 | 1 | INFRA-05 | — | Desktop response contains no offcanvas element (parity check) | integration | `dotnet test --filter "DesktopLayoutParity"` | ❌ W0 | ⬜ pending |
| 12-03-01 | 03 | 2 | INFRA-06 | — | mobile.css link present in mobile response; touch targets min-height 44px | integration + file | `dotnet test --filter "MobileCss"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileDetectionMiddlewareTests.cs` — stubs for INFRA-01
- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileViewLocationExpanderTests.cs` — stubs for INFRA-02, INFRA-03
- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileLayoutTests.cs` — stubs for INFRA-04, INFRA-05
- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileCssTests.cs` — stubs for INFRA-06

*Existing test infrastructure (WebApplicationFactoryBase + HttpClient) covers all phase requirements — no framework install needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Offcanvas nav renders all auth-conditional sections correctly (admin, DM tools, player links) | INFRA-04 | Auth state testing requires session — complex to automate fully | Log in as Admin, DM, and regular player on mobile UA; verify respective nav items appear in offcanvas drawer |
| D&D theme baseline renders correctly (Cinzel font, dark navbar) | INFRA-06 | Visual rendering not automatable | Open any page from mobile UA in browser; verify Cinzel font and dark navbar palette |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
