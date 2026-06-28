---
phase: 19-admin-shop-management-views
plan: "06"
subsystem: mobile-views
tags: [mobile, shop, razor, css, integration-test]
dependency_graph:
  requires:
    - 19-01 (RED integration test stub for GetMobilePage_ShopDetails)
  provides:
    - Shop/Details.Mobile.cshtml — mobile single-column item detail view
    - shop-details.mobile.css — glass card + parchment text styles for Details mobile view
  affects:
    - EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css
tech_stack:
  added: []
  patterns:
    - Glass card mobile view pattern (shop-details-card-mobile)
    - Inlined non-modal content (D-17, D-18 decisions)
    - ShopItemDetailsViewModel full-type declaration (not base ShopItemViewModel)
    - TempData toast blocks copied verbatim from desktop Details.cshtml
    - bootstrap.Toast initialization in @section Scripts
key_files:
  created:
    - EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css
  modified: []
decisions:
  - Use ShopItemDetailsViewModel (full type) not ShopItemViewModel — prevents property-loss pitfall (RESEARCH.md Pitfall 3)
  - Content inlined directly (not delegated to _ShopItemDetailsContent partial) per D-17
  - Non-modal path only; no ViewBag.IsModal check per D-18
  - Sell-to-shop form omitted per RESEARCH Assumption A1 / Open Question 1
  - btn-primary for purchase button (CLAUDE.md filled colored button requirement)
metrics:
  duration: "~2 minutes"
  completed: "2026-06-25"
  tasks_completed: 1
  files_modified: 2
status: complete
---

# Phase 19 Plan 06: Shop/Details Mobile View Summary

**One-liner:** Mobile single-column shop item detail view with inlined non-modal content, conditional purchase form, and working TempData toast notifications using ShopItemDetailsViewModel.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create shop-details.mobile.css + Shop/Details.Mobile.cshtml | adc8a2d | EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml, EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css |

## What Was Built

### shop-details.mobile.css

Glass card CSS for the Shop/Details mobile view:
- `.shop-details-card-mobile` — glass card with `rgba(255,255,255,0.15)` background, `backdrop-filter: blur(15px)`, 16px padding
- `.parchment-text` — parchment color `#F4E4BC` with text-shadow for item name/price headings
- `.parchment-text-muted` — faded parchment `rgba(244,228,188,0.7)` for description/meta
- `.shop-details-card-mobile .badge { text-shadow: none !important; }` — badge override
- No `.item-rarity`/`.rarity-*` classes redefined (those live in `shop.mobile.css`)
- No `@media` queries (device targeting handled at layout-selection layer)

### Shop/Details.Mobile.cshtml

Mobile single-column item detail page:
- `@model ShopItemDetailsViewModel` — full type including DmVotes/RecentTransactions (prevents property-loss pitfall)
- `@using EuphoriaInn.Service.ViewModels.ShopViewModels` and `@using EuphoriaInn.Domain.Enums` directives
- `@section Styles` linking `~/css/shop-details.mobile.css` with `asp-append-version="true"`
- Toast container at top of page with three `@if (TempData["Success"])` / `@if (TempData["Error"])` / `@if (TempData["GoldReceived"])` blocks copied verbatim from desktop `Details.cshtml` lines 52–98
- `.shop-details-card-mobile` wrapper with header row (item name as `h5.parchment-text` + type badge `bg-primary`), rarity badge (`item-rarity rarity-@...`), description (`parchment-text-muted`), price (`strong.parchment-text`)
- Conditional purchase section: `Published && Quantity != 0` → purchase form with `asp-controller="Shop" asp-action="Purchase"`, AntiForgeryToken, quantity input, `btn-primary w-100` submit; `Quantity == 0` → "Out of stock." message; else → "This item is not currently available for purchase." message
- Back-to-Shop `btn-secondary w-100` anchor linking `/Shop`
- `@section Scripts` with `bootstrap.Toast` initialization on DOMContentLoaded
- No `Layout =`, no `@inject`, no `ViewBag.IsModal`, no `@await Html.PartialAsync(...)`

## Deviations from Plan

None — plan executed exactly as written.

## Verification Results

- `dotnet build EuphoriaInn.Service` → Build succeeded, 0 errors, 0 warnings
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~GetMobilePage_ShopDetails" --no-build -q` → Passed: 1, Failed: 0 (GREEN)
- `@model ShopItemDetailsViewModel` confirmed in Details.Mobile.cshtml
- `shop-details-card-mobile` CSS class present in both view and CSS file
- `~/css/shop-details.mobile.css` linked in @section Styles
- `asp-action="Purchase"` form present, gated on `Model.Status == ItemStatus.Published && Model.Quantity != 0`
- Three TempData toast blocks (Success/Error/GoldReceived) present
- `bootstrap.Toast` initialization in @section Scripts
- No `ViewBag.IsModal` reference, no `Html.PartialAsync`, no `Layout =`, no `@inject`
- `grep -c "\.item-rarity\|\.rarity-" shop-details.mobile.css` → 0 (rarity classes not redefined)
- No `@media` queries in shop-details.mobile.css

## Known Stubs

None — all content is wired to real model properties from ShopItemDetailsViewModel.

## Threat Flags

No new security surface beyond the plan's threat model:
- T-19-14: `@Html.AntiForgeryToken()` present in purchase form (CSRF mitigation)
- T-19-15: `min="1"` on quantity input (client convenience; server validates)
- T-19-16: All model properties rendered via Razor auto-encoding (`@Model.Description`, `@Model.Name`, TempData values); no `Html.Raw` used

## Self-Check: PASSED

- [x] `EuphoriaInn.Service/Views/Shop/Details.Mobile.cshtml` exists
- [x] `EuphoriaInn.Service/wwwroot/css/shop-details.mobile.css` exists
- [x] Commit adc8a2d exists (feat(19-06): add Shop/Details.Mobile.cshtml + shop-details.mobile.css)
- [x] Integration test GetMobilePage_ShopDetails passes (GREEN)
