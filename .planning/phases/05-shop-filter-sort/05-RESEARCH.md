# Phase 5: Shop Filter & Sort - Research

**Researched:** 2026-04-20
**Domain:** ASP.NET Core 8 MVC — server-side filtering/sorting with query parameter binding
**Confidence:** HIGH

## Summary

Phase 5 adds rarity-checkbox filtering and price sorting to the shop index page using a plain HTML `<form method="get">` — no JavaScript dependency. All decisions and UI contracts are locked in CONTEXT.md and the approved UI-SPEC. Research confirms ASP.NET Core's native multi-value query string binding handles `?rarity=Rare&rarity=VeryRare` → `IList<ItemRarity>` with zero custom code. The existing `ShopController.Index` action pattern (single `ItemType? type` parameter) extends cleanly to receive `IList<ItemRarity>? rarity` and `string? sort` with identical model-binding mechanics.

The one design choice left open (D-DISCR: where to apply rarity+sort — new service method vs. controller post-fetch) is resolvable in-process: because published item counts are small and the existing `GetPublishedItemsAsync` / `GetItemsByTypeAsync` already load all items in one query, controller-side LINQ filtering is the lowest-friction approach. No EF Core migration is required; no new database query is needed. A service-layer method adds interface surface area without benefit at this scale.

Category tab links require a helper (either a Razor function or a controller-built `RouteValueDictionary`) to propagate the multi-value rarity list alongside the type. This is the most non-trivial part of the implementation: ASP.NET Core's `Url.Action` with anonymous route objects does not naturally serialize `IList<T>` to repeated query string keys. Research confirms the correct approach is a helper method that builds a `RouteValueDictionary` with indexed keys (e.g., `rarity[0]`, `rarity[1]`), or a Razor helper method that constructs the href string manually via `QueryHelpers`.

**Primary recommendation:** Filter and sort in the controller (post-fetch LINQ). Use `QueryHelpers.AddQueryString` in a Razor `@functions` helper to build tab URLs that carry forward multi-value rarity state.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Render rarity checkboxes as a compact row directly above the item grid, styled consistently with the existing category tabs. All five rarities (Common / Uncommon / Rare / Very Rare / Legendary) appear as checkboxes.
- **D-02:** A single "Apply" button submits the filter+sort form together. No per-checkbox auto-submit.
- **D-03:** Sort control is a `<select>` dropdown (Price ↑ / Price ↓ / Default) inside the same `<form>` as the rarity checkboxes. One submit covers both filter and sort.
- **D-04:** All three parameters (type, rarity, sort) stack in one URL: `?type=Equipment&rarity=Rare&rarity=VeryRare&sort=price_asc`. The existing category tab links (All / Equipment / Magic Items / Quest Items) must carry forward active rarity and sort query state so switching tabs doesn't reset filters.
- **D-05:** When the filtered+sorted result set is empty, show a "No items match your filters" message with a "Clear filters" `<a href>` link that navigates to the unfiltered shop.

### Claude's Discretion

- Exact HTML structure and CSS class names for the filter row (follow existing `shop-categories` / `category-btn` patterns).
- Whether to add a new `GetFilteredItemsAsync(type, rarities, sort)` service method or filter+sort in the controller/ViewModel after `GetPublishedItemsAsync`.
- URL parameter names for rarity and sort (e.g., `rarity`, `sort`).
- How "Default" sort order is defined (insertion order, name, or id).

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SHOP-01 | User can filter shop items by item rarity (one or more rarity values) | ASP.NET Core binds `?rarity=Rare&rarity=VeryRare` → `IList<ItemRarity>` natively; controller LINQ applies `.Where(i => rarities.Contains(i.Rarity))` post-fetch |
| SHOP-02 | User can sort shop items by price ascending or descending | Controller LINQ applies `.OrderBy(i => i.Price)` / `.OrderByDescending(i => i.Price)` based on `sort` string; default = insertion order (no OrderBy) |
| SHOP-03 | Filter and sort state persists in the URL as query parameters (bookmarkable) | `<form method="get">` serializes checkboxes + select as query params natively; no custom URL-building needed for form submission |
| SHOP-04 | Applying filter/sort does not require a page reload beyond the initial request (server-side, no JS dependency) | `<form method="get">` submits synchronously; all filtering happens server-side before View renders; works with JS disabled |

</phase_requirements>

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core 8 MVC | 8.0 | Controller action, model binding, routing | Project stack — confirmed in CLAUDE.md |
| Microsoft.AspNetCore.WebUtilities | 8.0 (built-in) | `QueryHelpers.AddQueryString` for tab URL construction | Ships with ASP.NET Core; zero new dependency |
| EF Core + LINQ | 9.0.6 | Post-fetch in-memory filtering and sorting | Already used in ShopService; LINQ on materialized list |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5.3.0 (CDN) | 5.3.0 | Filter row layout (`flex-wrap`, `form-select-sm`) | All existing shop UI; no change |
| Font Awesome 6.4.0 (CDN) | 6.4.0 | Icons in Apply/Clear buttons (`fa-filter`, `fa-times`) | Matches existing shop icon usage |

**No new NuGet packages required.** `QueryHelpers` is in `Microsoft.AspNetCore.WebUtilities`, which is referenced transitively by `Microsoft.NET.Sdk.Web`.

**Version verification:** No new packages to install; all required components are in the existing solution.

---

## Architecture Patterns

### Recommended Project Structure

No new files or directories required beyond changes to:

```
EuphoriaInn.Service/
├── Controllers/Shop/ShopController.cs     ← extend Index action params
├── ViewModels/ShopViewModels/
│   └── ShopIndexViewModel.cs              ← add SelectedRarities, SelectedSort
└── Views/Shop/
    ├── Index.cshtml                        ← add filter row, update tab links, update empty state
    └── (no new view files)
EuphoriaInn.Service/wwwroot/css/
    └── shop.css                            ← add ~50 lines for filter row classes
```

No changes to Domain, Repository, or test helpers are strictly required (test helper extension is optional for new integration tests).

### Pattern 1: Multi-Value Query String Binding (ASP.NET Core Native)

**What:** ASP.NET Core model binding handles repeated query string keys with the same name automatically when the action parameter is `IList<T>` or `T[]`.

**When to use:** Any action parameter that accepts multiple values from a GET form.

**Example:**

```csharp
// Source: ASP.NET Core docs — Model Binding, Collections
[HttpGet]
public async Task<IActionResult> Index(
    ItemType? type = null,
    IList<ItemRarity>? rarity = null,
    string? sort = null,
    CancellationToken token = default)
{
    var items = type.HasValue
        ? await shopService.GetItemsByTypeAsync(type.Value, token)
        : await shopService.GetPublishedItemsAsync(token);

    // Filter by rarity (server-side LINQ on materialized list)
    if (rarity?.Count > 0)
        items = items.Where(i => rarity.Contains(i.Rarity)).ToList();

    // Sort by price
    items = sort switch
    {
        "price_asc"  => items.OrderBy(i => i.Price).ToList(),
        "price_desc" => items.OrderByDescending(i => i.Price).ToList(),
        _            => items  // default: insertion order preserved
    };

    var viewModel = new ShopIndexViewModel
    {
        Items = mapper.Map<IList<ShopItemViewModel>>(items),
        SelectedType = type,
        SelectedRarities = rarity ?? [],
        SelectedSort = sort
    };
    // ... existing UserPurchases population unchanged
}
```

**Confidence:** HIGH — confirmed by ASP.NET Core model binding documentation and existing codebase `IList<T>` patterns.

### Pattern 2: Tab URL Construction with Multi-Value Rarity State

**What:** `Url.Action` with anonymous objects does not serialize `IList<ItemRarity>` to repeated keys. `QueryHelpers.AddQueryString` handles this correctly.

**When to use:** Any Razor view that needs to build URLs with repeated query string parameters.

**Example:**

```csharp
// Source: Microsoft.AspNetCore.WebUtilities.QueryHelpers
// In Index.cshtml @functions block or a Razor helper

@functions {
    string BuildTabUrl(ItemType? tabType, IList<ItemRarity> selectedRarities, string? selectedSort)
    {
        var baseUrl = Url.Action("Index", "Shop")!;
        var queryParams = new Dictionary<string, StringValues>();

        if (tabType.HasValue)
            queryParams["type"] = tabType.Value.ToString();

        foreach (var r in selectedRarities)
            queryParams.Add("rarity", r.ToString());  // repeated keys → repeated values

        if (!string.IsNullOrEmpty(selectedSort))
            queryParams["sort"] = selectedSort;

        return QueryString.Create(queryParams).HasValue
            ? baseUrl + QueryString.Create(queryParams)
            : baseUrl;
    }
}
```

**Alternative (simpler for this case):** Use `QueryHelpers.AddQueryString(baseUrl, pairs)` where pairs is an `IEnumerable<KeyValuePair<string, string?>>` built from the rarity list. Both approaches work identically at runtime.

**Confidence:** HIGH — `QueryHelpers` is part of ASP.NET Core framework, not a third-party library.

### Pattern 3: ShopIndexViewModel Extension

**What:** Add two new properties to carry filter state into the view for rendering checked/selected state.

```csharp
public class ShopIndexViewModel
{
    // existing properties unchanged ...

    public IList<ItemRarity> SelectedRarities { get; set; } = [];
    public string? SelectedSort { get; set; }

    // Convenience: true when any filter is active (drives "Clear Filters" button visibility)
    public bool HasActiveFilters => SelectedRarities.Count > 0 || SelectedSort != null;
}
```

### Anti-Patterns to Avoid

- **Url.Action with anonymous IList:** `@Url.Action("Index", "Shop", new { rarity = Model.SelectedRarities })` will serialize the list as a single comma-separated string or array notation, not as repeated `rarity=` keys. Always use `QueryHelpers` or manual `QueryString.Create` for multi-value params.
- **Extending IShopService with a new method for this phase:** The filter/sort is purely presentation-layer logic (ordering and subsetting an already-fetched list). Adding `GetFilteredItemsAsync` to the service interface creates unnecessary abstraction for in-memory LINQ that operates on a materialized list that's already in the controller.
- **Encoding rarity as int in the URL:** Use the enum name (`Rare`, `VeryRare`) not the int value. ASP.NET Core model binding resolves enum names by default; names are also readable in bookmarked URLs.
- **Using `[FromQuery]` attribute for the rarity list:** Not needed. ASP.NET Core binds `IList<ItemRarity> rarity` from query string without any attribute decoration — same as how `ItemType? type` already works.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Repeated query string keys in URL | Custom string interpolation / `StringBuilder` | `Microsoft.AspNetCore.Http.QueryString.Create(IEnumerable<KeyValuePair<string,StringValues>>)` | Handles encoding, empty values, and key repetition correctly |
| Multi-value form field binding | Manual `Request.Query["rarity"]` parsing | `IList<ItemRarity> rarity` action parameter | ASP.NET Core model binding handles enum parsing, null safety, and type coercion |
| Preserving existing filter state in form | Hidden inputs manually duplicated | Single `<input type="hidden" name="type" value="...">` inside the form + form serialization | The form already serializes checkbox and select state; only `type` (not managed by the form's own controls) needs a hidden field |

**Key insight:** All the hard work (multi-value binding, URL encoding, enum parsing) is handled by ASP.NET Core and the browser's form serialization. The implementation is primarily wiring, not infrastructure.

---

## Common Pitfalls

### Pitfall 1: VeryRare CSS Class Name Mismatch

**What goes wrong:** The `ItemRarity.VeryRare` enum value serializes to `"VeryRare"` (PascalCase). The existing shop CSS uses `.rarity-veryrare` (all lowercase). If the checkbox renders `rarity-VeryRare` the background color doesn't apply.

**Why it happens:** `rarity.ToString().ToLower()` on `"VeryRare"` → `"veryrare"` (correct). But if someone uses `rarity.ToString()` directly without `.ToLower()` the CSS class breaks.

**How to avoid:** Always apply `.ToLower()` when building rarity CSS classes: `rarity-@rarity.ToString().ToLower()`. The UI-SPEC confirms this pattern. The existing item cards already do this correctly (`data-item-name="@item.Name.ToLowerInvariant()"` and `rarity-@item.Rarity.ToString().ToLower()`).

**Warning signs:** Very Rare items show no background color on their rarity badge in the filter row.

### Pitfall 2: Tab "All Items" URL Drops Rarity State

**What goes wrong:** The "All Items" tab links to `@Url.Action("Index", "Shop")` which produces `/Shop` — stripping all rarity and sort state. User changes tab, all filters reset.

**Why it happens:** The existing tab links were written before filter state existed; they use static `Url.Action` with only the `type` parameter.

**How to avoid:** Update all four tab links (including "All Items") to use `BuildTabUrl(null, Model.SelectedRarities, Model.SelectedSort)` — note `null` for tabType on "All Items". The helper must include the current rarity and sort params even when type is null.

**Warning signs:** Clicking "All Items" after setting a rarity filter shows all rarities instead of the filtered set.

### Pitfall 3: Empty SelectedRarities Causes Unintended LINQ Behavior

**What goes wrong:** If `rarity` parameter is null and the view passes `null` to the form's `@(Model.SelectedRarities.Contains(rarity))` check, a NullReferenceException occurs at render time.

**Why it happens:** `IList<ItemRarity>?` is nullable in the action signature but the ViewModel property should be initialized to an empty list.

**How to avoid:** Initialize `SelectedRarities` with `= []` in the ViewModel property declaration (already shown in Pattern 3 above). The controller assigns `SelectedRarities = rarity ?? []` when building the ViewModel.

**Warning signs:** NullReferenceException in Index.cshtml at the checkbox rendering loop.

### Pitfall 4: Form GET Submission Loses `type` Parameter

**What goes wrong:** The filter form uses `<form method="get">` but the `type` parameter is not a form control inside the form — it's the active category tab state. Without a hidden input, form submission to `/Shop?rarity=Rare&sort=price_asc` silently drops the `type` filter.

**Why it happens:** `<form method="get">` only serializes the named inputs that are children of the form. The `type` parameter comes from the tab links, not the filter form.

**How to avoid:** Include `<input type="hidden" name="type" value="@Model.SelectedType" />` inside the filter form, conditional on `Model.SelectedType != null`. The UI-SPEC already specifies this pattern.

**Warning signs:** After applying a rarity filter while on the "Equipment" tab, the result shows items of all types.

### Pitfall 5: QueryString.Create Requires `StringValues` Not `string`

**What goes wrong:** `QueryString.Create(IEnumerable<KeyValuePair<string, StringValues>>)` requires `StringValues` values, not plain `string`. Passing `string` causes a compiler error or requires an explicit cast.

**Why it happens:** `StringValues` is the internal type used by ASP.NET Core for multi-value string collections (`Microsoft.Extensions.Primitives`).

**How to avoid:** Either use `new StringValues(rarityName)` for single values, or use the simpler overload `QueryString.Create(IEnumerable<KeyValuePair<string, string?>>)` which is also available and works with plain strings. Verify the overload resolution when writing the helper.

---

## Code Examples

### Index Action — Complete Updated Signature

```csharp
// ShopController.cs
[HttpGet]
public async Task<IActionResult> Index(
    ItemType? type = null,
    IList<ItemRarity>? rarity = null,
    string? sort = null,
    CancellationToken token = default)
```

### ViewModel Extension

```csharp
// ShopIndexViewModel.cs — additions only
public IList<ItemRarity> SelectedRarities { get; set; } = [];
public string? SelectedSort { get; set; }
public bool HasActiveFilters => SelectedRarities.Count > 0 || SelectedSort != null;
```

### Tab URL Helper (Razor @functions block)

```csharp
// Views/Shop/Index.cshtml — @functions block at top of file
@using Microsoft.AspNetCore.Http.Extensions
@functions {
    string BuildTabUrl(ItemType? tabType, IList<ItemRarity> rarities, string? sort)
    {
        var pairs = new List<KeyValuePair<string, string?>>();
        if (tabType.HasValue)
            pairs.Add(new("type", tabType.Value.ToString()));
        foreach (var r in rarities)
            pairs.Add(new("rarity", r.ToString()));
        if (!string.IsNullOrEmpty(sort))
            pairs.Add(new("sort", sort));
        var qs = QueryString.Create(pairs);
        return Url.Action("Index", "Shop") + qs.Value;
    }
}
```

### Checkbox Rendering Pattern

```html
@{
    var allRarities = Enum.GetValues<EuphoriaInn.Domain.Enums.ItemRarity>();
}
@foreach (var r in allRarities)
{
    <label class="filter-check-label">
        <input type="checkbox" name="rarity" value="@r"
               @(Model.SelectedRarities.Contains(r) ? "checked" : "") />
        <span class="item-rarity rarity-@r.ToString().ToLower()">
            @r.ToString().Replace("VeryRare", "Very Rare")
        </span>
    </label>
}
```

### Empty State Conditional Logic

```html
@if (!Model.Items.Any() && Model.HasActiveFilters)
{
    <!-- Filtered empty state (D-05) -->
    <div class="empty-shop">
        <i class="fas fa-filter fa-3x mb-3 text-muted"></i>
        <h3>No items match your filters</h3>
        <p>Try removing some rarity filters or changing the sort order.</p>
        <a href="@Url.Action("Index", "Shop", Model.SelectedType != null ? new { type = Model.SelectedType } : null)"
           class="btn btn-sm filter-clear-btn mt-2">
            <i class="fas fa-times me-1"></i>Clear filters
        </a>
    </div>
}
else if (!Model.Items.Any())
{
    <!-- Shop globally empty — existing copy unchanged -->
    <div class="empty-shop">
        <i class="fas fa-store-slash fa-3x mb-3 text-muted"></i>
        <h3>The shop is currently empty</h3>
        <p>Check back later for new items, or contact a Dungeon Master about stocking the shop.</p>
    </div>
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Client-side JS filter (existing JS text search) | Server-side query param filter (this phase) | Phase 5 | No JS required; URL is bookmarkable; works with JS disabled. Coexists with the existing JS text search (unaffected). |

**No deprecated patterns introduced by this phase.**

---

## Open Questions

1. **`QueryString.Create` overload availability in .NET 8**
   - What we know: `Microsoft.AspNetCore.Http.QueryString.Create(IEnumerable<KeyValuePair<string, string?>>)` is documented for .NET 6+.
   - What's unclear: Whether the exact overload signature accepting `string?` values (not `StringValues`) is present in `net8.0` without additional using statements.
   - Recommendation: During implementation, verify with a quick compile; if unavailable, use `QueryHelpers.AddQueryString` from `Microsoft.AspNetCore.WebUtilities` which is definitively available and accepts `IEnumerable<KeyValuePair<string, string?>>`.

2. **"Default" sort stability**
   - What we know: CONTEXT.md discretion section defines default as "insertion order". `GetPublishedItemsAsync` returns items in EF Core default order (which is typically Id ASC for SQL Server, but not guaranteed).
   - What's unclear: Whether the EF query has an explicit `OrderBy` or relies on database insertion order.
   - Recommendation: Check `ShopRepository.GetPublishedItemsAsync` before implementing. If no explicit order is set, "default" is stable enough for a small dataset. No change required to the repository.

---

## Environment Availability

Step 2.6: SKIPPED — this phase is purely code and view changes. No external tools, services, or CLIs beyond the existing .NET 8 SDK and SQL Server are introduced.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| Config file | `xunit.runner.json` in `EuphoriaInn.IntegrationTests/` |
| Quick run command | `dotnet test EuphoriaInn.UnitTests/ --no-build -x` |
| Full suite command | `dotnet test --no-build` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHOP-01 | GET /Shop?rarity=Rare returns only Rare items | Integration | `dotnet test EuphoriaInn.IntegrationTests/ --filter "ShopController" --no-build` | ❌ Wave 0 |
| SHOP-01 | GET /Shop?rarity=Rare&rarity=Uncommon returns items of both rarities | Integration | same | ❌ Wave 0 |
| SHOP-01 | GET /Shop?rarity=Legendary with no Legendary items returns empty with filter message | Integration | same | ❌ Wave 0 |
| SHOP-02 | GET /Shop?sort=price_asc returns items ordered by price ascending | Integration | same | ❌ Wave 0 |
| SHOP-02 | GET /Shop?sort=price_desc returns items ordered by price descending | Integration | same | ❌ Wave 0 |
| SHOP-03 | Response HTML contains `rarity=Rare` and `sort=price_asc` in category tab href after filter applied | Integration | same | ❌ Wave 0 |
| SHOP-04 | All filter behaviors covered by integration tests (server-side; JS disabled is implicit) | Integration | same | — |

**Note:** The existing `ShopControllerIntegrationTests.cs` does not have filter/sort test methods. New tests extend this file. The `TestDataHelper.CreateShopItemAsync` method has a hard-coded `Rarity = 0` (Common). Wave 0 must either extend this helper to accept a `rarity` parameter or create items via direct `DbContext` manipulation in the new tests.

### Sampling Rate

- **Per task commit:** `dotnet test EuphoriaInn.UnitTests/ --no-build`
- **Per wave merge:** `dotnet test --no-build`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] New test methods in `EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs` — covers SHOP-01, SHOP-02, SHOP-03
- [ ] `TestDataHelper.CreateShopItemAsync` extended with optional `ItemRarity rarity = ItemRarity.Common` parameter — needed for SHOP-01 multi-rarity tests
- [ ] No framework install needed — xUnit and FluentAssertions already in project

---

## Sources

### Primary (HIGH confidence)

- Codebase direct read — `ShopController.cs`, `ShopIndexViewModel.cs`, `ShopItemViewModel.cs`, `IShopService.cs`, `ShopService.cs`, `Views/Shop/Index.cshtml`, `shop.css`
- Codebase direct read — `ItemRarity.cs`, `ItemType.cs` enum definitions
- Codebase direct read — `TestDataHelper.cs`, `ShopControllerIntegrationTests.cs`
- `05-CONTEXT.md` — all locked decisions (D-01 through D-05)
- `05-UI-SPEC.md` — approved HTML structure, CSS additions, interaction contract, query parameter schema
- ASP.NET Core 8 model binding documentation (knowledge base, HIGH confidence for stable .NET APIs)

### Secondary (MEDIUM confidence)

- `QueryHelpers.AddQueryString` and `QueryString.Create` API — training knowledge cross-verified against known ASP.NET Core 8 framework inclusions; `Microsoft.AspNetCore.WebUtilities` is a first-party Microsoft package present in all ASP.NET Core web projects.

### Tertiary (LOW confidence)

- None. All claims are verifiable from codebase inspection or stable framework documentation.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; existing project dependencies cover all needs
- Architecture: HIGH — patterns directly derived from existing `ShopController.Index` action and codebase conventions
- Pitfalls: HIGH — all pitfalls identified from direct codebase inspection (VeryRare CSS, tab URL, hidden type field, null list initialization)
- Test map: HIGH — existing test infrastructure confirmed by direct file read; Wave 0 gaps are explicit

**Research date:** 2026-04-20
**Valid until:** 2026-06-01 (stable ASP.NET Core APIs; no fast-moving dependencies)
