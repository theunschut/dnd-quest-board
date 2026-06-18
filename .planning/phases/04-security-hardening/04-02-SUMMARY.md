---
phase: 04-security-hardening
plan: 02
subsystem: auth
tags: [aspnet-identity, automapper, domain-model, security]

# Dependency graph
requires:
  - phase: 04-security-hardening-01
    provides: Lockout and password-length enforcement already applied to Identity config
provides:
  - HasKey field removed from user-facing Account/Edit form (admin-only via EditUserViewModel)
  - Password property removed from User domain model, Equals, GetHashCode
  - EntityProfile UserEntity->User reverse map no longer references non-existent Password member
affects: [any phase touching User domain model, AutoMapper entity profiles, or account edit flows]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "HasKey stays in EditUserViewModel (admin); removed from EditProfileViewModel (user-facing)"
    - "User domain model carries only fields needed for domain logic — Identity manages password hashing"

key-files:
  created: []
  modified:
    - EuphoriaInn.Service/ViewModels/AccountViewModels/EditProfileViewModel.cs
    - EuphoriaInn.Service/Views/Account/Edit.cshtml
    - EuphoriaInn.Service/Controllers/Admin/AccountController.cs
    - EuphoriaInn.Domain/Models/User.cs
    - EuphoriaInn.Domain/Automapper/EntityProfile.cs

key-decisions:
  - "SEC-04: HasKey stays in Admin EditUserViewModel only; removed from user-facing EditProfileViewModel and Account/Edit view and controller actions"
  - "SEC-05: Password removed from User domain model; UserEntity continues to hold PasswordHash managed by ASP.NET Core Identity"
  - "EntityProfile reverse map CreateMap<UserEntity, User>() simplified to no ForMember — Password member no longer exists on User"

patterns-established:
  - "Domain model User: carries only identity-agnostic fields (Id, Name, Email, HasKey, Quests, Signups)"
  - "Password lifecycle: entirely owned by ASP.NET Core Identity (UserEntity.PasswordHash); never surfaced in domain layer"

requirements-completed: [SEC-04, SEC-05]

# Metrics
duration: 12min
completed: 2026-04-20
---

# Phase 4 Plan 02: Security-Hardening — HasKey Admin-Only + Password Domain Removal Summary

**HasKey locked to admin panel only and Password property purged from User domain model; AutoMapper and build stay green**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-04-20T16:20:00Z
- **Completed:** 2026-04-20T16:32:00Z
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments

- Removed `HasKey` from `EditProfileViewModel`, `Account/Edit.cshtml`, and both `AccountController.Edit` GET/POST actions; admin `EditUserViewModel` retains it unchanged
- Removed `Password` property from `User` domain model and updated `Equals`/`GetHashCode` to exclude it
- Simplified `EntityProfile` `CreateMap<UserEntity, User>()` — the `.ForMember(dest => dest.Password, ...)` override removed since the member no longer exists, eliminating a potential compile error; `PasswordHash` ignore on the forward map retained

## Task Commits

1. **Task 1: Remove HasKey from user-facing Edit (SEC-04)** - `ddebf1e` (feat)
2. **Task 2: Remove Password from User domain model + AutoMapper cleanup (SEC-05)** - `353f892` (feat)

## Files Created/Modified

- `EuphoriaInn.Service/ViewModels/AccountViewModels/EditProfileViewModel.cs` - Removed HasKey property
- `EuphoriaInn.Service/Views/Account/Edit.cshtml` - Removed HasKey form-check block
- `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` - Removed HasKey from Edit GET initializer and Edit POST assignment
- `EuphoriaInn.Domain/Models/User.cs` - Removed Password property, Equals Password term, GetHashCode Password arg
- `EuphoriaInn.Domain/Automapper/EntityProfile.cs` - Replaced Password-ignoring reverse map with bare `CreateMap<UserEntity, User>()`

## Decisions Made

- SEC-04: Only the user-facing `EditProfileViewModel` and `Account/Edit.cshtml` are touched; admin `EditUserViewModel` is left fully intact per D-07
- SEC-05: No replacement for Password on User — Identity's `UserEntity.PasswordHash` is the authoritative store; domain layer has no need to carry it
- EntityProfile simplification is correct: removing `.ForMember(dest => dest.Password, opt => opt.Ignore())` is required once `Password` is gone (leaving it would be a compile error)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Worktree path mismatch: Edit tool initially wrote to the main repo path (`/c/Repos/quest-board/`) rather than the worktree path (`/c/Repos/quest-board/.claude/worktrees/agent-aa61d80a/`). Reverted those accidental main-repo changes with `git checkout --`, then re-applied all edits to the correct worktree paths. No functional impact.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- SEC-04 and SEC-05 complete; user-facing account edit is clean
- Phase 04-03 (`.env` gitignore) is independent and can proceed
- Admin panel still controls HasKey correctly via `EditUserViewModel`

---
*Phase: 04-security-hardening*
*Completed: 2026-04-20*
