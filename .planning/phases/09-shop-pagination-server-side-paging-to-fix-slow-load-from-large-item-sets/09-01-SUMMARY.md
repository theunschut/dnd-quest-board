---
phase: 09-shop-pagination-server-side-paging-to-fix-slow-load-from-large-item-sets
plan: 01
subsystem: api
tags: [efcore, pagination, shop, csharp, repository-pattern]

requires:
  - phase: 05-shop-filter-sort
    provides: IShopService interface and filter/sort groundwork in ShopController

provides:
  - IShopRepository.GetPagedPublishedItemsAsync — composable EF Core IQueryable with Skip/Take + CountAsync
  - IShopService.GetPagedPublishedItemsAsync — domain-model-returning service delegation
  - ShopIndexViewModel extended with SearchQuery, CurrentPage, TotalPages, TotalItems, HasActiveSearch
  - Failing unit + integration tests proving the RED/GREEN TDD cycle

affects:
  - 09-02 (Plan 02 will wire ShopController.Index + views against these contracts)

tech-stack:
  added: []
  patterns:
    - Composable IQueryable with CountAsync before OrderBy/Skip/Take — count runs before pagination mutates the query
    - Repository returns entities; service maps to domain models (avoids circular project reference)
    - Obsolete stubs on removed ViewModel properties so Razor views compile until Plan 02 removes them

key-files:
  created:
    - EuphoriaInn.UnitTests/Services/ShopServiceTests.cs
  modified:
    - EuphoriaInn.Repository/Interfaces/IShopRepository.cs
    - EuphoriaInn.Repository/ShopRepository.cs
    - EuphoriaInn.Domain/Interfaces/IShopService.cs
    - EuphoriaInn.Domain/Services/ShopService.cs
    - EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs
    - EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs

key-decisions:
  - "Repository returns (IList<ShopItemEntity>, int) not (IList<ShopItem>, int) — avoids circular project reference since Repository does not reference Domain"
  - "Service layer maps entity list to domain model list post-fetch — consistent with existing GetPublishedItemsAsync pattern"
  - "Obsolete stubs kept for EquipmentItems/MagicItems in ViewModel so Razor views compile; Plan 02 removes them after updating Index.cshtml"
  - "Integration test SeedPublishedShopItemsAsync creates context directly (not via TestDataHelper) to support varying item properties"

patterns-established:
  - "Paged repo pattern: CountAsync before OrderBy; Skip/Take applied after sort selection"

requirements-completed: [SHOP-PAG-01, SHOP-PAG-02, SHOP-PAG-03, SHOP-PAG-07]

duration: 20min
completed: 2026-04-21
---

# Phase 09 Plan 01: Shop Pagination — Data Layer Summary

**EF Core composable IQueryable pagination with filter/search/sort all applied before Skip/Take, plus extended ViewModel and TDD-confirmed test coverage**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-04-21T21:05:00Z
- **Completed:** 2026-04-21T21:25:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Added `GetPagedPublishedItemsAsync` to repository and service layers with composable EF Core query
- Extended `ShopIndexViewModel` with `SearchQuery`, `CurrentPage`, `TotalPages`, `TotalItems`, `HasActiveSearch`
- TDD RED/GREEN cycle confirmed: unit test + integration smoke test both green; 6 Wave-2 integration tests skipped until Plan 02

## Task Commits

1. **Task 1: Failing tests (TDD RED)** — `be5169a` (test)
2. **Task 2: Repository + service + ViewModel implementation (TDD GREEN)** — `42d7350` (feat)

## Files Created/Modified

- `EuphoriaInn.UnitTests/Services/ShopServiceTests.cs` — New: unit test for service delegation + 6 skipped integration test stubs
- `EuphoriaInn.Repository/Interfaces/IShopRepository.cs` — Added `GetPagedPublishedItemsAsync(int? type, IList<int>? rarityInts, string? sort, string? search, int page, int pageSize, CancellationToken)` returning `(IList<ShopItemEntity>, int)`
- `EuphoriaInn.Repository/ShopRepository.cs` — Added implementation with composable IQueryable, CountAsync before Skip/Take
- `EuphoriaInn.Domain/Interfaces/IShopService.cs` — Added `GetPagedPublishedItemsAsync` with domain model return type
- `EuphoriaInn.Domain/Services/ShopService.cs` — Added delegating method mapping entities to domain models
- `EuphoriaInn.Service/ViewModels/ShopViewModels/ShopIndexViewModel.cs` — Added pagination/search properties; `EquipmentItems`/`MagicItems` replaced with `[Obsolete]` stubs
- `EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs` — Added `SeedPublishedShopItemsAsync` helper + 7 new test methods

## Decisions Made

- Repository interface uses `int?` and `IList<int>?` instead of `ItemType?` and `IList<ItemRarity>?` because the Repository project does not reference EuphoriaInn.Domain (no circular dependency). The service layer converts enums to ints before calling the repository.
- `[Obsolete]` stubs kept for `EquipmentItems`/`MagicItems` in ShopIndexViewModel so Razor views compile at runtime without errors until Plan 02 updates `Index.cshtml`.
- Unit test mocks `IShopService` directly (not a real service instance) because the method doesn't need a concrete service to verify delegation — it tests the contract via NSubstitute.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed integration test QuestBoardContext namespace**
- **Found during:** Task 2 build verification
- **Issue:** Test referenced `EuphoriaInn.Repository.QuestBoardContext` but class is in `EuphoriaInn.Repository.Entities` namespace
- **Fix:** Updated fully-qualified reference to `EuphoriaInn.Repository.Entities.QuestBoardContext`
- **Files modified:** EuphoriaInn.IntegrationTests/Controllers/ShopControllerIntegrationTests.cs
- **Committed in:** 42d7350 (Task 2 commit)

**2. [Rule 1 - Architecture] Repository interface uses primitive types instead of domain enums**
- **Found during:** Task 2 — adding `GetPagedPublishedItemsAsync` to IShopRepository
- **Issue:** Repository project does not reference Domain project; using `ItemType?`/`IList<ItemRarity>?` would require adding a circular project reference
- **Fix:** Repository interface accepts `int?` and `IList<int>?`; service converts enums to ints before delegation
- **Files modified:** EuphoriaInn.Repository/Interfaces/IShopRepository.cs, EuphoriaInn.Domain/Services/ShopService.cs
- **Committed in:** 42d7350 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bugs/architecture)
**Impact on plan:** Both fixes required for correctness. No scope creep. Plan 02 can wire controller + view against the contract as-is.

## Known Stubs

- `ShopIndexViewModel.EquipmentItems` and `MagicItems` — marked `[Obsolete]`, return `Items` as-is. Plan 02 removes these after updating `Index.cshtml` to render `Model.Items` directly.
- 6 integration tests marked `[Fact(Skip = "Wave 2 — enabled in Plan 02...")]` — placeholders for pagination behaviour tests pending controller/view wiring.

## Issues Encountered

None beyond the auto-fixed deviations above.

## Next Phase Readiness

- `IShopService.GetPagedPublishedItemsAsync` contract is stable and DI-registered
- `ShopIndexViewModel` carries all fields Plan 02 needs (`CurrentPage`, `TotalPages`, etc.)
- Plan 02 can update `ShopController.Index` to call the paged method and render the pagination UI

---
*Phase: 09-shop-pagination-server-side-paging-to-fix-slow-load-from-large-item-sets*
*Completed: 2026-04-21*
