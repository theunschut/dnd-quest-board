---
phase: 02-email-service-consolidation
plan: "03"
subsystem: shop
tags: [shop, controller-refactor, service-extraction, dto, unit-tests, CTRL-04]
dependency_graph:
  requires: []
  provides: [TransactionWithRemaining-dto, GetUserTransactionsWithRemainingAsync, CalculateRemainingQuantity-helper]
  affects: [IShopService, ShopService, ShopController, EuphoriaInn.UnitTests]
tech_stack:
  added: []
  patterns: [dto-record, private-static-helper, controller-delegation-to-service]
key_files:
  created:
    - EuphoriaInn.Domain/Models/Shop/TransactionWithRemaining.cs
    - EuphoriaInn.Domain/Properties/AssemblyInfo.cs
    - EuphoriaInn.UnitTests/Services/ShopServiceTests.cs
  modified:
    - EuphoriaInn.Domain/Interfaces/IShopService.cs
    - EuphoriaInn.Domain/Services/ShopService.cs
    - EuphoriaInn.Service/Controllers/Shop/ShopController.cs
decisions:
  - "CalculateRemainingQuantity takes UserTransactionEntity (not domain model) because worktree architecture passes entities through the service layer; consistent with existing ReturnOrSellItemAsync pattern"
  - "InternalsVisibleTo added via AssemblyInfo.cs (not csproj attribute) to avoid MSBuild cache locking issues in parallel agent environment"
metrics:
  duration_minutes: 25
  completed_date: "2026-04-17"
  tasks_completed: 2
  files_changed: 6
---

# Phase 02 Plan 03: Shop Controller Remaining-Quantity Extraction Summary

**One-liner:** Extracted remaining-quantity calculation from `ShopController.Index` into `ShopService.GetUserTransactionsWithRemainingAsync` via a shared `CalculateRemainingQuantity` private static helper, eliminating controller business logic (CTRL-04).

## What Was Built

### TransactionWithRemaining DTO

`EuphoriaInn.Domain/Models/Shop/TransactionWithRemaining.cs`:

```csharp
public record TransactionWithRemaining(UserTransaction Transaction, int RemainingQuantity);
```

A simple value-object that pairs a mapped domain `UserTransaction` with the computed `RemainingQuantity` integer. Lives in `Domain.Models.Shop` to keep the persisted `UserTransaction` model clean.

### GetUserTransactionsWithRemainingAsync Signature

Added to `IShopService`:

```csharp
Task<IReadOnlyList<TransactionWithRemaining>> GetUserTransactionsWithRemainingAsync(int userId, CancellationToken token = default);
```

Implemented in `ShopService`: fetches all user transaction entities once, filters to `Purchase` type, maps each entity to the domain model, and enriches with the computed remaining quantity using the shared helper.

### CalculateRemainingQuantity Shared Helper

```csharp
private static int CalculateRemainingQuantity(UserTransactionEntity purchase, IList<UserTransactionEntity> allTransactions)
{
    var returned = allTransactions
        .Where(t => t.TransactionType == (int)TransactionType.Sell &&
                    t.OriginalTransactionId == purchase.Id)
        .Sum(t => t.Quantity);
    return purchase.Quantity - returned;
}
```

The helper is called from both `GetUserTransactionsWithRemainingAsync` and `ReturnOrSellItemAsync` — confirmed 3 total occurrences in ShopService.cs (1 definition + 2 call sites).

### Slimmed ShopController.Index

The `Index` action body was reduced from ~35 lines to ~20 lines. The `existingReturns` calculation and `foreach` loop over mapped transactions were removed. The controller now:

1. Fetches published items
2. Calls `shopService.GetUserTransactionsWithRemainingAsync(...)` once
3. Projects each `TransactionWithRemaining` to a `UserTransactionViewModel` via AutoMapper + manual `RemainingQuantity` assignment

## Commits

- `913d23a`: `feat(02-03): add GetUserTransactionsWithRemainingAsync with shared CalculateRemainingQuantity helper`
- `a84aaf0`: `feat(02-03): slim ShopController.Index — consume GetUserTransactionsWithRemainingAsync`

## Verification

- `existingReturns` in ShopController.cs: 0 matches (CTRL-04 satisfied)
- `TransactionType.Sell` in ShopController.cs: 0 matches
- `CalculateRemainingQuantity(` in ShopService.cs: 3 matches (definition + 2 call sites, >= 2 required)
- All 20 unit tests pass (`dotnet test EuphoriaInn.UnitTests`)
- Full solution build: succeeded

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Architecture] Adapted CalculateRemainingQuantity to work with UserTransactionEntity**

- **Found during:** Task 1 implementation
- **Issue:** The worktree's architecture (pre-Phase 1) has `ShopService` working with `UserTransactionEntity` objects directly (casting `TransactionType` to int), unlike the main branch which uses domain models. The plan's helper signature used domain `UserTransaction`.
- **Fix:** Changed helper signature to `UserTransactionEntity` to match the actual types flowing through this service layer. The `GetUserTransactionsWithRemainingAsync` method maps entities to domain models via `Mapper.Map<UserTransaction>` before populating the DTO.
- **Files modified:** `EuphoriaInn.Domain/Services/ShopService.cs`

**2. [Rule 3 - Blocking] Added InternalsVisibleTo via AssemblyInfo.cs instead of csproj**

- **Found during:** Task 1 (build contention)
- **Issue:** Using `<AssemblyAttribute>` in the csproj caused persistent MSBuild cache locking errors in the parallel agent build environment.
- **Fix:** Added `EuphoriaInn.Domain/Properties/AssemblyInfo.cs` with `[assembly: InternalsVisibleTo("EuphoriaInn.UnitTests")]` attribute instead.
- **Files modified:** `EuphoriaInn.Domain/Properties/AssemblyInfo.cs`

## Known Stubs

None — all functionality is wired through to the view.

## Self-Check: PASSED
