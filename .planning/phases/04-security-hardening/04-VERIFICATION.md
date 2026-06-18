---
phase: 04-security-hardening
verified: 2026-04-20T15:00:00Z
status: passed
score: 12/12 must-haves verified
gaps: []
human_verification:
  - test: "Trigger account lockout after 5 failed logins"
    expected: "Login form shows 'Account locked due to too many failed attempts. Try again in 15 minutes.' after the 5th bad password; account is inaccessible for 15 minutes"
    why_human: "Requires live browser session and a valid user account — cannot simulate SignInManager lockout state programmatically without a running server"
  - test: "Attempt to set HasKey via Account/Edit"
    expected: "HasKey field is absent from the rendered form; submitting the form does not change the HasKey value on the account"
    why_human: "Requires browser rendering of the Razor view to confirm the checkbox is absent and that form POST cannot smuggle the value"
---

# Phase 4: Security Hardening Verification Report

**Phase Goal:** Failed login attempts are rate-limited with lockout, the minimum password length meets the 8-character standard, HasKey is admin-only, the Password property is removed from the domain model, and .env is not tracked by git
**Verified:** 2026-04-20T15:00:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #  | Truth                                                                                                           | Status     | Evidence                                                                                            |
|----|----------------------------------------------------------------------------------------------------------------|------------|-----------------------------------------------------------------------------------------------------|
| 1  | Program.cs configures Identity with MaxFailedAccessAttempts = 5 and DefaultLockoutTimeSpan = 15 minutes       | VERIFIED   | Lines 46-47 of Program.cs contain exact strings; AllowedForNewUsers = true also present (line 48)   |
| 2  | Program.cs sets Identity RequiredLength to 8                                                                   | VERIFIED   | Line 40: `options.Password.RequiredLength = 8`                                                      |
| 3  | AccountController.Login POST passes lockoutOnFailure: true to PasswordSignInAsync                              | VERIFIED   | Line 26 of AccountController.cs: `lockoutOnFailure: true`                                           |
| 4  | AccountController.Login POST shows a lockout-specific error when result.IsLockedOut is true                    | VERIFIED   | Lines 33-35: `if (result.IsLockedOut)` with friendly message                                        |
| 5  | RegisterViewModel Password MinimumLength is 8                                                                  | VERIFIED   | Line 18: `MinimumLength = 8` in StringLength attribute                                              |
| 6  | A new EF Core migration exists that sets LockoutEnabled = 1 for all existing AspNetUsers rows                  | VERIFIED   | `20260420142117_EnableLockoutForExistingUsers.cs` with `migrationBuilder.Sql("UPDATE AspNetUsers SET LockoutEnabled = 1")` in Up() |
| 7  | User-facing Account/Edit form no longer exposes HasKey                                                         | VERIFIED   | EditProfileViewModel.cs has no HasKey; Edit.cshtml has no HasKey or "building key"; AccountController Edit GET/POST do not reference HasKey |
| 8  | HasKey is retained in admin EditUserViewModel                                                                  | VERIFIED   | `EuphoriaInn.Service/ViewModels/AdminViewModels/EditUserViewModel.cs` line 21 retains `HasKey`; admin EditUser.cshtml retains the checkbox |
| 9  | User domain model has no Password property and Equals/GetHashCode do not reference Password                    | VERIFIED   | User.cs has no Password property; Equals compares Id, Name, Email, HasKey only; GetHashCode uses those same four fields |
| 10 | EntityProfile is compatible with the Password-less User model                                                  | VERIFIED   | EntityProfile has no `dest.Password` reference; only `dest.PasswordHash` ignore (Identity-managed); no compile errors |
| 11 | .gitignore contains an exact `.env` entry (not *.env, not commented)                                          | VERIFIED   | `grep -Fx ".env" .gitignore` exits 0 (line 718); `grep -Fx "*.env" .gitignore` exits non-zero      |
| 12 | .env is not tracked by git; .env.example remains tracked                                                      | VERIFIED   | `git ls-files -- .env` returns empty; `git ls-files -- .env.example` returns `.env.example`          |

**Score:** 12/12 truths verified

### Required Artifacts

| Artifact                                                                            | Expected                                  | Status   | Details                                                                 |
|-------------------------------------------------------------------------------------|-------------------------------------------|----------|-------------------------------------------------------------------------|
| `EuphoriaInn.Service/Program.cs`                                                    | Identity lockout + 8-char password config | VERIFIED | Contains MaxFailedAccessAttempts = 5, DefaultLockoutTimeSpan 15 min, RequiredLength = 8 |
| `EuphoriaInn.Service/Controllers/Admin/AccountController.cs`                        | Login with lockout handling               | VERIFIED | lockoutOnFailure: true on line 26; IsLockedOut branch on lines 33-35   |
| `EuphoriaInn.Service/ViewModels/AccountViewModels/RegisterViewModel.cs`             | 8-char client-side minimum                | VERIFIED | MinimumLength = 8 on line 18                                            |
| `EuphoriaInn.Service/ViewModels/AccountViewModels/EditProfileViewModel.cs`          | User-facing edit DTO without HasKey       | VERIFIED | No HasKey property present                                              |
| `EuphoriaInn.Service/Views/Account/Edit.cshtml`                                     | User-facing edit form without HasKey      | VERIFIED | No HasKey or building-key references present                            |
| `EuphoriaInn.Domain/Models/User.cs`                                                 | Password-free User domain model           | VERIFIED | No Password property; Equals/GetHashCode updated                        |
| `EuphoriaInn.Repository/Automapper/EntityProfile.cs`                                | UserEntity <-> User mapping without Password | VERIFIED | No dest.Password; PasswordHash ignore retained for Identity           |
| `EuphoriaInn.Repository/Migrations/20260420142117_EnableLockoutForExistingUsers.cs` | Migration backfilling LockoutEnabled      | VERIFIED | Up() has UPDATE SET 1; Down() has UPDATE SET 0; Designer.cs also present |
| `.gitignore`                                                                        | Ignore rule for .env                      | VERIFIED | Exact `.env` line present (line 718); no *.env wildcard                 |

### Key Link Verification

| From                                     | To                                      | Via                       | Status   | Details                                                                           |
|------------------------------------------|-----------------------------------------|---------------------------|----------|-----------------------------------------------------------------------------------|
| AccountController.Login POST             | userService.PasswordSignInAsync         | lockoutOnFailure parameter | VERIFIED | `lockoutOnFailure: true` confirmed on call site; forwarded through UserService and IdentityService to SignInManager |
| Program.cs AddIdentity options           | Identity lockout enforcement            | options.Lockout            | VERIFIED | options.Lockout.MaxFailedAccessAttempts confirmed in Program.cs                   |
| EditProfileViewModel                     | Account/Edit.cshtml                     | model binding (absence)    | VERIFIED | asp-for="HasKey" is ABSENT from Edit.cshtml as required                           |
| EntityProfile UserEntity -> User mapping | User (Password-free)                    | AutoMapper (absence)       | VERIFIED | dest.Password is ABSENT; ForMember for PasswordHash ignore retained               |
| Migration Up()                           | AspNetUsers table                       | migrationBuilder.Sql       | VERIFIED | `UPDATE AspNetUsers SET LockoutEnabled = 1` present in Up()                       |
| git index                                | .env                                    | tracked status             | VERIFIED | `git ls-files -- .env` returns empty; .gitignore has exact `.env` rule            |

### Data-Flow Trace (Level 4)

Not applicable for this phase. All artifacts are configuration files, view models, domain models, and a database migration — none render dynamic runtime data that requires a data-flow trace.

### Behavioral Spot-Checks

| Behavior                                               | Command                                                                               | Result       | Status |
|--------------------------------------------------------|---------------------------------------------------------------------------------------|--------------|--------|
| Solution builds with zero errors                       | `dotnet build EuphoriaInn.slnx --nologo`                                              | 0 errors     | PASS   |
| All unit tests pass (30 tests)                         | `dotnet test EuphoriaInn.slnx --nologo --no-build`                                    | Passed 30/30 | PASS   |
| All integration tests pass (53 tests)                  | `dotnet test EuphoriaInn.slnx --nologo --no-build`                                    | Passed 53/53 | PASS   |
| Migration file lists EnableLockoutForExistingUsers     | `ls EuphoriaInn.Repository/Migrations/*EnableLockout*`                                | 2 files found| PASS   |
| .gitignore has exact .env entry                        | `grep -Fx ".env" .gitignore`                                                          | Matched      | PASS   |
| .env not tracked                                       | `git ls-files -- .env`                                                                | Empty        | PASS   |
| .env.example tracked                                   | `git ls-files -- .env.example`                                                        | .env.example | PASS   |
| No user.Password / dest.Password references in C# code| `grep -rn "user\.Password\|dest\.Password" EuphoriaInn.{Domain,Service,Repository}`   | 0 matches    | PASS   |

### Requirements Coverage

| Requirement | Source Plan | Description                                                                                 | Status    | Evidence                                                                                     |
|-------------|-------------|---------------------------------------------------------------------------------------------|-----------|----------------------------------------------------------------------------------------------|
| SEC-01      | 04-01       | lockoutOnFailure: true in PasswordSignInAsync; LockoutOptions configured (5 attempts, 15 min) | SATISFIED | AccountController line 26; Program.cs lines 46-48; full chain through IdentityService confirmed |
| SEC-02      | 04-03       | EF Core migration sets LockoutEnabled = 1 for all existing users in AspNetUsers              | SATISFIED | Migration 20260420142117_EnableLockoutForExistingUsers.cs with correct SQL in Up()/Down()    |
| SEC-03      | 04-01       | Minimum password length is 8 characters (up from 6)                                         | SATISFIED | Program.cs RequiredLength = 8; RegisterViewModel MinimumLength = 8                           |
| SEC-04      | 04-02       | HasKey checkbox removed from Account/Edit.cshtml and EditProfileViewModel; admin-only via EditUser | SATISFIED | No HasKey in EditProfileViewModel.cs or Edit.cshtml; AdminViewModels/EditUserViewModel.cs retains it |
| SEC-05      | 04-02       | Password property removed from User domain model, Equals, and GetHashCode; AutoMapper explicit | SATISFIED | User.cs has no Password; Equals/GetHashCode updated; EntityProfile has no dest.Password      |
| SEC-06      | 04-04       | .env added to .gitignore; .env.example is the only tracked env file                         | SATISFIED | .gitignore line 718 has exact `.env`; git index confirms .env untracked, .env.example tracked |

No orphaned requirements detected. All six SEC-0x IDs declared in plan frontmatter map to a plan, have implementation evidence, and appear in REQUIREMENTS.md.

### Anti-Patterns Found

None detected. Scanned modified files for TODO/FIXME/placeholder comments, empty returns, and hardcoded stubs. No issues found.

### Human Verification Required

#### 1. Account Lockout Flow

**Test:** Using a browser, attempt to log in with a valid email and wrong password 5 times in succession.
**Expected:** On the 5th failure the login form displays "Account locked due to too many failed attempts. Try again in 15 minutes." Attempting to log in with the correct password during the 15-minute window must also show the locked message (not "Invalid login attempt.").
**Why human:** Requires a live ASP.NET Core session backed by SQL Server with real Identity middleware — the lockout counter and the SignInManager's locked-out branch cannot be exercised by static code inspection.

#### 2. HasKey Absence on User-Facing Edit Form

**Test:** Log in as a non-admin user, navigate to /Account/Edit, inspect the rendered HTML.
**Expected:** No checkbox or label referencing "HasKey" or "Has Building Key" or "building key" is present. Submitting the form must not alter the user's HasKey value in the database.
**Why human:** Razor view rendering requires a running server; confirming that a hidden-field POST bypass does not work requires live form inspection.

### Gaps Summary

No gaps. All 12 must-have truths are verified. All 6 requirements (SEC-01 through SEC-06) are satisfied with implementation evidence. Build passes with 0 errors and 0 warnings. All 83 tests (30 unit + 53 integration) pass.

---

_Verified: 2026-04-20T15:00:00Z_
_Verifier: Claude (gsd-verifier)_
