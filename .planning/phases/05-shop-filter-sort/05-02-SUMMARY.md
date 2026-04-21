---
phase: 05-shop-filter-sort
plan: 02
subsystem: ui
tags: [aspnet-mvc, razor, integration-tests, shop, filter-ui, css, buildtaburl]

requires:
  - phase: 05-shop-filter-sort
    plan: 01
    provides: ShopController rarity filter + price sort backend, ShopIndexViewModel state properties

provides:
  - Visible filter-row form in Views/Shop/Index.cshtml (SHOP-01/02/03/04 UI complete)
  - BuildTabUrl Razor @functions helper for multi-value rarity+sort tab URL construction (SHOP-03)
  - Filter-aware empty state 'No items match your filters' with Clear filters link (D-05)
  - ~100 lines of filter row CSS in shop.css (.shop-filter-row, .filter-apply-btn, .filter-clear-btn, etc.)
  - 6th Shop integration test verifying rendered HTML checkbox state and tab URL carry-forward

affects: [shop-feature-work]

tech-stack:
  added: []
  patterns:
    - "BuildTabUrl @functions helper: uses Microsoft.AspNetCore.Http.QueryString.Create with List<KeyValuePair> for multi-value rarity tab URLs"
    - "Razor @if blocks for option selected= attribute: avoids inline ternary Razor limitation in select options"
    - "Filter row form method=get: native browser submission, zero JavaScript dependency"

key-files:
  created: []
  modified:
    - EuphoriaInn.Service/Views/Shop/Index.cshtml
    - EuphoriaInn.Service/wwwroot/css/shop.css
    - EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs

key-decisions:
  - "BuildTabUrl uses QueryString.Create(List<KeyValuePair>) — chosen over RouteValueDictionary because Url.Action does not natively serialize IList<T> as repeated query keys"
  - "Sort select uses @if blocks for selected= attribute — inline C# ternary in Razor HTML attributes is not supported (known Razor limitation)"
  - "Filter-aware empty state uses Model.HasActiveFilters in else-if branch — cleaner than null-checking Model.Items twice"

requirements-completed: [SHOP-01, SHOP-02, SHOP-03, SHOP-04]

duration: 4min
completed: 2026-04-21
---

# Phase 05 Plan 02: Shop Filter & Sort View Wiring Summary

**Visible filter row form with rarity checkboxes, price sort select, Apply/Clear buttons, BuildTabUrl tab helper, and filter-aware empty state — SHOP-01 through SHOP-04 fully satisfied end-to-end**

## Performance

- **Duration:** 4 min
- **Started:** 2026-04-21T06:16:10Z
- **Completed:** 2026-04-21T06:19:58Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments

- Replaced the Plan 01 hidden filter form with the full visible `shop-filter-row` form containing 5 rarity checkboxes and a price sort select
- Added `@functions` block with `BuildTabUrl` helper using `QueryString.Create` to construct tab URLs that carry forward multi-value rarity and sort parameters
- Updated all four category tab links (All Items, Equipment, Magic Items, Quest Items) to use `BuildTabUrl`, preserving active filter/sort state on tab switch
- Added filter-aware empty state: "No items match your filters" with a Clear filters link when `Model.HasActiveFilters` is true and no items match
- Appended ~100 lines of filter row CSS to shop.css following UI-SPEC color, spacing, and typography contracts (0.5rem 1rem padding, accent-color #ffc107, no font-weight: 500)
- Added integration test `Shop_FilterForm_RendersCheckboxesAndTabUrlsPreserveState` verifying rendered HTML contains filter row, checked Rare checkbox, selected price_asc option, and rarity=Rare/sort=price_asc in tab URLs
- All 6 Shop-category integration tests pass; full suite 59 integration + 30 unit tests green

## Task Commits

1. **Task 1: Add filter-row form, BuildTabUrl helper, and filter-aware empty state** - `a698e46` (feat)
2. **Task 2: Add Shop_FilterForm integration test** - `7e8113a` (test)

## Files Created/Modified

- `EuphoriaInn.Service/Views/Shop/Index.cshtml` - Added @functions BuildTabUrl, updated tab hrefs, replaced hidden form with visible filter row, added filter-aware empty state
- `EuphoriaInn.Service/wwwroot/css/shop.css` - Appended filter row CSS rules (.shop-filter-row through .filter-clear-btn:hover)
- `EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs` - Added Shop_FilterForm_RendersCheckboxesAndTabUrlsPreserveState test

## Decisions Made

- `BuildTabUrl` uses `QueryString.Create(List<KeyValuePair<string, string?>>)` rather than `RouteValueDictionary` because `Url.Action` with an anonymous object does not serialize `IList<T>` as repeated query keys — the `QueryString.Create` approach appends `?rarity=Rare&rarity=VeryRare` correctly.
- Sort select `selected=` attribute uses `@if` blocks instead of inline C# ternary — this is a known Razor limitation where `selected="@(condition ? "selected" : "")"` does not render a boolean attribute correctly.
- `else if (Model.HasActiveFilters)` used in the empty state rather than `else if (!Model.Items.Any() && Model.HasActiveFilters)` — the outer `else` already implies `!Model.Items.Any()` so the redundant null check is omitted.

## Deviations from Plan

None - plan executed exactly as written.

All UI-SPEC contracts honored:
- Typography: only `font-weight: 400` and `font-weight: 600` in new rules (no `font-weight: 500`)
- Spacing: `padding: 0.5rem 1rem` (8px/16px on 4px grid) on Apply and Clear buttons
- Color: `accent-color: #ffc107`, background `rgba(0,0,0,0.5)`, text `#F4E4BC`
- Interaction: `<form method="get">` — works with JavaScript disabled

## Known Stubs

None — filter form is fully wired to ShopController.Index backend from Plan 01.

---
*Phase: 05-shop-filter-sort*
*Completed: 2026-04-21*
