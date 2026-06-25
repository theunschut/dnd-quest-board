---
phase: 19-admin-shop-management-views
plan: "04"
subsystem: shop-management-mobile-forms
tags: [mobile, shop-management, forms, cshtml, css, admin]
dependency_graph:
  requires:
    - 19-01 (RED test stubs for ShopManagementCreate and ShopManagementEdit)
  provides:
    - ShopManagement/Create.Mobile.cshtml — single-column create form for mobile
    - ShopManagement/Edit.Mobile.cshtml — single-column edit form with status alert for mobile
    - shop-management-create.mobile.css — glass card + availability-window spacing
    - shop-management-edit.mobile.css — glass card + status alert spacing
  affects:
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs (GetMobilePage_ShopManagementCreate and GetMobilePage_ShopManagementEdit now GREEN)
tech_stack:
  added: []
  patterns:
    - Glass-card mobile view pattern (shop-mgmt-*-card-mobile, 16px padding, backdrop-filter blur)
    - Stripped updatePriceSuggestion() — button refs removed, rarity hint retained
    - toggleAvailabilityWindow() carried verbatim from desktop
    - @@keyframes legendary-glow copied verbatim from desktop (Razor @@ escaping preserved)
    - No @media queries in mobile CSS files — device targeting at layout-selection layer
key_files:
  created:
    - EuphoriaInn.Service/Views/ShopManagement/Create.Mobile.cshtml
    - EuphoriaInn.Service/Views/ShopManagement/Edit.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop-management-create.mobile.css
    - EuphoriaInn.Service/wwwroot/css/shop-management-edit.mobile.css
  modified: []
decisions:
  - Edit.Mobile.cshtml carries the alert-info status block verbatim from desktop (D-15) — DMs need to know if item is awaiting approval
  - Create.Mobile.cshtml omits the Preview button present on desktop — not needed on mobile
  - Edit.Mobile.cshtml omits the View Details link present on desktop — simplified for mobile button row
  - updatePriceSuggestion() on mobile reads only Rarity (not Type) unlike the desktop Edit version which also checks itemType=='Equipment'; Create desktop also only checks Rarity — mobile strips the Type branch consistently
metrics:
  duration: "~5 minutes"
  completed: "2026-06-25"
  tasks_completed: 2
  files_modified: 4
status: complete
---

# Phase 19 Plan 04: ShopManagement Create & Edit Mobile Views Summary

**One-liner:** Mobile single-column glass-card forms for ShopManagement Create and Edit with stripped button-free price entry and working rarity price-suggestion hints.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create ShopManagement/Create.Mobile.cshtml + shop-management-create.mobile.css | 05b1d94 | Create.Mobile.cshtml, shop-management-create.mobile.css |
| 2 | Create ShopManagement/Edit.Mobile.cshtml + shop-management-edit.mobile.css | dd6f734 | Edit.Mobile.cshtml, shop-management-edit.mobile.css |

## What Was Built

### Task 1 — Create.Mobile.cshtml + CSS

Created a mobile-first single-column shop item creation form wrapping content in `<div class="shop-mgmt-create-card-mobile mb-3">`. Key characteristics:

- All desktop `col-md-*` grid columns replaced with single full-width stacked groups
- Price field: plain `<input asp-for="Price" ...>` with NO `input-group` wrapper, NO `randomPriceBtn`/`calculatedPriceBtn` buttons (D-12)
- `id="price-suggestion"` hint div updates on `#Rarity` change via `updatePriceSuggestion()`
- `updatePriceSuggestion()` stripped of all `randomBtn.disabled` / `calculatedBtn.disabled` references — only `suggestionDiv.innerHTML` and `suggestionDiv.className` branches remain
- `toggleAvailabilityWindow()` and the availability window section carried verbatim from desktop
- `@@keyframes legendary-glow` style block copied verbatim — `@@` Razor escaping preserved
- `setRandomPrice()` and `setCalculatedPrice()` functions entirely removed
- No `Layout =`, no `@inject`, no desktop Preview button
- `@section Styles` links `~/css/shop-management-create.mobile.css` with `asp-append-version="true"`

CSS file `shop-management-create.mobile.css`: glass card (16px padding, backdrop-filter blur), parchment `.form-label`/`h5` text, faded `.form-text`/`small`, `.badge { text-shadow: none !important }`, availability-window section top margin. Zero `@media` queries.

### Task 2 — Edit.Mobile.cshtml + CSS

Created a mobile-first single-column shop item edit form wrapping content in `<div class="shop-mgmt-edit-card-mobile mb-3">`. Mirrors Create exactly, with these Edit-specific additions:

- Hidden `<input asp-for="Id" />` field for the edit route
- `alert-info` status block kept verbatim from desktop (D-15) — shows Current Status badge and "awaiting DM approval" note for Draft items
- `<select asp-for="Type">` has `onchange="updatePriceSuggestion()"` (copied from desktop Edit)
- Rarity select has the Draft warning div from desktop
- `DOMContentLoaded` handler calls `updatePriceSuggestion()` to pre-populate hint on page load
- Submit button copy is "Save Changes" (per UI-SPEC), Cancel links to `/ShopManagement`
- Same JS stripping rules as Create: no `randomPriceBtn`/`calculatedPriceBtn` refs, no `setRandomPrice`/`setCalculatedPrice`

CSS file `shop-management-edit.mobile.css`: same glass card pattern + `.alert-info { margin-bottom: 1rem }` status alert spacing. Zero `@media` queries.

## Deviations from Plan

None — plan executed exactly as written.

Note: The desktop Edit.cshtml `updatePriceSuggestion()` checks `itemType == 'Equipment'` as a first branch, enabling buttons even without a rarity. On mobile the button-reference removal reduces this to the rarity-only branch — the plan explicitly says "same JS stripping rules as Create" which only checks rarity. This is intentional per D-12 and does not affect functional correctness (no buttons to enable/disable on mobile).

## Verification Results

- `dotnet build EuphoriaInn.Service` → Build succeeded, 0 errors (MSB3492 transient Windows file-lock noise suppressed per documented decision)
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~GetMobilePage_ShopManagementCreate|FullyQualifiedName~GetMobilePage_ShopManagementEdit"` → Passed: 2, Failed: 0
- Create.Mobile.cshtml contains `shop-mgmt-create-card-mobile` and links `~/css/shop-management-create.mobile.css` — confirmed
- Edit.Mobile.cshtml contains `shop-mgmt-edit-card-mobile` and links `~/css/shop-management-edit.mobile.css` — confirmed
- Edit.Mobile.cshtml contains `alert-info` block (D-15) — confirmed
- Neither file contains `setRandomPrice`, `setCalculatedPrice`, `randomPriceBtn`, or `calculatedPriceBtn` — confirmed
- Neither file contains `input-group` wrapper around Price field — confirmed
- Both files contain `@@keyframes` (escaped) — confirmed
- Neither file contains `Layout =` or `@inject` — confirmed
- CSS files contain zero `@media` queries — confirmed

## Known Stubs

None — both forms are fully wired to their ViewModels and all form fields are bound.

## Threat Flags

No new security surface introduced. Antiforgery tokens are present via `<form asp-action="...">` tag helpers (same as desktop). No `Html.Raw` usage; all model values bound via `asp-for`. Server-side validation via `CreateShopItemViewModel`/`EditShopItemViewModel` unchanged.

## Self-Check: PASSED

- [x] `EuphoriaInn.Service/Views/ShopManagement/Create.Mobile.cshtml` exists
- [x] `EuphoriaInn.Service/Views/ShopManagement/Edit.Mobile.cshtml` exists
- [x] `EuphoriaInn.Service/wwwroot/css/shop-management-create.mobile.css` exists
- [x] `EuphoriaInn.Service/wwwroot/css/shop-management-edit.mobile.css` exists
- [x] Commit 05b1d94 exists (Task 1)
- [x] Commit dd6f734 exists (Task 2)
- [x] Both integration tests GREEN (Passed: 2, Failed: 0)
