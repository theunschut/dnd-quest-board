# Phase 4: Security Hardening - Context

**Gathered:** 2026-04-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Five targeted security changes — all behaviour-preserving except for the newly enforced restrictions:
1. Enable account lockout on login (5 attempts, 15-min lock) with a friendly inline error message
2. EF Core migration sets `LockoutEnabled = 1` for all existing users in `AspNetUsers`
3. Raise minimum password length from 6 to 8 characters — silent enforcement, no forced reset
4. Remove `HasKey` checkbox from user-facing `Account/Edit.cshtml` and `EditProfileViewModel`; admin `EditUserViewModel` retains it
5. Remove `Password` property from `User` domain model, `Equals`, and `GetHashCode`; add explicit AutoMapper ignore on both directions
6. Add `.env` to `.gitignore` and run `git rm --cached .env` to stop tracking; `.env.example` remains the only tracked env file

No behavior changes to existing flows beyond the new restrictions (lockout, password minimum).

</domain>

<decisions>
## Implementation Decisions

### Lockout UX (SEC-01)

- **D-01:** Change `lockoutOnFailure: false` → `true` in `Controllers/Admin/AccountController.cs:26` (the main login for all users).
- **D-02:** Add `LockoutOptions` to `AddIdentity` block in `Program.cs`: `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`.
- **D-03:** Add an `IsLockedOut` branch to the login `POST` action that adds a specific `ModelState` error: **"Account locked due to too many failed attempts. Try again in 15 minutes."** — friendly, no security theatre for a private group app.

### EF Core Migration (SEC-02)

- **D-04:** Create an EF Core migration that updates `AspNetUsers` to set `LockoutEnabled = 1` for all existing rows. Raw SQL in `Up()`: `migrationBuilder.Sql("UPDATE AspNetUsers SET LockoutEnabled = 1")`.

### Password Minimum (SEC-03)

- **D-05:** Change `options.Password.RequiredLength = 6` → `8` in `Program.cs`.
- **D-06:** Silent enforcement — no forced reset, no UI notice. Existing users with short passwords can still log in; the new rule only applies on registration and password changes.

### HasKey Admin-Only (SEC-04)

- **D-07:** Remove `HasKey` property from `EditProfileViewModel` and its field from `Account/Edit.cshtml`. The admin path uses the separate `AdminViewModels/EditUserViewModel.cs` which already contains `HasKey` — leave that untouched.

### Password Property Removal (SEC-05)

- **D-08:** Remove `Password` property, its `[Required]`/`[StringLength]` attributes, and its references in `Equals` and `GetHashCode` from `EuphoriaInn.Domain/Models/User.cs`.
- **D-09:** Add explicit `.ForMember(dest => dest.Password, opt => opt.Ignore())` (or equivalent) on both mapping directions in `EntityProfile.cs` to prevent accidental re-mapping.

### .env Cleanup (SEC-06)

- **D-10:** Add `.env` to `.gitignore` (not just a comment — an actual entry). The existing `.env.example` with placeholder values stays tracked.
- **D-11:** Run `git rm --cached .env` as part of the phase to stop tracking the file going forward. The file remains on disk locally. Note: `.env` content remains in git history — this is accepted; no history rewrite.

### Claude's Discretion

- Exact placement of `LockoutOptions` within the `AddIdentity` options block.
- Whether to also update the `[MinLength]` annotation in `RegisterViewModel`'s Password field to match Identity's 8-char rule (safe to add for client-side consistency).
- Order of the six changes within the plan.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Files Being Changed

- `EuphoriaInn.Service/Program.cs` — Identity options block (lines ~33-52): add LockoutOptions, change RequiredLength
- `EuphoriaInn.Service/Controllers/Admin/AccountController.cs:26` — change `lockoutOnFailure: false` → `true`; add `IsLockedOut` branch
- `EuphoriaInn.Service/Views/Account/Edit.cshtml` — remove `HasKey` field (lines ~38-44)
- `EuphoriaInn.Service/ViewModels/AccountViewModels/EditProfileViewModel.cs` — remove `HasKey` property
- `EuphoriaInn.Domain/Models/User.cs` — remove `Password` property + Equals/GetHashCode references
- `EuphoriaInn.Domain/Automapper/EntityProfile.cs` — add explicit Password ignore on both mapping directions
- `.gitignore` — add `.env` entry
- `EuphoriaInn.Repository/Migrations/` — new migration for LockoutEnabled + existing user backfill

### Requirements

- `.planning/REQUIREMENTS.md` §Security — SEC-01 through SEC-06

### Codebase Map

- `.planning/codebase/CONCERNS.md` — original concern catalogue for context on why these were identified

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- `LockoutOptions` configuration: add inside the existing `AddIdentity` lambda in `Program.cs` — no new registration needed.
- `AdminViewModels/EditUserViewModel.cs` — already has `HasKey`; leave untouched. Only `AccountViewModels/EditProfileViewModel.cs` needs the property removed.
- `.env.example` already exists — no need to create it.

### Established Patterns

- EF Core raw SQL in migrations: `migrationBuilder.Sql(...)` pattern — used in prior migrations in `EuphoriaInn.Repository/Migrations/`.
- AutoMapper explicit ignore: `opt.Ignore()` — see `EntityProfile.cs` for existing examples of field exclusion.
- `ModelState.AddModelError(string.Empty, "message")` — existing pattern in `AccountController.Login` POST for error display.

### Integration Points

- Login flow: `AccountController.Login (POST)` → `userService.PasswordSignInAsync` → `identityService.PasswordSignInAsync` → `signInManager.PasswordSignInAsync`. The `lockoutOnFailure` parameter flows through all three layers unchanged — only the call site in the controller needs updating.
- `User` domain model `Password` removal: verify no controller or service reads `user.Password` after this change. The `Password` property in `User` is a legacy carry-over; Identity manages password hashing in `UserEntity`.

</code_context>

<specifics>
## Specific Ideas

- Lockout message text: **"Account locked due to too many failed attempts. Try again in 15 minutes."**
- `.gitignore` entry: just `.env` on its own line (not `*.env` — don't accidentally ignore `.env.example`).
- `git rm --cached .env` should be run as part of this phase's commit sequence; note in plan that history is not rewritten.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 04-security-hardening*
*Context gathered: 2026-04-20*
