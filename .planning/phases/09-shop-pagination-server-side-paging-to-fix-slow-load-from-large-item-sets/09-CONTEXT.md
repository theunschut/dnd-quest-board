# Phase 9: Shop Pagination - Context

**Gathered:** 2026-04-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Add server-side pagination to the player-facing shop (`ShopController.Index`) so the database loads only one page of items per request rather than fetching all published items into memory. Simultaneously replace the client-side JS text search (`filterShopItems`) with a server-side name search parameter that stacks with existing filter/sort/page state in the URL.

Scope: player-facing shop only. `ShopManagementController` is NOT in scope.

</domain>

<decisions>
## Implementation Decisions

### Page Size
- **D-01:** 12 items per page on the player-facing shop.

### Pagination UI
- **D-02:** Bootstrap 5 numbered pager (Previous / 1 2 3 ... / Next) rendered below the item grid.
- **D-03:** Pagination links carry forward ALL active filter/sort/search state. A "page 3" link for an active filter becomes `?type=Equipment&rarity=Rare&sort=price_asc&search=sword&page=3`.

### JS Text Search Replacement
- **D-04:** The client-side `filterShopItems()` JS function is REMOVED entirely.
- **D-05:** A server-side name search is added as a `?search=` query parameter. Implementation: text input + submit button inside the existing filter form. Searching by name/description matches happen at the repository/query level (not post-fetch in memory).
- **D-06:** `?search=` stacks with all other params: `?type=&rarity=&sort=&search=&page=`. The search input is visible in the shop UI where the old JS search bar was.

### Filter+Sort+Page Stacking
- **D-07:** Changing any filter, sort, or search value resets the page number to 1. This is the standard behavior — page N of filter A is not meaningful for filter B.
- **D-08:** All URL params (`type`, `rarity`, `sort`, `search`, `page`) stack in a single URL, consistent with the Phase 5 pattern. The existing `BuildTabUrl` helper (or equivalent) must be extended to include `search` and `page` when generating category tab links.

### Claude's Discretion
- Whether to push filtering/sorting/search into the repository layer (EF `Where`/`OrderBy`/`Skip`/`Take` query) or do a hybrid (repo handles pagination and search, controller handles rarity filter and sort post-fetch). Given that Phase 5 intentionally kept `IShopService` interface unchanged, the planner should decide the cleanest approach consistent with the architecture.
- Exact Bootstrap pager markup (e.g., `<nav aria-label="..."><ul class="pagination">`) and how many page number links to show (e.g., always show first/last, ±2 from current).
- The `?search=` URL parameter name.
- Whether `GetPublishedItemsAsync` / `GetItemsByTypeAsync` grow a paged overload in `IShopRepository` and `IShopService`, or if a new unified `GetPagedPublishedItemsAsync(filter, sort, search, page, pageSize)` replaces the unbounded variants for the shop Index path.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Shop Controller & Repository (primary targets)
- `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` — current Index action (unbounded queries, post-fetch filter/sort); pagination + search replace this pattern
- `EuphoriaInn.Repository/ShopRepository.cs` — `GetPublishedItemsAsync` and `GetItemsByTypeAsync`; these are the queries that need `Skip`/`Take` (or a new paged method)
- `EuphoriaInn.Domain/Interfaces/IShopRepository.cs` — repository contract; any new paged method goes here
- `EuphoriaInn.Domain/Interfaces/IShopService.cs` — service contract; any new paged service method goes here
- `EuphoriaInn.Domain/Services/ShopService.cs` — service implementation

### ViewModels
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs` — extend with `int CurrentPage`, `int TotalPages`, `int TotalItems`, `string? SearchQuery`

### Shop View & CSS
- `EuphoriaInn.Service/Views/Shop/Index.cshtml` — add pager UI, replace JS search bar with server-side form input, remove `filterShopItems()` call
- `EuphoriaInn.Service/wwwroot/css/shop.css` — styling reference for consistency
- `EuphoriaInn.Service/wwwroot/js/site.js` — `filterShopItems` function lives here; remove it

### Phase 5 Context (stacking pattern established here)
- `.planning/phases/05-shop-filter-sort/05-CONTEXT.md` — Phase 5 decisions D-04 and Claude's Discretion note on `BuildTabUrl` logic; Phase 9 extends the same URL-stacking pattern

### Requirements
- `.planning/REQUIREMENTS.md` — no Phase 9 REQ-IDs yet (TBD); planner should confirm requirements coverage

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ShopController.Index` already accepts `ItemType? type`, `IList<ItemRarity>? rarity`, `string? sort` via query string — extend with `string? search` and `int page = 1`.
- `ShopIndexViewModel` already has `SelectedType`, `SelectedRarities`, `SelectedSort` — extend with `SearchQuery`, `CurrentPage`, `TotalPages`, `TotalItems`.
- Phase 5's `BuildTabUrl` pattern in `Index.cshtml` generates filter-aware URLs; extend it to also carry `search` and `page` (reset `page` to 1 when generating links that change filter state).

### Established Patterns
- URL query stacking: `?type=Equipment&rarity=Rare&rarity=VeryRare&sort=price_asc` (Phase 5). Phase 9 adds `&search=sword&page=2`.
- Repository queries use `.Where().ToListAsync()` today; pagination adds `.Skip((page-1)*pageSize).Take(pageSize)` and a separate `.CountAsync()` call for total item count.
- No existing pagination component anywhere in the codebase — this is the first one.

### Integration Points
- `ShopController.Index` is the single entry point for all player shop browsing.
- `EuphoriaInn.Service/wwwroot/js/site.js` contains `filterShopItems()` — remove this function.
- The JS `filterShopItems` is called from `Index.cshtml` via `onkeyup` and `onchange` events on the search input and rarity checkbox; both references must be cleaned up when removing the function.

</code_context>

<specifics>
## Specific Ideas

No specific references or "I want it like X" moments — open to standard ASP.NET Core MVC pagination patterns consistent with the existing shop UI style.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 09-shop-pagination-server-side-paging-to-fix-slow-load-from-large-item-sets*
*Context gathered: 2026-04-21*
