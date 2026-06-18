---
phase: 11
slug: navigation-token-generation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 11 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit.v3 3.2.2 + FluentAssertions 8.9.0 |
| **Unit config** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| **Integration config** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests` |
| **Full suite command** | `dotnet test` (from solution root) |
| **Estimated runtime** | ~5 seconds (unit), ~30 seconds (full) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.UnitTests`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 11-01-01 | 01 | 1 | TOKEN-01, TOKEN-02 | — | Canonical MAC message alphabetical order | unit | `dotnet test EuphoriaInn.UnitTests --filter IntegrationTokenService` | ❌ W0 | ⬜ pending |
| 11-01-02 | 01 | 1 | TOKEN-03, TOKEN-04 | — | Expiry 300s, username lowercase | unit | `dotnet test EuphoriaInn.UnitTests --filter IntegrationTokenService` | ❌ W0 | ⬜ pending |
| 11-01-03 | 01 | 1 | TOKEN-05 | — | Returns redirect or 404 | integration | `dotnet test EuphoriaInn.IntegrationTests --filter LaunchOmphalos` | ❌ W0 | ⬜ pending |
| 11-01-04 | 01 | 1 | NAV-03, NAV-04 | — | ViewBag.ShowOmphalosButton gating | integration | `dotnet test EuphoriaInn.IntegrationTests --filter LaunchOmphalos` | ❌ W0 | ⬜ pending |
| 11-02-01 | 02 | 2 | NAV-01, NAV-02 | — | Navbar link only when configured | integration | `dotnet test EuphoriaInn.IntegrationTests --filter OmphalosNavItem` | ❌ W0 | ⬜ pending |
| 11-02-02 | 02 | 2 | NAV-05 | — | No UI when disabled | integration | `dotnet test EuphoriaInn.IntegrationTests --filter OmphalosNavItem` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` — stubs for TOKEN-01 through TOKEN-04
- [ ] `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` — stubs for TOKEN-05, NAV-03, NAV-04, NAV-05
- [ ] `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` — new ViewComponent class (Wave 1 deliverable, not test stub)
- [ ] `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` — new ViewComponent view (Wave 2 deliverable)

*Existing test infrastructure in EuphoriaInn.UnitTests/ and EuphoriaInn.IntegrationTests/ covers xunit + FluentAssertions setup — no new framework installation needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| "Open Omphalos" link appears in DM navbar dropdown and opens new tab | NAV-01, NAV-02 | Rendered HTML in layout requires browser/snapshot test; integration test covers smoke only | Log in as DM, check navbar dropdown shows "Open Omphalos" link; click to verify `target="_blank"` opens Omphalos URL |
| "Open Session Notes" button visible on Details/Manage pages for DM | NAV-03, NAV-04 | ViewBag rendering in Razor view not easily testable in xunit integration tests | Log in as DM, navigate to Quest Details and Quest Manage pages; verify button appears when configured |
| No Omphalos UI visible when integration disabled | NAV-05 | Requires toggling Admin Settings and re-checking pages | Disable integration in Admin Settings, navigate to both quest pages and navbar — verify no Omphalos elements |
| Clicking "Open Session Notes" redirects with correct signed URL structure | TOKEN-01–05 | End-to-end requires a running Omphalos instance for full validation | Integration test covers redirect shape; full end-to-end tested at Phase 12 convergence |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
