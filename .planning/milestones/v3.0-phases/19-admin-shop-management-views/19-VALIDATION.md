---
phase: 19
slug: admin-shop-management-views
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-25
---

# Phase 19 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit + FluentAssertions (existing) |
| **Config file** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileViewsTests" -q` |
| **Full suite command** | `dotnet test EuphoriaInn.IntegrationTests -q` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** `dotnet build EuphoriaInn.Service --no-restore -q`
- **After every plan wave:** `dotnet test EuphoriaInn.IntegrationTests -q`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 19-??-?? | TBD | 0 | ADMIN-01 | — | N/A | stub | `dotnet test --filter "GetMobilePage_AdminUsers"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | ADMIN-01 | — | N/A | stub | `dotnet test --filter "GetMobilePage_AdminEditUser"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | ADMIN-01 | — | N/A | stub | `dotnet test --filter "GetMobilePage_AdminQuests"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | ADMIN-02 | — | N/A | stub | `dotnet test --filter "GetMobilePage_ShopManagementIndex"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | ADMIN-02 | — | N/A | stub | `dotnet test --filter "GetMobilePage_ShopManagementCreate"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | ADMIN-02 | — | N/A | stub | `dotnet test --filter "GetMobilePage_ShopManagementEdit"` | ❌ W0 | ⬜ pending |
| 19-??-?? | TBD | 0 | SHOPMGMT-01 | — | N/A | stub | `dotnet test --filter "GetMobilePage_ShopDetails"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — append 7 Phase 19 test stubs (Admin/Users, Admin/EditUser, Admin/Quests, ShopManagement/Index, ShopManagement/Create, ShopManagement/Edit, Shop/Details)
- [ ] `REQUIREMENTS.md` — add ADMIN-01, ADMIN-02, SHOPMGMT-01 requirement entries

*Notes:*
- *Admin tests require Admin-role user (`CreateAuthenticatedAdminClientAsync` or `roles: new[] { "Admin" }`)*
- *ShopManagement tests require DungeonMaster-role user (`roles: new[] { "DungeonMaster" }`)*
- *ShopManagement/Edit test requires a seeded ShopItem (for the route param)*
- *Shop/Details test requires any authenticated user and a seeded Published ShopItem*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| No horizontal scroll on Admin/Users mobile view at 390px | ADMIN-01 | Visual/layout assertion | Open on mobile or DevTools 390px; confirm no horizontal overflow |
| No horizontal scroll on ShopManagement pages at 390px | ADMIN-02 | Visual/layout assertion | Open on mobile or DevTools 390px; confirm no horizontal overflow |
| Shop/Details single-column layout on mobile | SHOPMGMT-01 | Visual/layout assertion | Open on mobile or DevTools 390px; confirm single-column card layout |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
