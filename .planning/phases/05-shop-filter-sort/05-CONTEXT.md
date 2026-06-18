# Phase 5: Shop Filter & Sort - Context

**Gathered:** 2026-04-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Add rarity filter (multi-select, one or more values) and price sort (ascending/descending) to the shop Index page. All filtering and sorting happens server-side. Filter+sort state is reflected in the URL as query parameters. No client-side JavaScript dependency required for this feature to work.

</domain>

<decisions>
## Implementation Decisions

### Rarity Filter UI

- **D-01:** Render rarity checkboxes as a compact row directly above the item grid, styled consistently with the existing category tabs. All five rarities (Common / Uncommon / Rare / Very Rare / Legendary) appear as checkboxes.
- **D-02:** A single "Apply" button submits the filter+sort form together. No per-checkbox auto-submit.

### Sort Control

- **D-03:** Sort control is a `<select>` dropdown (Price ↑ / Price ↓ / Default) inside the same `<form>` as the rarity checkboxes. One submit covers both filter and sort.

### Type + Rarity + Sort Stacking

- **D-04:** All three parameters (type, rarity, sort) stack in one URL: `?type=Equipment&rarity=Rare&rarity=VeryRare&sort=price_asc`. The existing category tab links (All / Equipment / Magic Items / Quest Items) must carry forward active rarity and sort query state so switching tabs doesn't reset filters.

### Empty State

- **D-05:** When the filtered+sorted result set is empty, show a "No items match your filters" message with a "Clear filters" `<a href>` link that navigates to the unfiltered shop.

### Claude's Discretion

- Exact HTML structure and CSS class names for the filter row (follow existing `shop-categories` / `category-btn` patterns).
- Whether to add a new `GetFilteredItemsAsync(type, rarities, sort)` service method or filter+sort in the controller/ViewModel after `GetPublishedItemsAsync`.
- URL parameter names for rarity and sort (e.g., `rarity`, `sort`).
- How "Default" sort order is defined (insertion order, name, or id).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Shop Domain
- `EuphoriaInn.Domain/Enums/ItemRarity.cs` — ItemRarity enum values (Common, Uncommon, Rare, VeryRare, Legendary)
- `EuphoriaInn.Domain/Enums/ItemType.cs` — ItemType enum (Equipment, MagicItem, QuestItems)
- `EuphoriaInn.Domain/Interfaces/IShopService.cs` — existing service contract; new filter/sort method (if added) goes here
- `EuphoriaInn.Domain/Services/ShopService.cs` — service implementation

### Shop Controller & ViewModels
- `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` — existing Index action with `ItemType? type` parameter pattern
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs` — extend with rarity list and sort value
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopItemViewModel.cs` — Rarity property already present

### Shop View
- `EuphoriaInn.Service/Views/Shop/Index.cshtml` — main view; category tabs, JS search bar, item grid all live here
- `EuphoriaInn.Service/wwwroot/css/shop.css` — styling for category tabs, item cards, existing patterns to follow

### Requirements
- `.planning/REQUIREMENTS.md` §SHOP-01 – SHOP-04 — the four acceptance criteria for this phase

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ShopController.Index` already accepts `ItemType? type` via query string — same pattern to extend for `IList<ItemRarity>? rarity` and `string? sort`.
- `ShopIndexViewModel.SelectedType` — extend with `IList<ItemRarity> SelectedRarities` and `string? SelectedSort`.
- `ItemRarity` enum in `ShopItemViewModel` already has `RarityDisplayName` and `RarityColorClass` helpers — reuse for checkbox labels.

### Established Patterns
- Category tabs use `<a href="@Url.Action(..., new { type = ... })">` with `active` class when selected — new type links must also include rarity+sort params to preserve stacked state.
- Filtering by type is currently done at the service layer (`GetItemsByTypeAsync`) — rarity/sort can follow the same pattern or be handled post-fetch in the controller.
- No existing multi-value query param pattern in the codebase — `IList<ItemRarity>? rarity` is the first multi-value parameter; ASP.NET Core model binding handles `?rarity=Rare&rarity=VeryRare` natively.

### Integration Points
- `ShopController.Index` is the single entry point — all filter/sort changes land here.
- View category tab `<a href>` links need updating to propagate active rarity+sort state forward.
- The existing JS text search (`filterShopItems`) operates client-side on rendered cards — it is unaffected by this phase.

</code_context>

<specifics>
## Specific Ideas

No specific references or "I want it like X" moments — open to standard approaches consistent with the existing shop UI style.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 05-shop-filter-sort*
*Context gathered: 2026-04-20*
