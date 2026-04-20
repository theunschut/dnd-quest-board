---
phase: 05
phase_name: Shop Filter & Sort
status: draft
created: 2026-04-20
design_system: none (Bootstrap 5 CDN + custom CSS)
---

# UI-SPEC: Phase 05 — Shop Filter & Sort

## 1. Design System

**Tool:** None (no shadcn / no components.json). Project uses Bootstrap 5.3.0 via CDN with custom CSS layers:
- `wwwroot/css/site.css` — global tokens
- `wwwroot/css/shop.css` — shop-specific component rules
- Font Awesome 6.4.0 (CDN) — icon library
- Google Fonts: Cinzel (serif, headings only)

**Registry safety gate:** Not applicable — no shadcn registry in use.

---

## 2. Spacing

8-point scale. All margin/padding values must be multiples of 4px.

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | Checkbox gap, inline icon spacing (`me-1`) |
| sm | 8px | Label-to-control gap, checkbox row internal padding |
| md | 16px | Filter row outer padding, form element group gap |
| lg | 24px | Section separation (filter row ↔ category tabs) |
| xl | 32px | Not used in this phase |

Bootstrap utility classes map: `me-1` = 4px, `me-2` = 8px, `p-2` = 8px, `p-3` = 16px, `mb-3` = 16px.

No exceptions for touch targets in this phase — all controls are standard Bootstrap form elements with default heights (38px inputs, 24px checkboxes).

---

## 3. Typography

**Source:** Detected from existing shop.css and site layout. Pre-populated; not re-asked.

| Role | Size | Weight | Line-height | Element |
|------|------|--------|-------------|---------|
| Section label | 14px (0.875rem) | 500 (medium) | 1.4 | Filter row labels, checkbox labels |
| Body / control text | 16px (1rem) | 400 (regular) | 1.5 | Select dropdown text, button text |
| Button label | 14px (0.875rem) | 500 (medium) | 1 | "Apply Filters" button, "Clear filters" link |
| Empty state heading | 20px (1.25rem) | 600 (semibold) | 1.3 | `<h3>` inside `.empty-shop` |

**Fonts:** Cinzel for section labels only if the filter row uses a heading element. All other filter row text uses the default Bootstrap sans-serif stack (system-ui / Segoe UI).

Maximum 4 sizes in use; maximum 2 weights (400 regular, 600 semibold). The 500-weight buttons are Bootstrap's default and should remain; do not introduce a third custom weight.

---

## 4. Color Contract

**Theme:** Witcher 3 dark — dark navy backgrounds with amber accent.

| Role | Value | 60/30/10 |
|------|-------|----------|
| Dominant surface | `rgba(0,0,0,0.6)` — `.shop-inventory` background | 60% |
| Secondary surfaces | `rgba(0,0,0,0.7)` — `.category-btn`, filter form background | 30% |
| Accent | `#ffc107` (amber) | 10% |
| Text primary | `#F4E4BC` (warm parchment) | — |
| Text muted | `rgba(244,228,188,0.7)` | — |
| Destructive | Not applicable in this phase | — |

**Accent reserved for:**
- Active category button background (`.category-btn.active`)
- "Apply Filters" button border/hover state
- Filter row border (matches existing 2px `#444` → amber on focus)
- Checkbox `accent-color` CSS property: `#ffc107`

**Rarity-specific colors (pre-existing, do not change):**

| Rarity | CSS class | Background |
|--------|-----------|------------|
| Common | `.rarity-common` | `#6c757d` (gray) |
| Uncommon | `.rarity-uncommon` | `#198754` (green) |
| Rare | `.rarity-rare` | `#0d6efd` (blue) |
| Very Rare | `.rarity-veryrare` | `#6f42c1` (purple) |
| Legendary | `.rarity-legendary` | gradient red→amber + glow animation |

These rarity colors must be reused on the checkbox labels in the filter row (as small badge-style indicators matching the `.item-rarity` pattern). Do not introduce new rarity color values.

---

## 5. Component Inventory

### 5.1 Filter Row (new component)

**Location:** Between `.shop-categories` and `.shop-search` (inserted above the existing search bar).

**HTML structure:**

```html
<form method="get" action="@Url.Action("Index", "Shop")" class="shop-filter-row mb-3">
  <!-- Hidden: carry active type forward -->
  @if (Model.SelectedType != null)
  {
    <input type="hidden" name="type" value="@Model.SelectedType" />
  }

  <!-- Rarity checkboxes -->
  <div class="filter-rarity-group">
    <span class="filter-label">Rarity:</span>
    @foreach (var rarity in allRarities)
    {
      <label class="filter-check-label">
        <input type="checkbox" name="rarity" value="@rarity"
               @(Model.SelectedRarities.Contains(rarity) ? "checked" : "") />
        <span class="item-rarity rarity-@rarity.ToString().ToLower()">
          @rarityDisplayName
        </span>
      </label>
    }
  </div>

  <!-- Sort select -->
  <div class="filter-sort-group">
    <label for="sortSelect" class="filter-label">Sort:</label>
    <select id="sortSelect" name="sort" class="form-select form-select-sm filter-sort-select">
      <option value="" @(Model.SelectedSort == null ? "selected" : "")>Default</option>
      <option value="price_asc" @(Model.SelectedSort == "price_asc" ? "selected" : "")>Price &#8593;</option>
      <option value="price_desc" @(Model.SelectedSort == "price_desc" ? "selected" : "")>Price &#8595;</option>
    </select>
  </div>

  <!-- Submit -->
  <button type="submit" class="btn btn-sm filter-apply-btn">
    <i class="fas fa-filter me-1"></i>Apply
  </button>

  <!-- Clear (only rendered when active filters exist) -->
  @if (Model.SelectedRarities.Any() || Model.SelectedSort != null)
  {
    <a href="@Url.Action("Index", "Shop", Model.SelectedType != null ? new { type = Model.SelectedType } : null)"
       class="btn btn-sm filter-clear-btn">
      <i class="fas fa-times me-1"></i>Clear
    </a>
  }
</form>
```

**CSS additions to `shop.css`:**

```css
.shop-filter-row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
    padding: 8px 16px;
    background: rgba(0, 0, 0, 0.5);
    border: 1px solid #444;
    border-radius: 6px;
    margin-bottom: 16px;
}

.filter-label {
    color: #F4E4BC;
    font-size: 0.875rem;
    font-weight: 500;
    white-space: nowrap;
    margin-right: 4px;
}

.filter-rarity-group {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px;
}

.filter-check-label {
    display: flex;
    align-items: center;
    gap: 4px;
    cursor: pointer;
}

.filter-check-label input[type="checkbox"] {
    accent-color: #ffc107;
    width: 14px;
    height: 14px;
    cursor: pointer;
}

.filter-sort-group {
    display: flex;
    align-items: center;
    gap: 4px;
    margin-left: 8px;
}

.filter-sort-select {
    background: rgba(0, 0, 0, 0.7);
    border-color: #444;
    color: #F4E4BC;
    min-width: 120px;
    font-size: 0.875rem;
}

.filter-sort-select:focus {
    border-color: #ffc107;
    box-shadow: 0 0 0 0.2rem rgba(255, 193, 7, 0.25);
    background: rgba(0, 0, 0, 0.8);
    color: #F4E4BC;
}

.filter-sort-select option {
    background: #1a1a2e;
    color: #F4E4BC;
}

.filter-apply-btn {
    background: rgba(0, 0, 0, 0.7);
    border: 2px solid #444;
    color: #fff;
    padding: 0.375rem 0.75rem;
    border-radius: 6px;
    font-size: 0.875rem;
    font-weight: 500;
    transition: all 0.3s ease;
}

.filter-apply-btn:hover {
    background: #ffc107;
    border-color: #ffc107;
    color: #000;
}

.filter-clear-btn {
    background: transparent;
    border: 1px solid #6c757d;
    color: rgba(244, 228, 188, 0.7);
    padding: 0.375rem 0.75rem;
    border-radius: 6px;
    font-size: 0.875rem;
    transition: all 0.3s ease;
}

.filter-clear-btn:hover {
    border-color: #dc3545;
    color: #dc3545;
}
```

### 5.2 Category Tab Links (modified)

The existing four `<a href>` category tabs must carry forward active rarity and sort state when clicked. The URL must include all active `rarity` parameters and the `sort` parameter.

**Pattern:**

```csharp
// In controller: build RouteValueDictionary with stacked params
// In view: helper method or inline Url.Action with routeValues object
// Each tab <a href> must include rarity[] and sort alongside type
```

Tab link Razor pattern (per tab):
```html
<a href="@BuildTabUrl(ItemType.Equipment, Model.SelectedRarities, Model.SelectedSort)"
   class="category-btn @(Model.SelectedType == ItemType.Equipment ? "active" : "")">
    <i class="fas fa-shield-alt me-2"></i>Equipment
</a>
```

Active state: same existing `.category-btn.active` class — amber fill, black text.

### 5.3 Empty State (filter result is empty)

**Trigger:** `Model.Items` is empty and `Model.SelectedRarities.Any() || Model.SelectedSort != null`.

**Render:** Replace existing generic empty shop block with a filter-specific message.

```html
<div class="empty-shop">
    <i class="fas fa-filter fa-3x mb-3 text-muted"></i>
    <h3>No items match your filters</h3>
    <p>Try removing some rarity filters or changing the sort order.</p>
    <a href="@Url.Action("Index", "Shop", Model.SelectedType != null ? new { type = Model.SelectedType } : null)"
       class="btn btn-sm filter-clear-btn mt-2">
        <i class="fas fa-times me-1"></i>Clear filters
    </a>
</div>
```

When the shop is globally empty (no items at all, no filters active), use existing copy: "The shop is currently empty."

---

## 6. Interaction Contract

### 6.1 Filter and Sort Submission

- Mechanism: Standard HTML `<form method="get">` — no JavaScript required.
- All filter and sort state lives in query parameters: `?type=Equipment&rarity=Rare&rarity=VeryRare&sort=price_asc`.
- The form is submitted by clicking "Apply". No per-checkbox or per-select auto-submit.
- On submit the page performs a full GET request — no partial updates.
- The URL after submission is bookmarkable and shareable (SHOP-03).
- JavaScript disabled: works identically. The form submits with native browser behavior.

### 6.2 Category Tab Navigation

- Each tab `<a href>` carries forward active `rarity[]` parameters and `sort` from the current URL.
- Clicking a tab changes `type` but preserves rarity and sort state.
- "All Items" tab links to `?rarity=...&sort=...` (no `type` param).
- Active tab detection: compare `Model.SelectedType` to the tab's type value.

### 6.3 Query Parameter Schema

| Parameter | Type | Values | Notes |
|-----------|------|--------|-------|
| `type` | string | `Equipment`, `MagicItem`, `QuestItems` | Existing, unchanged |
| `rarity` | string (repeatable) | `Common`, `Uncommon`, `Rare`, `VeryRare`, `Legendary` | Multi-value; ASP.NET Core binds to `IList<ItemRarity>` |
| `sort` | string | `price_asc`, `price_desc`, `` (empty/absent = default) | Single value |

Default sort order (when `sort` is absent): original insertion order (database Id ascending). This is the existing behavior of `GetPublishedItemsAsync`.

### 6.4 States and Transitions

| State | Condition | UI |
|-------|-----------|-----|
| No filters active | No rarity selected, no sort chosen | Filter row visible; all checkboxes unchecked; sort shows "Default"; "Clear" button hidden |
| Filters active | Any rarity checked OR sort non-default | "Clear" button visible; matching checkboxes checked; sort shows selected value |
| Results exist | `Model.Items.Any()` | Normal inventory grid |
| Filtered empty | `!Model.Items.Any()` AND filters active | Filter-specific empty state with "Clear filters" link |
| Shop globally empty | `!Model.Items.Any()` AND no filters | Generic empty state: "The shop is currently empty" |

---

## 7. Copywriting Contract

| Element | Copy |
|---------|------|
| Filter row label — rarity | "Rarity:" |
| Filter row label — sort | "Sort:" |
| Sort option — default | "Default" |
| Sort option — ascending | "Price ↑" |
| Sort option — descending | "Price ↓" |
| Apply button | "Apply" (with `fa-filter` icon) |
| Clear button | "Clear" (with `fa-times` icon) |
| Filtered empty heading | "No items match your filters" |
| Filtered empty subtext | "Try removing some rarity filters or changing the sort order." |
| Filtered empty CTA | "Clear filters" (plain `<a>` link, not a button) |
| Shop globally empty heading | "The shop is currently empty" (unchanged from existing) |
| Shop globally empty subtext | "Check back later for new items, or contact a Dungeon Master about stocking the shop." (unchanged) |

No destructive actions in this phase.

---

## 8. Accessibility Contract

- All checkboxes have visible `<label>` elements with `for` / `id` association OR wrap the input (implicit label pattern used above).
- The sort `<select>` has an explicit `<label for="sortSelect">`.
- "Clear" link is an `<a href>` (not a button) so it is navigable without JavaScript.
- Rarity badge spans inside checkbox labels are decorative — they do not replace the label text; the rarity name appears in the badge text itself.
- Filter form uses `method="get"` so browser back navigation restores filter state correctly.
- Color is not the only differentiator for rarity — the rarity name text is always present inside the badge.

---

## 9. Responsive Contract

| Breakpoint | Filter Row Behavior |
|------------|-------------------|
| ≥992px (lg) | Single horizontal row: label + checkboxes + sort + buttons |
| 768–991px (md) | `flex-wrap: wrap` — checkboxes wrap to second line if needed |
| <768px (sm) | Rarity checkboxes stack; sort and apply remain inline on their own row |

The `flex-wrap: wrap` on `.shop-filter-row` handles wrapping automatically. No JavaScript required. The purchase history side panel collapses on `<992px` per existing CSS — filter row is unaffected by this.

---

## 10. Out of Scope (this phase)

- Client-side live filtering by rarity (no JS dependency per SHOP-04)
- Price range slider
- "Select all" / "Clear all" rarity shortcut buttons
- Sorting by name or rarity
- Persistence of filter preferences across sessions (cookie/localStorage)
- Pagination (deferred to v2 PERF-01)

---

## 11. Pre-Population Sources

| Field | Source |
|-------|--------|
| Design system (Bootstrap 5, Font Awesome, Cinzel) | Detected from `site.css`, `shop.css`, view layout |
| Color tokens | Detected from `shop.css` (amber `#ffc107`, surface `rgba(0,0,0,0.6)`, text `#F4E4BC`) |
| Rarity colors and CSS classes | Detected from `shop.css` (`.rarity-*` classes) |
| Typography sizes | Detected from `shop.css` and Bootstrap defaults |
| Category tab structure and class names | Detected from `Views/Shop/Index.cshtml` |
| Filter row placement | CONTEXT.md D-01 (above item grid, below category tabs) |
| Submit mechanism | CONTEXT.md D-02 (single "Apply" button, no per-checkbox auto-submit) |
| Sort control type | CONTEXT.md D-03 (`<select>` dropdown, same form as checkboxes) |
| URL parameter stacking | CONTEXT.md D-04 (type + rarity + sort all in one URL) |
| Empty state approach | CONTEXT.md D-05 ("No items match your filters" + "Clear filters" link) |
| Rarity enum values | `EuphoriaInn.Domain/Enums/ItemRarity.cs` |
| Default sort definition | Discretion — insertion order (Id ASC), matching existing `GetPublishedItemsAsync` behavior |
| Query parameter names | Discretion — `rarity`, `sort` (consistent with existing `type` convention) |
| "Clear" button visibility | Discretion — hidden when no filters active; shown when any filter active |
