# Phase 9: Shop Pagination — Research

**Researched:** 2026-04-21
**Domain:** ASP.NET Core 8 MVC server-side pagination with EF Core, Bootstrap 5 pager UI
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** 12 items per page on the player-facing shop.
- **D-02:** Bootstrap 5 numbered pager (Previous / 1 2 3 ... / Next) rendered below the item grid.
- **D-03:** Pagination links carry forward ALL active filter/sort/search state. A "page 3" link for an active filter becomes `?type=Equipment&rarity=Rare&sort=price_asc&search=sword&page=3`.
- **D-04:** The client-side `filterShopItems()` JS function is REMOVED entirely.
- **D-05:** A server-side name search is added as a `?search=` query parameter. Implementation: text input + submit button inside the existing filter form. Searching by name/description matches happen at the repository/query level (not post-fetch in memory).
- **D-06:** `?search=` stacks with all other params: `?type=&rarity=&sort=&search=&page=`. The search input is visible in the shop UI where the old JS search bar was.
- **D-07:** Changing any filter, sort, or search value resets the page number to 1. This is the standard behavior — page N of filter A is not meaningful for filter B.
- **D-08:** All URL params (`type`, `rarity`, `sort`, `search`, `page`) stack in a single URL, consistent with the Phase 5 pattern. The existing `BuildTabUrl` helper (or equivalent) must be extended to include `search` and `page` when generating category tab links.

### Claude's Discretion

- Whether to push filtering/sorting/search into the repository layer (EF `Where`/`OrderBy`/`Skip`/`Take` query) or do a hybrid (repo handles pagination and search, controller handles rarity filter and sort post-fetch). Given that Phase 5 intentionally kept `IShopService` interface unchanged, the planner should decide the cleanest approach consistent with the architecture.
- Exact Bootstrap pager markup (e.g., `<nav aria-label="..."><ul class="pagination">`) and how many page number links to show (e.g., always show first/last, ±2 from current).
- The `?search=` URL parameter name.
- Whether `GetPublishedItemsAsync` / `GetItemsByTypeAsync` grow a paged overload in `IShopRepository` and `IShopService`, or if a new unified `GetPagedPublishedItemsAsync(filter, sort, search, page, pageSize)` replaces the unbounded variants for the shop Index path.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.
</user_constraints>

---

## Summary

Phase 9 adds server-side pagination and replaces the client-side JS text search on the player-facing `ShopController.Index`. Today the controller calls `GetPublishedItemsAsync` or `GetItemsByTypeAsync`, retrieves the full published item list into memory, then applies rarity filter and sort via in-memory LINQ. With the addition of a new unified paged repository method, the database will return only 12 items per request along with a total-count value that drives pager math.

The implementation has three clean layers of change: (1) a new `GetPagedPublishedItemsAsync` method added to `IShopRepository`, `IShopService`, and their implementations that pushes type/search/page/pageSize into EF Core `Where`/`Skip`/`Take` and executes a parallel `CountAsync`; (2) rarity filtering and sort pushed into the repository query so no post-fetch LINQ remains on the shop Index path; (3) the `ShopIndexViewModel`, `ShopController.Index` action signature, `Index.cshtml` view, `shop.css`, and `site.js` updated to complete the visible end of the feature. The UI-SPEC produced in the prior discussion session already prescribes every pixel of the pager component.

**Primary recommendation:** Add a single unified `GetPagedPublishedItemsAsync(ItemType? type, IList<ItemRarity>? rarities, string? sort, string? search, int page, int pageSize)` to the repository and service layers. This eliminates post-fetch filtering entirely, keeps the controller thin, and avoids polluting the existing unbounded `GetPublishedItemsAsync` / `GetItemsByTypeAsync` signatures that `ShopManagementController` still uses.

---

## Standard Stack

### Core (all already in the project — no new packages)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core (SqlServer provider) | 9.0.6 | `Skip`/`Take`/`CountAsync` for pagination | Already in `EuphoriaInn.Repository`; translate directly to SQL `OFFSET`/`FETCH` |
| ASP.NET Core 8 MVC | 8.0.x | Model binding for `int page = 1`, `string? search` | Already in `EuphoriaInn.Service`; no new binding needed |
| Bootstrap 5.3.0 | CDN | `.pagination` component for pager UI | Already loaded in `_Layout.cshtml` |
| Font Awesome 6.4.0 | CDN | `fa-chevron-left` / `fa-chevron-right` icons in pager | Already loaded |

No new NuGet packages. No new CDN libraries.

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Microsoft.AspNetCore.Http.QueryString.Create` | built-in | Builds stacked URL query strings with repeated keys | Already used in Phase 5's `BuildTabUrl` — same pattern for pager links |

---

## Architecture Patterns

### Recommended Approach: Unified Paged Repository Method

Push all filtering, sorting, search, and pagination into one EF Core query at the repository layer. The controller receives a pre-filtered, pre-sorted page of items plus a total count. No post-fetch LINQ remains on the Index path.

```
IShopRepository.GetPagedPublishedItemsAsync(
    ItemType? type,
    IList<ItemRarity>? rarities,
    string? sort,
    string? search,
    int page,
    int pageSize,
    CancellationToken token)
    → Task<(IList<ShopItem> Items, int TotalCount)>
```

`IShopService` adds a matching method that delegates directly. `ShopController.Index` calls the service method once and maps the result into the updated `ShopIndexViewModel`.

### Why unified over hybrid

Phase 5 kept `IShopService` unchanged because filtering was post-fetch LINQ — cheap at small scale. Phase 9's reason-to-exist is moving pagination *into the database*. Post-fetch rarity filtering after a paged DB query would produce wrong page sizes (e.g., DB returns 12, then rarity filter drops 4, leaving 8 on the page). The entire filter set must be inside the EF query for `Skip`/`Take` to be meaningful.

### EF Core Pagination Pattern

```csharp
// Source: EF Core official docs — IQueryable deferred execution
var query = DbContext.ShopItems
    .Include(si => si.CreatedByDm)
    .Where(si => si.Status == 1)
    .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
    .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow);

// Type filter
if (type.HasValue)
    query = query.Where(si => si.Type == (int)type.Value);

// Rarity filter (multi-value — IN clause)
if (rarities is { Count: > 0 })
{
    var rarityInts = rarities.Select(r => (int)r).ToList();
    query = query.Where(si => rarityInts.Contains(si.Rarity));
}

// Search (name OR description, case-insensitive via EF Core translation)
if (!string.IsNullOrWhiteSpace(search))
    query = query.Where(si =>
        si.Name.Contains(search) || si.Description.Contains(search));

// Sort
query = sort switch
{
    "price_asc"  => query.OrderBy(si => si.Price),
    "price_desc" => query.OrderByDescending(si => si.Price),
    _            => query.OrderBy(si => si.Name)
};

// Execute count and page in parallel — two DB roundtrips
var totalCount = await query.CountAsync(cancellationToken: token);
var entities   = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken: token);
```

Key: `CountAsync` runs on the filtered-but-unsorted query (sort does not affect count; calling it before the `OrderBy` avoids an unnecessary ORDER BY on the count path). Apply `OrderBy` before `Skip`/`Take`.

### Return Type Choice: Tuple vs PagedResult record

Two options:

| Option | Code | Tradeoff |
|--------|------|----------|
| Named tuple | `Task<(IList<ShopItem> Items, int TotalCount)>` | Zero ceremony, inline deconstruction in service/controller |
| Dedicated record | `PagedResult<ShopItem>` in Domain.Models | Self-documenting, extensible (add HasNextPage etc.) |

Given the project has no existing pagination type and only one call site, a named tuple is idiomatic for the project's current style. A `PagedResult<T>` record is also acceptable — planner decides. Either works cleanly; the tuple is simpler to introduce without a new file.

### ShopIndexViewModel Extension

```csharp
// Additions to existing ShopIndexViewModel
public string? SearchQuery { get; set; }
public int CurrentPage { get; set; } = 1;
public int TotalPages { get; set; } = 1;
public int TotalItems { get; set; } = 0;
public bool HasActiveSearch => !string.IsNullOrEmpty(SearchQuery);
// Extend HasActiveFilters: SelectedRarities.Count > 0 || SelectedSort != null || HasActiveSearch
```

Remove computed properties `EquipmentItems` and `MagicItems` — they filter `Items` client-side, which conflicts with server-side pagination (the page only contains 12 items; these properties would silently return a sub-set of the page). The view renders `Model.Items` directly (all items on the page are already the correct type, filtered server-side).

### BuildPageUrl / BuildTabUrl Pattern

The Razor `@functions` block in `Index.cshtml` already has `BuildTabUrl`. Extend it and add `BuildPageUrl`:

```csharp
// Page URL — all current state, only page number changes
string BuildPageUrl(int pageNumber)
{
    var pairs = new List<KeyValuePair<string, string?>>();
    if (Model.SelectedType.HasValue)
        pairs.Add(new("type", Model.SelectedType.Value.ToString()));
    foreach (var r in Model.SelectedRarities)
        pairs.Add(new("rarity", r.ToString()));
    if (!string.IsNullOrEmpty(Model.SelectedSort))
        pairs.Add(new("sort", Model.SelectedSort));
    if (!string.IsNullOrEmpty(Model.SearchQuery))
        pairs.Add(new("search", Model.SearchQuery));
    if (pageNumber > 1)                        // omit page=1 for clean URLs
        pairs.Add(new("page", pageNumber.ToString()));
    return Url.Action("Index", "Shop") + QueryString.Create(pairs).Value;
}

// Tab URL — extend existing to also carry search; always reset page to 1
string BuildTabUrl(ItemType? tabType, IList<ItemRarity> rarities, string? sort, string? search = null)
{
    var pairs = new List<KeyValuePair<string, string?>>();
    if (tabType.HasValue)
        pairs.Add(new("type", tabType.Value.ToString()));
    foreach (var r in rarities)
        pairs.Add(new("rarity", r.ToString()));
    if (!string.IsNullOrEmpty(sort))
        pairs.Add(new("sort", sort));
    if (!string.IsNullOrEmpty(search))
        pairs.Add(new("search", search));
    // page intentionally omitted — tab switch always goes to page 1
    return Url.Action("Index", "Shop") + QueryString.Create(pairs).Value;
}
```

### Filter Form: page=1 hidden field

Add `<input type="hidden" name="page" value="1" />` inside the `<form method="get">` so that any filter or sort change (submitted via the form) always resets to page 1. This is the D-07 mechanism.

### Pager Page Window Algorithm

The UI-SPEC specifies: always show page 1 and last page; show current ±2; use ellipsis for gaps. Implement as a Razor helper or inline loop producing a `List<int?>` where `null` represents an ellipsis:

```csharp
IEnumerable<int?> PageWindow(int current, int total)
{
    var pages = new SortedSet<int> { 1, total };
    for (int i = Math.Max(1, current - 2); i <= Math.Min(total, current + 2); i++)
        pages.Add(i);

    int? prev = null;
    foreach (var p in pages)
    {
        if (prev.HasValue && p - prev.Value > 1)
            yield return null; // ellipsis
        yield return p;
        prev = p;
    }
}
```

### Controller Action Signature

```csharp
[HttpGet]
public async Task<IActionResult> Index(
    ItemType? type = null,
    IList<ItemRarity>? rarity = null,
    string? sort = null,
    string? search = null,
    int page = 1,
    CancellationToken token = default)
```

### Anti-Patterns to Avoid

- **Post-fetch rarity filtering with Skip/Take:** If `Skip`/`Take` runs before rarity filtering, pages will be inconsistently sized. All filtering must be inside the EF query.
- **Two separate CountAsync + ToListAsync without a shared base query:** If filter conditions are duplicated between count and list queries, they can drift. Build one `IQueryable<ShopItemEntity>` and reuse it for both calls.
- **Sorting before CountAsync:** Unnecessary; SQL Server adds ORDER BY to the COUNT subquery, which the engine ignores but it wastes parse time. Apply sort after the count.
- **Including `Transactions` navigation in the paged list query:** `GetPublishedItemsAsync` does not include `Transactions` (only `CreatedByDm`); the paged query should follow the same pattern. `Transactions` is only loaded by `GetItemWithDetailsAsync`.
- **`page` parameter without clamping:** If a user navigates to `?page=9999` and the shop only has 1 page, clamp `page = Math.Max(1, Math.Min(page, totalPages))` after computing `totalPages`. Do this in the controller before building the ViewModel.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| SQL pagination | Manual ROW_NUMBER() or cursor logic | EF Core `.Skip().Take()` | EF translates to `OFFSET N ROWS FETCH NEXT M ROWS ONLY` automatically on SQL Server |
| Total count query | Separate manual SQL COUNT | EF Core `.CountAsync()` on the same `IQueryable` | Same filter predicates, no duplication |
| Multi-value `rarity` binding | Manual string splitting from query string | ASP.NET Core model binding with `IList<ItemRarity>? rarity` | Already works — Phase 5 established this pattern |
| URL query string building with repeated keys | String concatenation | `QueryString.Create(List<KeyValuePair<string,string?>>)` | Already used by `BuildTabUrl`; handles URL encoding and repeated keys correctly |
| Pager HTML | Custom JS pager component | Bootstrap 5 `.pagination` + Razor loop | Server-rendered, accessible, no JS, already styled via CDN |

---

## Common Pitfalls

### Pitfall 1: Post-fetch rarity filter breaks page size invariant
**What goes wrong:** DB returns 12 items with `Skip`/`Take`, then controller LINQ filters by rarity — page shows fewer than 12 items even when more exist. Page N+1 starts at the wrong offset.
**Why it happens:** Phase 5 applied rarity filter post-fetch deliberately (IShopService unchanged). Phase 9 moves pagination into the DB, making post-fetch filtering incorrect.
**How to avoid:** Push rarity (and sort and search) into the EF `IQueryable` before `Skip`/`Take`. All filtering must happen in one query.
**Warning signs:** Integration test showing page 1 returns fewer items than `pageSize` when filtered items exist on later pages.

### Pitfall 2: `TotalPages` computed as 0 when shop is empty
**What goes wrong:** `TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)` returns 0 when `totalCount == 0`. Pager renders Previous/Next to page 0/1 on an empty shop.
**Why it happens:** Integer ceiling of 0/12 is 0.
**How to avoid:** `TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize))`.
**Warning signs:** Empty shop state renders a pager with `?page=1` active and Previous disabled but Next enabled.

### Pitfall 3: `EquipmentItems` / `MagicItems` computed properties silently filter the page
**What goes wrong:** `ShopIndexViewModel.EquipmentItems` and `MagicItems` filter `Items` in memory. With server-side pagination, `Items` only contains the current page. A type-filtered shop page (e.g., `?type=Equipment`) passes `Equipment` items via the new paged query — if the view still references `Model.EquipmentItems`, it will display correctly but only because type was already filtered. However, the all-items tab (no type filter) would show ALL items on the page in both computed lists, causing duplicates.
**Why it happens:** These computed properties were designed for client-side tab switching in a world where the full item list was in memory.
**How to avoid:** Remove `EquipmentItems` and `MagicItems` from `ShopIndexViewModel`. Render `Model.Items` directly in the view (the type is already server-filtered).
**Warning signs:** Items appearing in multiple tabs or the item grid showing duplicates.

### Pitfall 4: Search `Contains` is case-sensitive on some SQL Server collations
**What goes wrong:** `si.Name.Contains(search)` translates to `LIKE '%search%'` in SQL — case sensitivity depends on the database collation. On case-sensitive collations "Sword" would not match "sword".
**Why it happens:** EF Core delegates case-sensitivity to the DB collation.
**How to avoid:** For this project, the SQL Server default collation (`SQL_Latin1_General_CP1_CI_AS`) is case-insensitive. This is a known safe assumption for the project's Docker `mssql/server:2022-latest` container, which uses the default collation unless overridden. No additional code change needed. Document as an assumption.
**Warning signs:** Would only manifest if the database was created with a `_CS_` collation — not the case here.

### Pitfall 5: `page` out of range after filter change
**What goes wrong:** User is on page 3 of "All Items". Applies a rarity filter that only has 1 page of results. The form submits with `page=1` (due to the hidden field) — correct. But if a user manually edits the URL to `?page=5` on a 1-page result set, `Skip(48)` returns empty results while `CurrentPage=5` is displayed.
**Why it happens:** No input clamping.
**How to avoid:** In `ShopController.Index`, after computing `totalPages`, clamp: `page = Math.Max(1, Math.Min(page, totalPages))`.
**Warning signs:** Empty inventory grid with "Showing 0–0 of N items" while `CurrentPage > TotalPages`.

### Pitfall 6: Category tab links no longer carry `search` forward
**What goes wrong:** User searches "sword" then clicks "Equipment" tab — search is lost because `BuildTabUrl` did not include `search`.
**Why it happens:** `BuildTabUrl` signature was written in Phase 5 before `search` existed.
**How to avoid:** Extend `BuildTabUrl` to accept `string? search = null` and emit `search` pair when non-null. All four call sites in `Index.cshtml` pass `Model.SearchQuery`.
**Warning signs:** Clicking a category tab when a search is active clears the search query from the URL.

---

## Code Examples

### Repository: GetPagedPublishedItemsAsync
```csharp
// Pattern: build composable IQueryable, then count + paginate
public async Task<(IList<ShopItem> Items, int TotalCount)> GetPagedPublishedItemsAsync(
    ItemType? type,
    IList<ItemRarity>? rarities,
    string? sort,
    string? search,
    int page,
    int pageSize,
    CancellationToken token = default)
{
    var query = DbContext.ShopItems
        .Include(si => si.CreatedByDm)
        .Where(si => si.Status == 1)
        .Where(si => si.AvailableFrom == null || si.AvailableFrom <= DateTime.UtcNow)
        .Where(si => si.AvailableUntil == null || si.AvailableUntil >= DateTime.UtcNow);

    if (type.HasValue)
        query = query.Where(si => si.Type == (int)type.Value);

    if (rarities is { Count: > 0 })
    {
        var rarityInts = rarities.Select(r => (int)r).ToList();
        query = query.Where(si => rarityInts.Contains(si.Rarity));
    }

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(si =>
            si.Name.Contains(search) || si.Description.Contains(search));

    var totalCount = await query.CountAsync(cancellationToken: token);

    query = sort switch
    {
        "price_asc"  => query.OrderBy(si => si.Price),
        "price_desc" => query.OrderByDescending(si => si.Price),
        _            => query.OrderBy(si => si.Name)
    };

    var entities = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken: token);

    return (Mapper.Map<IList<ShopItem>>(entities), totalCount);
}
```

### Controller: updated Index action
```csharp
[HttpGet]
public async Task<IActionResult> Index(
    ItemType? type = null,
    IList<ItemRarity>? rarity = null,
    string? sort = null,
    string? search = null,
    int page = 1,
    CancellationToken token = default)
{
    const int pageSize = 12;

    var (items, totalCount) = await shopService.GetPagedPublishedItemsAsync(
        type, rarity, sort, search, page, pageSize, token);

    var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
    page = Math.Max(1, Math.Min(page, totalPages)); // clamp out-of-range page

    var viewModel = new ShopIndexViewModel
    {
        Items           = mapper.Map<IList<ShopItemViewModel>>(items),
        SelectedType    = type,
        SelectedRarities = rarity ?? [],
        SelectedSort    = sort,
        SearchQuery     = search,
        CurrentPage     = page,
        TotalPages      = totalPages,
        TotalItems      = totalCount
    };

    // ... user purchases enrichment (unchanged) ...

    return View(viewModel);
}
```

### Razor: pager component (from UI-SPEC)
```html
@if (Model.TotalPages > 1)
{
    <nav aria-label="Shop page navigation" class="shop-pagination mt-3 mb-4 text-center">
        <ul class="pagination justify-content-center">
            <li class="page-item @(Model.CurrentPage <= 1 ? "disabled" : "")">
                <a class="page-link shop-page-link" href="@BuildPageUrl(Model.CurrentPage - 1)"
                   aria-label="Previous page">
                    <i class="fas fa-chevron-left me-1"></i>Previous
                </a>
            </li>

            @foreach (var p in PageWindow(Model.CurrentPage, Model.TotalPages))
            {
                if (p == null)
                {
                    <li class="page-item disabled">
                        <span class="page-link">…</span>
                    </li>
                }
                else if (p == Model.CurrentPage)
                {
                    <li class="page-item active">
                        <span class="page-link">@p</span>
                    </li>
                }
                else
                {
                    <li class="page-item">
                        <a class="page-link shop-page-link" href="@BuildPageUrl(p.Value)">@p</a>
                    </li>
                }
            }

            <li class="page-item @(Model.CurrentPage >= Model.TotalPages ? "disabled" : "")">
                <a class="page-link shop-page-link" href="@BuildPageUrl(Model.CurrentPage + 1)"
                   aria-label="Next page">
                    Next<i class="fas fa-chevron-right ms-1"></i>
                </a>
            </li>
        </ul>
        <p class="shop-pagination-count text-muted mb-0">
            Showing @((Model.CurrentPage - 1) * 12 + 1)–@(Math.Min(Model.CurrentPage * 12, Model.TotalItems))
            of @Model.TotalItems items
        </p>
    </nav>
}
```

---

## Files to Change

All changes are isolated to existing files. No new files required except potentially a `PagedResult<T>` record (optional).

| File | Change |
|------|--------|
| `EuphoriaInn.Domain/Interfaces/IShopRepository.cs` | Add `GetPagedPublishedItemsAsync` signature |
| `EuphoriaInn.Domain/Interfaces/IShopService.cs` | Add `GetPagedPublishedItemsAsync` signature |
| `EuphoriaInn.Domain/Services/ShopService.cs` | Implement delegation to repository |
| `EuphoriaInn.Repository/ShopRepository.cs` | Implement `GetPagedPublishedItemsAsync` with EF Core query |
| `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs` | Add 5 new properties; remove `EquipmentItems` + `MagicItems` computed properties |
| `EuphoriaInn.Service/Controllers/Shop/ShopController.cs` | Update `Index` signature; call new paged method; compute `TotalPages`; clamp `page` |
| `EuphoriaInn.Service/Views/Shop/Index.cshtml` | Add `BuildPageUrl`, extend `BuildTabUrl`; replace JS search bar; add pager; remove `data-item-name`/`data-item-description` attrs; remove `shopEmptyMsg` hidden div; add search empty state variant |
| `EuphoriaInn.Service/wwwroot/css/shop.css` | Add `/* PAGINATION */` section per UI-SPEC CSS |
| `EuphoriaInn.Service/wwwroot/js/site.js` | Remove `filterShopItems()` function |

`ShopManagementController` uses `GetPublishedItemsAsync`, `GetItemsByStatusAsync`, `GetItemsByDmAsync` — none of these are changed. The new method is purely additive.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj`, `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| Quick run (unit) | `dotnet test EuphoriaInn.UnitTests` |
| Integration run | `dotnet test EuphoriaInn.IntegrationTests` |
| Full suite | `dotnet test` (from repo root) |

Integration tests use SQLite in-memory via `WebApplicationFactoryBase` + `TestDatabase`. The existing `ShopControllerIntegrationTests` (7 tests) and `ShopServiceTests` (unit) are the primary regression targets.

### Phase Requirements → Test Map

| ID | Behavior | Test Type | Command |
|----|----------|-----------|---------|
| SHOP-PAG-01 | Shop Index returns 12 items when >12 published items exist | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "ShopControllerIntegrationTests"` |
| SHOP-PAG-02 | `?page=2` returns items 13–24 | Integration | same filter |
| SHOP-PAG-03 | `?search=sword` filters results server-side (no JS required) | Integration | same filter |
| SHOP-PAG-04 | `?type=Equipment&rarity=Rare&sort=price_asc&search=s&page=2` all stack correctly | Integration | same filter |
| SHOP-PAG-05 | Pager HTML rendered when TotalPages > 1 | Integration | same filter |
| SHOP-PAG-06 | `filterShopItems` removed from JS — site.js no longer contains the function | Static grep | `grep -n "filterShopItems" EuphoriaInn.Service/wwwroot/js/site.js` (expect no output) |
| SHOP-PAG-07 | Out-of-range `?page=9999` returns last valid page, not empty | Integration | same filter |

### Wave 0 Gaps

New integration tests for the paged behavior (SHOP-PAG-01 through SHOP-PAG-07) do not yet exist. The unit test file `ShopServiceTests.cs` does not yet have a test for `GetPagedPublishedItemsAsync`. These are Wave 0 gaps:

- [ ] `EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs` — add test methods for pagination, search, and URL stacking
- [ ] `EuphoriaInn.UnitTests/Services/ShopServiceTests.cs` — add unit tests for `GetPagedPublishedItemsAsync` delegation and page-clamping
- [ ] `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs` — verify `CreateShopItemAsync` can bulk-create 15+ items for pagination tests (likely already exists; check capacity)

---

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — all changes are code/CSS/JS within the existing .NET 8 + SQL Server + Bootstrap 5 stack).

---

## Open Questions

1. **`EquipmentItems` / `MagicItems` computed properties — consumers outside `Index.cshtml`?**
   - What we know: They are defined in `ShopIndexViewModel` and referenced in `Index.cshtml`.
   - What's unclear: No other view references were found in a quick scan, but `ShopManagementController` does not use `ShopIndexViewModel` at all.
   - Recommendation: Confirm with a codebase grep before removing. If they appear elsewhere, keep them but mark them obsolete.

2. **`TestDataHelper.CreateShopItemAsync` bulk capacity**
   - What we know: It exists and is used in `ShopControllerIntegrationTests`. It creates one item per call.
   - What's unclear: Whether calling it 15 times in a test is performant enough for CI.
   - Recommendation: Use a loop in the test setup; 15 inserts on SQLite in-memory is fast (< 1s).

3. **Soft delete / availability window items counted toward TotalCount?**
   - What we know: `GetPublishedItemsAsync` filters `AvailableFrom` and `AvailableUntil`. The paged query will do the same.
   - What's unclear: Whether `TotalItems` in the pager should reflect only currently-available items (yes, per the existing filter), or all published items regardless of window.
   - Recommendation: Apply availability-window filter inside `GetPagedPublishedItemsAsync` (same as today) — `TotalItems` reflects exactly what's browseable right now.

---

## Sources

### Primary (HIGH confidence)
- Codebase read — `ShopController.cs`, `ShopRepository.cs`, `IShopRepository.cs`, `IShopService.cs`, `ShopService.cs`, `ShopIndexViewModel.cs`, `Index.cshtml`, `site.js`, `shop.css`
- `09-CONTEXT.md` — locked decisions D-01 through D-08
- `09-UI-SPEC.md` — pager markup, CSS, copywriting, interaction contract, ViewModel extension contract
- `05-CONTEXT.md` — Phase 5 stacking pattern and `BuildTabUrl` implementation history
- EF Core documentation (knowledge verified against `.NET 8 / EF Core 9` — `Skip`/`Take`/`CountAsync` API is stable and unchanged since EF Core 3.x)

### Secondary (MEDIUM confidence)
- `STATE.md` accumulated decisions — Phase 5 note on `QueryString.Create(List<KeyValuePair>)` for repeated query keys

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries; all patterns already in codebase
- Architecture: HIGH — EF Core pagination is a well-established pattern; unified method approach is the only option that makes `Skip`/`Take` correct under multi-filter conditions
- Pitfalls: HIGH — derived from direct code reading of current implementation and Phase 5 decisions

**Research date:** 2026-04-21
**Valid until:** 2026-05-21 (stable domain; no fast-moving dependencies)
