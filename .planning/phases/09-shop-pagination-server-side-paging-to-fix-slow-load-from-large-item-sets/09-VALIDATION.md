---
phase: 9
slug: shop-pagination-server-side-paging-to-fix-slow-load-from-large-item-sets
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-21
---

# Phase 9 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + FluentAssertions |
| **Config file** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj`, `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
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
| 9-01-01 | 01 | 1 | Repository layer | unit | `dotnet test EuphoriaInn.UnitTests --filter "Shop"` | ❌ W0 | ⬜ pending |
| 9-01-02 | 01 | 1 | Service layer | unit | `dotnet test EuphoriaInn.UnitTests --filter "Shop"` | ❌ W0 | ⬜ pending |
| 9-02-01 | 02 | 2 | Controller + view | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "Shop"` | ❌ W0 | ⬜ pending |
| 9-02-02 | 02 | 2 | Pager UI rendering | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "Shop"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.UnitTests/Shop/ShopPaginationServiceTests.cs` — unit tests for paged service method
- [ ] `EuphoriaInn.IntegrationTests/Shop/ShopPaginationIntegrationTests.cs` — integration tests for controller pagination behavior

*Existing test infrastructure covers the framework; Wave 0 adds phase-specific stubs.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Page reset on filter change | D-07 | Requires browser interaction with hidden input | Apply rarity filter, confirm URL has `page=1` |
| Empty state rendering | D-06 | Requires specific data setup | Search for non-existent item, verify empty state message shown |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
