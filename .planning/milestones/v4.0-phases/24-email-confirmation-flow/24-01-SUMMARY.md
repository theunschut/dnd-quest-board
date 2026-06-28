---
phase: 24-email-confirmation-flow
plan: "01"
subsystem: domain-model
tags: [email-confirmation, domain, extensions, unit-tests]
dependency_graph:
  requires: []
  provides:
    - User.EmailConfirmed (domain model property)
    - UserExtensions.WhereEmailConfirmed (extension method)
    - UserManagementViewModel.EmailConfirmed (view model property)
    - UserExtensionsTests (unit test class)
    - EmailConfirmationJobGuardTests (Wave 0 test scaffold)
  affects:
    - EuphoriaInn.Domain/Models/User.cs
    - EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs
    - EuphoriaInn.Domain/Extensions/UserExtensions.cs
    - EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs
    - EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs
tech_stack:
  added: []
  patterns:
    - IEnumerable<T> extension method for filtering confirmed users
    - Hand-coded Equals/GetHashCode updated to include new field
key_files:
  created:
    - EuphoriaInn.Domain/Extensions/UserExtensions.cs
    - EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs
    - EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs
  modified:
    - EuphoriaInn.Domain/Models/User.cs
    - EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs
decisions:
  - AutoMapper convention mapping picks up EmailConfirmed from IdentityUser<int> — no ForMember needed
  - UserManagementViewModel.EmailConfirmed is a flat bool (not nested User.EmailConfirmed) per UI-SPEC contract
  - EmailConfirmationJobGuardTests uses a placeholder Fact so the class is discoverable before plan 04 wires real assertions
metrics:
  duration: 8m
  completed: 2026-06-26
status: complete
---

# Phase 24 Plan 01: Domain Foundation Summary

**One-liner:** `bool EmailConfirmed` added to `User` domain model and `UserManagementViewModel`, with `WhereEmailConfirmed()` extension and Wave 0 test stubs — domain layer ready for all downstream email confirmation plans.

## What Was Built

### Task 1: EmailConfirmed on User and UserManagementViewModel

- Added `public bool EmailConfirmed { get; set; }` to `User` immediately after `HasKey`
- Updated `User.Equals` to include `&& EmailConfirmed == user.EmailConfirmed`
- Updated `User.GetHashCode` to `HashCode.Combine(Id, Name, Email, HasKey, EmailConfirmed)`
- Added `public bool EmailConfirmed { get; set; }` to `UserManagementViewModel` after `IsPlayer`
- AutoMapper convention maps `UserEntity.EmailConfirmed` (inherited from `IdentityUser<int>`) to `User.EmailConfirmed` — no migration needed

### Task 2: WhereEmailConfirmed Extension and Wave 0 Test Stubs

- Created `EuphoriaInn.Domain/Extensions/UserExtensions.cs` with static `WhereEmailConfirmed(this IEnumerable<User>)` returning `users.Where(u => u.EmailConfirmed)`
- Created `EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs` with 3 [Fact] tests:
  - Mixed list returns only confirmed users (HaveCount(2), OnlyContain)
  - All-unconfirmed list returns empty
  - Empty input returns empty without exception
- Created `EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs` with placeholder Fact (plan 04 replaces body)

## Verification

- `dotnet build EuphoriaInn.Domain EuphoriaInn.Service` — succeeded (warnings are pre-existing NU1510, unrelated to this plan)
- `dotnet test --filter "FullyQualifiedName~UserExtensionsTests|FullyQualifiedName~EmailConfirmationJobGuard"` — 4/4 passed

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

- `EmailConfirmationJobGuardTests.Placeholder_UntilPlan04WiresGuard` — intentional scaffold; plan 04 replaces the body with QuestService guard assertions per REQ-24-04.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, or schema changes introduced. The `WhereEmailConfirmed()` filter is a pure in-memory LINQ predicate; it mitigates T-24-04 (email sent to unverified addresses) as specified in the threat register.

## Self-Check

Files exist:
- EuphoriaInn.Domain/Extensions/UserExtensions.cs: FOUND
- EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs: FOUND
- EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs: FOUND

Commits:
- a4c3dbf: feat(24-01): add EmailConfirmed to User domain model and UserManagementViewModel
- 3de6376: feat(24-01): create WhereEmailConfirmed extension and Wave 0 test stubs

## Self-Check: PASSED
