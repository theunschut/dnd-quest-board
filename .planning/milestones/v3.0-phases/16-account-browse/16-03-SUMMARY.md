---
phase: 16-account-browse
plan: "03"
subsystem: mobile-shop
tags: [mobile, shop, css, integration-tests, BROWSE-01]
status: complete

dependency_graph:
  requires:
    - "16-01: integration test stubs (MobileShopIndex tests RED)"
  provides:
    - "BROWSE-01: mobile Shop index with 2-col grid, filter offcanvas, reused modal"
  affects:
    - "Plan 04: independent (GuildMembers mobile view)"

tech_stack:
  added: []
  patterns:
    - "glass card CSS pattern (rgba 0.15 background, blur 15px) — consistent with dm-profile.mobile.css"
    - "offcanvas-bottom filter drawer — Bootstrap 5 offcanvas, no new JS"
    - "Verbatim @functions block (BuildTabUrl/BuildPageUrl/PageWindow) from desktop view"
    - "Verbatim show.bs.modal JS event listener from desktop view"
    - "Literal &amp; in Razor HTML template for &amp; in HTML output (static text not encoded by Razor)"

key_files:
  created:
    - EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop.mobile.css
  modified: []

decisions:
  - "Literal &amp; in Razor template produces &amp; in HTML output — Razor does not encode static HTML text (only @expressions); writing Filter &amp; Sort in cshtml passes through as-is to HTML response"
  - "Separated @if badge block onto its own lines — inline @if immediately after & in HTML text breaks Razor HTML parser's token recognition"
  - "shop.mobile.css contains no @media queries — exclusively loaded by Index.Mobile.cshtml via @section Styles; device targeting is at layout-selection layer"

metrics:
  duration: "~12 minutes"
  completed_date: "2026-06-24"
  tasks_completed: 3
  files_modified: 2
---

# Phase 16 Plan 03: Shop Mobile View (BROWSE-01) Summary

Mobile Shop index with 2-column glass-card item grid, Bootstrap offcanvas-bottom filter drawer, verbatim desktop modal/JS listener, and dedicated shop.mobile.css — satisfying BROWSE-01 with zero desktop files modified.

## What Was Built

### Task 1: shop.mobile.css

Created `EuphoriaInn.Service/wwwroot/css/shop.mobile.css` — 149 lines, no `@media` queries.

Key rules:
- `.shop-grid-mobile`: `display: grid; grid-template-columns: 1fr 1fr; gap: 8px;`
- `.shop-item-card-mobile`: glass card (`rgba(255,255,255,0.15)`, `backdrop-filter: blur(15px)`, `border-radius: 12px`), `cursor: pointer`, `display: flex; flex-direction: column`
- `.shop-item-name`: parchment `#F4E4BC`, 2-line `-webkit-line-clamp`
- `.shop-item-price`: parchment, `font-weight: 700`, `margin-top: auto` (pushes to bottom)
- `.shop-header-card`: glass card with `padding: 16px`, parchment headings
- `#shopFilterOffcanvas`: `max-height: 70vh`, parchment `form-label` and `form-check-label`
- `.shop-empty-state`: glass card, centered, parchment headings
- Rarity colors: `.rarity-common` through `.rarity-legendary`
- `.item-rarity`: `text-shadow: none !important` (badge-no-shadow pattern)
- `.shop-pagination-mobile`: flex row, centered, wrapped
- `.text-muted` override inside glass cards

### Task 2: Shop/Index.Mobile.cshtml

Created `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml` — 317 lines.

Structure:
- `@using` + `@model ShopIndexViewModel` header (matches desktop)
- `@functions` block (BuildTabUrl/BuildPageUrl/PageWindow) copied verbatim from desktop view
- `@section Styles { <link href="~/css/shop.mobile.css" ... /> }`
- Glass card header with `fas fa-store` icon and subtitle
- Full-width `btn-warning` "Filter &amp; Sort" trigger button with `data-bs-target="#shopFilterOffcanvas"`
- `offcanvas offcanvas-bottom` drawer `id="shopFilterOffcanvas"` containing GET form with:
  - Category type links via `BuildTabUrl`
  - Rarity checkboxes via `Enum.GetValues<ItemRarity>()`
  - Sort `<select>` with price_asc/price_desc options
  - Search text input
  - Hidden `<input name="page" value="1">` (resets pagination on filter apply)
  - Apply Filters submit + conditional Clear Filters link
- `@if (Model.Items.Any())` → `<div class="shop-grid-mobile">` with `shop-item-card-mobile` cards
- Each card: item name (parchment), rarity badge, price, Buy/Login button (auth-gated)
- Empty state: `shop-empty-state` with `fa-box-open`, "No items found", faded parchment body
- Pagination via `PageWindow` when `TotalPages > 1`
- `#itemDetailsModal` markup copied verbatim from desktop view
- Toast container copied verbatim from desktop view
- `@section Scripts` with `show.bs.modal` event listener + toast init JS (verbatim from desktop)

Omissions (per D-03, D-04):
- No Purchase History side panel
- No floating merchant element

### Task 3: Tests GREEN

Both `MobileShopIndex*` integration tests pass:
- `MobileShopIndex_MobileUserAgent_RendersItemGridAndFilterButton` — asserts `Filter &amp; Sort` and `shop.mobile.css` present
- `MobileShopIndex_MobileUserAgent_OmitsPurchaseHistoryPanel` — asserts `purchase-history-panel` absent

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Razor &amp; encoding behavior for static HTML text**
- **Found during:** Task 3 (test turned RED)
- **Issue:** The plan's context note said "write `Filter & Sort` in Razor — Razor will auto-encode `&` to `&amp;`". This is incorrect for *static HTML text* in Razor templates. Razor only HTML-encodes dynamic `@expression` content. Static HTML text (like `Filter & Sort` written directly in `.cshtml`) passes through as-is, producing `Filter & Sort` in HTML output (not `&amp;`).
- **Secondary issue:** Writing `Filter & Sort@if (hasActiveFilters){...}` on a single line caused Razor's HTML parser to treat `& Sort@if` as potential HTML entity start, suppressing `@if` block execution (rendering literally as text).
- **Fix:** (a) Moved `@if` block to its own lines for clean Razor parsing. (b) Changed static text to `Filter &amp; Sort` — literal HTML entity in the template passes through to HTML output as `Filter &amp; Sort`, which the test asserts and browsers render as "Filter & Sort" visually.
- **Files modified:** `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml`
- **Commit:** 4a8d251

## Known Stubs

None — the shop mobile view renders real data from `Model.Items`. The empty state (`No items found`) is not a stub; it renders conditionally when no items exist.

## Threat Flags

None — this plan adds only view (cshtml) and CSS files. No new endpoints, auth paths, schema changes, or trust boundary crossings. The Shop controller endpoint at `/Shop` already existed.

## Self-Check: PASSED

- `EuphoriaInn.Service/Views/Shop/Index.Mobile.cshtml` — FOUND
- `EuphoriaInn.Service/wwwroot/css/shop.mobile.css` — FOUND
- Commit 9886392 (shop.mobile.css) — FOUND in git log
- Commit 955b120 (Index.Mobile.cshtml) — FOUND in git log
- Commit 4a8d251 (fix &amp; encoding) — FOUND in git log
- `dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileShopIndex"` → 2 Passed, 0 Failed — VERIFIED
- `dotnet build EuphoriaInn.Service` → Build succeeded — VERIFIED
- Desktop `Shop/Index.cshtml` unmodified — VERIFIED
