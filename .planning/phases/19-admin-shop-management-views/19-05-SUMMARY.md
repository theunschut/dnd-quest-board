---
phase: 19-admin-shop-management-views
plan: "05"
subsystem: shop-management-mobile
tags: [mobile, shop-management, razor-view, css, flat-list, authorization]
dependency_graph:
  requires:
    - 19-01 (RED test stubs — GetMobilePage_ShopManagementIndex)
  provides:
    - ShopManagement/Index.Mobile.cshtml — flat deduplicated item list with modals and auth-guarded action buttons
    - shop-management-index.mobile.css — glass card styles for outer container and per-item sub-cards
  affects:
    - EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css
tech_stack:
  added: []
  patterns:
    - Flat-list construction with DistinctBy deduplication (RESEARCH.md Pitfall 2)
    - Draft-first ordering via OrderBy(i => i.Status == ItemStatus.Draft ? 0 : 1)
    - Authorization-guarded icon-only action buttons via AuthorizationService.AuthorizeAsync
    - Glass card outer container always rendered (ensures CSS class present for integration test)
key_files:
  created:
    - EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css
  modified: []
decisions:
  - Outer shop-mgmt-index-card-mobile container always rendered (not conditional on allItems.Any()) — ensures integration test can assert CSS class present even when DM has no items yet
  - Empty state rendered inside the outer glass card as centered text with inline icon color, avoiding a separate shop-mgmt-empty-state CSS class in the per-view CSS
  - System.Linq @using added to view for DistinctBy LINQ extension method
metrics:
  duration: "~3 minutes"
  completed: "2026-06-25"
  tasks_completed: 1
  files_modified: 2
status: complete
---

# Phase 19 Plan 05: ShopManagement/Index Mobile View Summary

**One-liner:** ShopManagement Index mobile view with flat deduplicated Draft-first item list, status/auth-guarded icon-only action buttons, Deny and Bulk Actions modals, and matching per-view CSS.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create shop-management-index.mobile.css + Index.Mobile.cshtml | bfa3de0 | EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml, EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css |

## What Was Built

### CSS — shop-management-index.mobile.css

- Header comment and no-media-query note
- `.shop-mgmt-index-card-mobile` — glass card outer container (16px padding, backdrop-filter: blur(15px), border, border-radius: 12px, box-shadow)
- `.shop-mgmt-item-card` — nested per-item sub-card (12px padding, glass values, 10px bottom margin)
- Parchment text for `h5`/`.parchment-text`/item name (`#F4E4BC` with text-shadow)
- Faded parchment for `small`/price (`rgba(244, 228, 188, 0.7)`)
- `.badge { text-shadow: none !important; }` scoped to item card
- `.shop-mgmt-item-card .btn-sm { padding: 4px 8px; }` — icon-only button sizing for 320px screens
- No `.item-rarity` or `.rarity-*` definitions (reused from shop.mobile.css)
- No `@media` queries

### View — ShopManagement/Index.Mobile.cshtml

- `@using EuphoriaInn.Service.ViewModels.ShopViewModels`, `@using EuphoriaInn.Domain.Enums`, `@using System.Linq`
- `@model ShopManagementIndexViewModel`
- No `Layout =`, no `@inject IAuthorizationService`
- `@section Styles` linking `~/css/shop-management-index.mobile.css`
- Flat list construction: `Model.ItemsForReview.Concat(Model.MyItems).Concat(Model.AllOtherItems).OrderBy(i => i.Status == ItemStatus.Draft ? 0 : 1).ThenBy(i => i.Name).DistinctBy(i => i.Id).ToList()`
- Top action links: "Add New Item" (btn-success w-100) and "View Shop" (btn-outline-secondary w-100)
- Outer `shop-mgmt-index-card-mobile` div always rendered
- Per-item `shop-mgmt-item-card` sub-cards with rarity badge, name, price, status badge
- Status badge switch: Draft=bg-warning text-dark "Pending Review", Published=bg-success "Active", Archived=bg-secondary, Denied=bg-danger
- Icon-only action button row with `aria-label` on all buttons: View (fa-eye), Edit (fa-edit), Archive/Reopen (POST form with AntiForgeryToken), Deny (modal trigger, Draft only), Publish (Draft + AdminOnly), Delete (Denied or non-Draft non-Denied + AdminOnly)
- Empty state rendered inside outer card (no separate container)
- Deny Item modal and Bulk Actions stub modal copied verbatim from desktop
- `@section Scripts` with `bulkAction()` and `denyModal show.bs.modal` listener copied verbatim from desktop

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Outer container always rendered to satisfy integration test**
- **Found during:** Task 1 — first test run
- **Issue:** Plan specified outer `shop-mgmt-index-card-mobile` div only inside `@if (allItems.Any())` block. Integration test seeds no items (DM with no shop items yet), so the CSS class was never emitted into HTML — test assertion `html.Should().Contain("shop-mgmt-index-card-mobile")` failed.
- **Fix:** Moved the `shop-mgmt-index-card-mobile` outer div outside the conditional; placed the `@if (allItems.Any()) { foreach ... } else { empty state }` inside it. The outer container is always present in the rendered output.
- **Files modified:** EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml
- **Commit:** bfa3de0 (same commit — fixed before committing)

## Verification Results

- `dotnet build EuphoriaInn.Service` — Build succeeded, 0 C# errors
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~GetMobilePage_ShopManagementIndex"` — Passed (1/1)
- `shop-mgmt-index-card-mobile` present in view — confirmed
- `shop-mgmt-item-card` present in view — confirmed
- `~/css/shop-management-index.mobile.css` linked — confirmed
- `.DistinctBy(i => i.Id)` present — confirmed
- `ItemsForReview`, `MyItems`, `AllOtherItems` all concatenated — confirmed
- `AuthorizeAsync(User, "AdminOnly")` appears twice (Publish guard, Delete guard) — confirmed
- `#denyModal` element and `data-bs-target="#denyModal"` present — confirmed
- `aria-label="View item"`, `aria-label="Edit item"`, `aria-label="Deny item"`, `aria-label="Delete item"` all present — confirmed
- `grep -c "\.item-rarity" shop-management-index.mobile.css` returns 0 — confirmed
- `Layout =` not present — confirmed
- `@inject IAuthorizationService` not present — confirmed
- `@media` queries in CSS: 0 — confirmed

## Known Stubs

None — all item list rendering is wired to live model data (ShopManagementIndexViewModel collections). No hardcoded or placeholder values.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| T-19-11 mitigated | Index.Mobile.cshtml | All action POST forms carry @Html.AntiForgeryToken() copied from desktop |
| T-19-12 mitigated | Index.Mobile.cshtml | Publish and Delete buttons gated in Razor by (await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded |
| T-19-13 mitigated | Index.Mobile.cshtml | All @item.Name and data-item-name usages auto-encoded by Razor; no Html.Raw |

No new security-relevant surface beyond the plan's threat model.

## Self-Check: PASSED

- [x] `EuphoriaInn.Service/Views/ShopManagement/Index.Mobile.cshtml` created — confirmed
- [x] `EuphoriaInn.Service/wwwroot/css/shop-management-index.mobile.css` created — confirmed
- [x] Commit bfa3de0 exists — confirmed (`git log --oneline -1`)
- [x] Integration test GetMobilePage_ShopManagementIndex GREEN — confirmed (Passed: 1)
