# Domain Pitfalls

**Domain:** ASP.NET Core 8 MVC — Clean Architecture Refactor (Milestone 2)
**Researched:** 2026-04-15

---

## Critical Pitfalls

Mistakes that cause rewrites or silent regressions.

---

### Pitfall 1: AutoMapper Profile Registration Breaks When Both Profiles Scan the Same Assembly

**What goes wrong:**
`EntityProfile` lives in `EuphoriaInn.Domain` and `ViewModelProfile` lives in `EuphoriaInn.Service`. Both are currently registered explicitly in `Program.cs`:

```csharp
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<ViewModelProfile>();
    config.AddProfile<EntityProfile>();
});
```

This explicit form is safe. The danger arrives if any refactor step switches to assembly-scanning (`AddAutoMapper(typeof(Program).Assembly)` or `AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())`). When scanning finds `EntityProfile` through two paths — directly and transitively via the Domain assembly — AutoMapper throws `DuplicateTypeMapConfigurationException` at startup. The app does not start.

**Why it happens:**
`AddAutoMapper` with assembly scanning is not idempotent. The same `Profile` type registered twice produces duplicate `CreateMap` entries, which AutoMapper treats as a configuration error.

**Consequences:**
Application fails to start. If this happens inside a Docker build pipeline, the container exits immediately and the rollback window is narrow.

**Prevention:**
- Keep explicit `AddProfile<T>()` registration for both profiles. Do not switch to assembly scanning.
- If assembly scanning is ever needed, use `typeof(EntityProfile).Assembly` and `typeof(ViewModelProfile).Assembly` as distinct, non-overlapping targets.
- Run `mapper.ConfigurationProvider.AssertConfigurationIsValid()` in a unit test so profile breakage is caught before deployment.

**Detection:**
Application throws `AutoMapperConfigurationException` or `DuplicateTypeMapConfigurationException` on startup. No HTTP traffic is served.

**Phase:** Architecture Refactor (Phase 1 / early phases)

---

### Pitfall 2: Moving Email Dispatch into a Service That Receives Stale Pre-Finalize Quest State

**What goes wrong:**
CONCERNS.md item 28 identifies this already. The current `QuestController.Finalize` action:
1. Fetches `quest` via `GetQuestWithDetailsAsync(id)`
2. Calls `questService.FinalizeQuestAsync(...)` which mutates the DB
3. Then loops over the pre-fetched `quest.PlayerSignups` to build the email list

The pre-fetched `quest` object was loaded before `FinalizeQuestAsync` ran. After `FinalizeQuestAsync` writes to the DB, the in-memory `quest` object is stale. The spectator condition `ps.Role == SignupRole.Spectator` sends to all spectators regardless of `selectedPlayerIds`, producing unintended emails.

If email dispatch is moved into `FinalizeQuestAsync` (the correct architecture), the wrong fix is to pass `quest.PlayerSignups` (the pre-save object) into the service. The right fix is to build the email recipient list from the post-save entity state returned by `FinalizeQuestAsync`.

**Why it happens:**
EF Core change-tracking means the in-memory object graph is the same instance that was mutated, but signups loaded with `AsNoTracking` (or a separate context fetch) give a snapshot frozen at query time. Moving logic to a service does not automatically fix the timing — it just moves the bug.

**Consequences:**
- Spectators who should not receive email get one
- Players selected after the stale snapshot was taken do not receive email
- Silent failure: no exception is thrown, the wrong emails go out

**Prevention:**
- `FinalizeQuestAsync` must return the list of selected signups (including emails) that the service determined should be notified, computed from the post-save entity state.
- Alternatively, have the service perform the email dispatch itself and re-fetch the quest after `SaveChangesAsync`.
- Never pass a domain model fetched *before* a mutating service call back *into* a post-mutation email loop.

**Detection:**
- Write an integration test: finalize a quest, assert that only selected players plus spectators (not all players) received emails.
- Review every `await emailService.Send...` call that references a variable declared before `await service.Mutate...`.

**Phase:** Architecture Refactor — moving email/finalize logic to service layer

---

### Pitfall 3: Fixing the Domain → Repository Dependency Direction Breaks `BaseService<TModel, TEntity>`

**What goes wrong:**
`BaseService<TModel, TEntity>` is declared in `EuphoriaInn.Domain` with a type parameter `TEntity` that must satisfy `IBaseRepository<TEntity>`. `IBaseRepository<TEntity>` is in `EuphoriaInn.Repository`. This means `BaseService` in Domain holds a compile-time reference to a Repository type.

The fix — moving interfaces to Domain and having Repository implement them — is correct in principle. The pitfall is doing it in the wrong order:

1. If `IBaseRepository<T>` is moved to Domain before `BaseService` is updated, the Domain project temporarily has no reference to Repository, but `BaseService` still imports `EuphoriaInn.Repository.Interfaces`. The build breaks at step 1.
2. If the Repository project reference is removed from Domain before all usages are cleaned up (including `EntityProfile.cs` which imports `EuphoriaInn.Repository.Entities`), the build breaks silently with cascading errors across every service.

**Why it happens:**
`EntityProfile` is the largest hidden dependency. It imports `EuphoriaInn.Repository.Entities` directly for every `CreateMap<Model, *Entity>` pair. Moving profiles or breaking the Domain → Repository link without also moving `EntityProfile` out of Domain (or changing its approach) leaves the project uncompilable.

**Consequences:**
Build fails. If discovered mid-refactor with partial changes, it requires careful rollback or simultaneous multi-file changes.

**Prevention:**
- Define the full target state before starting: which interfaces live where, which project references which.
- Move `IBaseRepository<T>` to Domain first; update `BaseService` simultaneously in the same commit.
- Keep `EntityProfile` in Domain if Domain retains a reference to Repository entities (which is acceptable as a migration step). Document this as a known remaining dependency, not a completed clean break.
- Use `dotnet build` at each step before proceeding.

**Detection:**
Compiler errors referencing `EuphoriaInn.Repository` namespace from within `EuphoriaInn.Domain` after the reference is removed.

**Phase:** Architecture Refactor — layer dependency direction fix

---

### Pitfall 4: Enabling Account Lockout Without Setting `LockoutEnabled = true` on Existing Users

**What goes wrong:**
ASP.NET Core Identity's `LockoutEnabled` column on `AspNetUsers` defaults to `false` for users created before lockout was configured. Changing `lockoutOnFailure: false` to `lockoutOnFailure: true` in `PasswordSignInAsync` and adding `options.Lockout` in `Program.cs` has **no effect on existing users** whose `LockoutEnabled` column is `false`. Brute-force protection is silently absent for every user who already registered.

**Why it happens:**
`LockoutEnabled` per-user is a database flag that means "this account participates in lockout." Setting the Identity options only affects **new** user creation (when `options.Lockout.AllowedForNewUsers = true`, new users get `LockoutEnabled = true`). Existing rows are untouched.

**Consequences:**
The security fix appears done (code change visible in PR, tests pass) but pre-existing accounts have no brute-force protection. An attacker targeting a known email address can try unlimited passwords.

**Prevention:**
Run a migration that sets `LockoutEnabled = 1` for all existing users. This can be done as a raw SQL migration in EF Core:

```csharp
// Inside a new EF Core migration's Up() method:
migrationBuilder.Sql("UPDATE AspNetUsers SET LockoutEnabled = 1");
```

Do this in the same migration or EF migration batch as enabling lockout in code.

**Detection:**
Query `AspNetUsers` after deployment: `SELECT COUNT(*) FROM AspNetUsers WHERE LockoutEnabled = 0`. Any non-zero result means the fix is incomplete for those accounts.

**Phase:** Security fixes phase

---

### Pitfall 5: Moving `IConfiguration` Access in `EmailService` to `IOptions<T>` Breaks the Domain Layer's Dependency Graph

**What goes wrong:**
`EmailService` currently injects `IConfiguration` directly and reads `EmailSettings` inside each send method. The correct fix is `IOptions<EmailSettings>`. However, `EmailService` lives in `EuphoriaInn.Domain`. `Microsoft.Extensions.Options` is available to Domain already (`IConfiguration` requires the same package), so this is not a hard blocker — but the following mistake is common:

Adding `services.AddOptions<EmailSettings>().BindConfiguration("EmailSettings").ValidateDataAnnotations()` in Domain's `ServiceExtensions` works fine. The pitfall is adding it **only** in `EuphoriaInn.Repository`'s `ServiceExtensions` or only in `Program.cs` without a corresponding `using` in Domain's extension, causing `IOptions<EmailSettings>` to be unresolvable at runtime even though the type compiles.

**Why it happens:**
DI registration and type resolution are separate concerns. The type compiles if the NuGet package is referenced; the registration must also be present in the DI container configuration path that runs during startup.

**Consequences:**
`InvalidOperationException: Unable to resolve service for type 'IOptions<EmailSettings>'` at the first request that triggers email sending. This is a runtime-only error; the build passes.

**Prevention:**
- Register `AddOptions<EmailSettings>()` in `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` inside `AddDomainServices()`, which is already called from `Program.cs`.
- Write a startup smoke test or health check that triggers DI resolution for `IEmailService`.

**Detection:**
Runtime exception on first email send. Not caught by build or unit tests unless DI resolution is exercised in integration tests.

**Phase:** Code quality / technical debt — email refactor

---

### Pitfall 6: Removing `Password` Property from `User` Domain Model Breaks `Equals` / `GetHashCode` Without Breaking the Build

**What goes wrong:**
`User.Equals()` and `User.GetHashCode()` in `EuphoriaInn.Domain/Models/User.cs` currently include `Password` in their computation. Removing the property without also removing it from `Equals`/`GetHashCode` produces a compile error — that part is safe.

The actual risk is the reverse: a developer removes `Password` from `Equals`/`GetHashCode` while leaving it in the class "temporarily," or removes it from the class without checking for any serialization or Razor `@Html.HiddenFor(m => m.Password)` usage in views. The `EntityProfile` currently has `opt.Ignore()` for password, but a future AutoMapper profile that does a `ReverseMap()` without an explicit ignore would silently start mapping empty string back into the user update path.

**Why it happens:**
`ReverseMap()` creates a bidirectional mapping with the same ignore rules only if the ignore is added to both directions explicitly. An `Ignore()` on direction A does not propagate to direction B with `ReverseMap()`.

**Consequences:**
Password field silently overwritten with empty string on next `UpdateAsync` call, locking users out of their accounts.

**Prevention:**
- Search all views and controllers for any field named `Password` on `User`-typed objects before removal.
- After removal, run `mapper.ConfigurationProvider.AssertConfigurationIsValid()` — this catches unmapped members on both directions.
- When using `ReverseMap()`, always chain `.ForPath(dest => dest.Password, opt => opt.Ignore())` on both direction's ignore calls explicitly.

**Detection:**
Check `grep -r "Password" EuphoriaInn.Service/Views/` and `grep -r "\.Password" EuphoriaInn.Service/Controllers/` before removal to find any direct references. AutoMapper validation test catches mapping gaps.

**Phase:** Security fixes phase

---

## Moderate Pitfalls

---

### Pitfall 7: Removing `UpdateQuestPropertiesAsync` from the Interface While the Dead Method Still Has Internal Callers

**What goes wrong:**
`UpdateQuestPropertiesAsync` (non-notification variant) appears dead — no controller calls it. However, if any test project, admin tooling, or background task (even in a comment-out or feature-flagged state) calls it via `IQuestService`, removing it from the interface breaks the build in that project.

**Prevention:**
Before removing from `IQuestService`, run a solution-wide `Find All References` on the method name. In this codebase it is confirmed unused, so removal is safe — but the check takes ten seconds and prevents surprises.

**Phase:** Code quality phase

---

### Pitfall 8: Extracting the 30-Minute `IsSameDateTime` Constant Changes Business Behaviour If Renamed Incorrectly

**What goes wrong:**
`IsSameDateTime` uses `Math.Abs((date1 - date2).TotalMinutes) <= 30`. This is used in the "update proposed dates intelligently" logic to decide whether a changed date is "the same date" (preserve votes) or a new date (drop votes). Extracting this as a named constant is safe. The pitfall is misunderstanding the intent and changing it to a date-only comparison (`date1.Date == date2.Date`) during the refactor, which would mean two dates on the same calendar day are always treated as the same quest date — including two genuinely different sessions.

**Prevention:**
Leave the value at `30` and add a code comment explaining the intent (fuzzy match to tolerate minor time-of-day edits). Do not change the value or comparison strategy as part of the naming refactor.

**Phase:** Code quality phase

---

### Pitfall 9: `SignupRole` Magic Number Fix Requires Changing Entity Layer, Not Just Domain Layer

**What goes wrong:**
`playerSignup.SignupRole == 1` in `QuestService.cs` compares an `int` column from `PlayerSignupEntity` against a raw literal. The fix is to cast to the `SignupRole` enum before comparing. However, `PlayerSignupEntity.SignupRole` is defined as `int` in the Repository entity, while `SignupRole` enum lives in Domain. Casting `(SignupRole)playerSignup.SignupRole` works but leaves the entity as `int`. The cleaner fix — making `PlayerSignupEntity.SignupRole` store the enum type — requires an EF Core value converter or a migration, which is a wider change than expected for a "rename magic number" task.

**Prevention:**
For this milestone, use the cast approach (`(SignupRole)playerSignup.SignupRole == SignupRole.Spectator`) in service code without touching the entity. Document that the entity-level type change is a future improvement. Do not conflate the two changes.

**Phase:** Code quality phase

---

### Pitfall 10: Removing `SecurityConfiguration` Leaves a Dead `appsettings.json` Section That Confuses Future Developers

**What goes wrong:**
Deleting `SecurityConfiguration.cs` is straightforward. Forgetting to remove the `Security` section from `appsettings.json` (and `appsettings.Development.json` if it exists) leaves a config block that does nothing, which future developers may try to use or assume is active.

**Prevention:**
Delete the class and the `appsettings.json` section in the same PR. Run a text search for `"Security"` in JSON config files after deletion to confirm no orphaned sections remain.

**Phase:** Code quality phase

---

### Pitfall 11: `SmtpClient` Is Obsolete — Replacing with MailKit Mid-Refactor Expands Scope Unexpectedly

**What goes wrong:**
`System.Net.Mail.SmtpClient` is marked `[Obsolete]` in .NET and will produce build warnings. The correct long-term replacement is `MailKit`. However, MailKit uses a different API surface (`MimeMessage`, `SmtpClient` from `MailKit.Net.Smtp`), requires a NuGet addition to Domain, and changes how SSL/TLS is configured (explicit vs implicit). Treating this as a "while we're in the file anyway" change during the email refactor can balloon a targeted fix into a library migration that requires touching every email path and introduces new auth patterns.

**Prevention:**
If the milestone scope is refactoring architecture, keep `System.Net.Mail.SmtpClient` and suppress the obsolete warning with a `#pragma warning disable SYSLIB0006` comment and a TODO note. Schedule MailKit migration as a separate milestone item. Do not couple library replacement to architectural refactoring.

**Phase:** Code quality / email refactor phase — explicitly defer MailKit migration

---

## Minor Pitfalls

---

### Pitfall 12: Renaming `CharacterViewModels/GuildMembersIndexViewModel.cs` May Break Namespace-Qualified References in Views

**What goes wrong:**
The file `CharacterViewModels/GuildMembersIndexViewModel.cs` actually defines `CharactersIndexViewModel` — the filename does not match the class name. Renaming the file is safe in C# (filename and class name don't need to match). The risk is any Razor view that uses a fully-qualified `@model EuphoriaInn.Service.ViewModels.CharacterViewModels.CharactersIndexViewModel` declaration — the namespace does not change with a file rename, so views are unaffected. However, if a developer also renames the directory or moves the file, the namespace changes and every `@model` directive using it must be updated.

**Prevention:**
Rename the file only — do not move it or rename the directory. Verify with `grep -r "CharactersIndexViewModel" EuphoriaInn.Service/Views/` that all view `@model` declarations use the class name, not a path.

**Phase:** Code quality phase

---

### Pitfall 13: `.env` in `.gitignore` Takes Effect Going Forward — Existing History Is Not Cleaned

**What goes wrong:**
Adding `.env` to `.gitignore` prevents future accidental commits but does not remove it from existing git history. If a developer ever clones a fresh copy and runs `git log --all -p -- .env`, the current placeholder `.env` content is visible. This is low risk now (no real secrets in current `.env`), but sets the wrong expectation.

**Prevention:**
Add `.env` to `.gitignore` and document that the current committed `.env` contains only placeholder values. If real secrets are ever found in history, use `git filter-repo` to rewrite — but that is not needed now. The `.env.example` pattern with documented setup instructions is the correct ongoing practice.

**Phase:** Security fixes phase

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Layer dependency direction fix | `EntityProfile` still imports Repository entities — the Domain → Repository link cannot be fully severed until mapping is restructured | Accept as a named, documented partial fix; do not claim full independence prematurely |
| Email dispatch moved to service | Stale pre-finalize quest state passed to email builder | `FinalizeQuestAsync` must return or re-fetch post-save recipient list |
| Lockout enablement | Existing users' `LockoutEnabled` column stays `false` | SQL migration `UPDATE AspNetUsers SET LockoutEnabled = 1` in same EF migration |
| `Password` property removal | AutoMapper `ReverseMap()` introduces silent password overwrite | Explicit `Ignore()` on both directions; run `AssertConfigurationIsValid()` |
| AutoMapper profile restructure | Assembly scanning with duplicate registration | Keep explicit `AddProfile<T>()` calls; never switch to unscoped `AppDomain` scanning |
| Dead method removal | `UpdateQuestPropertiesAsync` verified unused but solution-wide check still required | Run Find All References before removing from interface |
| Magic number extraction | `IsSameDateTime` semantics changed during rename | Rename constant only; do not change the `30` value or comparison logic |
| `.env` gitignore | History still contains `.env` | Acceptable if current content is placeholder; document that no real secrets were committed |

---

## Sources

- CONCERNS.md (codebase audit, 2026-04-15) — items 2, 3, 5, 8, 15, 16, 17, 18, 19, 20, 30
- ARCHITECTURE.md (codebase analysis, 2026-04-15) — AutoMapper profile locations, BaseService generic signature
- [AutoMapper duplicate profile registration issue](https://github.com/AutoMapper/AutoMapper.Extensions.Microsoft.DependencyInjection/issues/85) — confirmed duplicate registration is not idempotent
- [AutoMapper multiple assembly scanning issue](https://github.com/AutoMapper/AutoMapper/issues/2300) — confirmed profiles not loaded from other projects without explicit registration
- [ASP.NET Core Identity lockout configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-8.0) — `LockoutEnabled` per-user vs. global options (HIGH confidence, official docs)
- [Code Maze: User Lockout with ASP.NET Core Identity](https://code-maze.com/user-lockout-aspnet-core-identity/) — existing users need `LockoutEnabled = true` via migration (MEDIUM confidence)
- [EF Core: Effectively decouple the data and domain model](https://dev.to/thecodewrapper/ef-core-effectively-decouple-the-data-and-domain-model-4h8j) — entity/domain separation patterns (MEDIUM confidence)
- [Two gotchas with scoped and singleton dependencies in ASP.NET Core](https://blog.markvincze.com/two-gotchas-with-scoped-and-singleton-dependencies-in-asp-net-core/) — DI lifetime pitfalls (MEDIUM confidence)

---

*Pitfalls audit: 2026-04-15*
