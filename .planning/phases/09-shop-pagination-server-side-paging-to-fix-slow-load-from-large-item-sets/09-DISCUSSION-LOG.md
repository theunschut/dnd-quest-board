# Phase 9: Shop Pagination - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-21
**Phase:** 09-shop-pagination-server-side-paging-to-fix-slow-load-from-large-item-sets
**Areas discussed:** Page size & scope, Pagination UI component, JS text search fate, Filter+sort+page stacking

---

## Page size & scope

| Option | Description | Selected |
|--------|-------------|----------|
| 12 items | Fits neatly into a 3 or 4-column grid | ✓ |
| 20 items | More items per page, fewer clicks | |
| 24 items | Divisible by 2, 3, and 4 | |
| You decide | Leave page size to Claude's discretion | |

**User's choice:** 12 items per page

| Option | Description | Selected |
|--------|-------------|----------|
| Player shop only | Management view is DM/admin-only | ✓ |
| Both player shop and management view | Paginate both for consistency | |

**User's choice:** Player-facing shop only (`ShopController`). `ShopManagementController` is out of scope.

---

## Pagination UI component

| Option | Description | Selected |
|--------|-------------|----------|
| Bootstrap numbered pager | Previous / 1 2 3 ... / Next links. Bookmarkable URLs, Bootstrap 5 style | ✓ |
| 'Load more' button | Appends next page; no direct-jump | |
| Infinite scroll | Scroll-triggered fetch; requires JS; complex | |

**User's choice:** Bootstrap numbered pager with bookmarkable URLs.

---

## JS text search fate

| Option | Description | Selected |
|--------|-------------|----------|
| Keep it — searches current page only | Zero backend changes | |
| Remove it entirely | Phase 5 already gave rarity filter + sort | |
| Replace with server-side name search param | Add ?search= query param, filter in repository | ✓ |

**User's choice:** Replace with server-side name search param.

Follow-up — search submit style:
| Option | Description | Selected |
|--------|-------------|----------|
| Text input + Submit button | User types, hits Enter or clicks Search | ✓ |
| Live-search on keyup with debounce | Requires JS fetch; more complex | |

**User's choice:** Text input + Submit button inside the existing filter form.

Follow-up — search stacking:
| Option | Description | Selected |
|--------|-------------|----------|
| Yes — all params stack together | Consistent with Phase 5 pattern | ✓ |
| No — search resets other filters | Simpler but breaks stacking principle | |

**User's choice:** All params stack: `?type=&rarity=&sort=&search=&page=`.

---

## Filter+sort+page stacking

| Option | Description | Selected |
|--------|-------------|----------|
| Reset to page 1 on filter/sort/search change | Standard behavior | ✓ |
| Stay on current page | Non-standard; risky if result set shrinks | |

**User's choice:** Reset to page 1 when any filter, sort, or search changes.

| Option | Description | Selected |
|--------|-------------|----------|
| Pager links carry all filter/sort/search state | Filters survive page navigation | ✓ |
| Pager links only include ?page=N | Filters reset on every page click | |

**User's choice:** Pager links include all active params.

---

## Claude's Discretion

- Exact Bootstrap pager markup and window size (e.g., ±2 pages from current, always show first/last)
- Whether to add a paged overload to `IShopRepository`/`IShopService` or create a new unified `GetPagedPublishedItemsAsync`
- Whether rarity filter and sort remain post-fetch in controller or move into the database query
- The exact URL parameter name for search (e.g., `search`)

## Deferred Ideas

None — discussion stayed within phase scope.
