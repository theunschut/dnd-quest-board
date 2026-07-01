---
phase: 33-session-persistence-persist-activegroupid-across-app-restart
reviewed: 2026-07-01T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - QuestBoard.Repository/Migrations/20260701163850_AddSessionStateTable.cs
  - QuestBoard.Repository/Migrations/20260701163850_AddSessionStateTable.Designer.cs
  - QuestBoard.Service/QuestBoard.Service.csproj
  - QuestBoard.Service/Program.cs
  - QuestBoard.Service/Controllers/Admin/AdminController.cs
  - QuestBoard.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs
findings:
  critical: 0
  warning: 4
  info: 3
  total: 7
status: issues_found
---

# Phase 33: Code Review Report

**Reviewed:** 2026-07-01
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

This phase adds two independent pieces of work: (1) session persistence backed by
`AddDistributedSqlServerCache` with a hand-written raw-SQL migration for `AspNetSessionState`,
and (2) a per-target-user email-resend rate limiter (`PartitionedRateLimiter<int>`) enforced
in `AdminController.SendConfirmationEmail` and `EditUser`. No BLOCKER-level defects were found —
the migration DDL matches the documented `SqlServerCache` schema, the `Testing`-environment
guard correctly avoids writing to real SQL Server during `dotnet test`, and the rate-limiter
partition key (target userId) is sound and independently tested.

The most significant gap: nothing in the codebase ever purges expired rows from
`AspNetSessionState`. `Microsoft.Extensions.Caching.SqlServer` does **not** self-clean;
Microsoft's own tooling (`dotnet sql-cache create` / the `SqlServerCache` docs) expects the
consuming app to schedule a periodic delete. Since this project already has Hangfire wired up
for recurring jobs (`DailyReminderJob`), this is a simple, low-risk fix that was seemingly missed.
A few smaller robustness and testability issues are noted below.

## Warnings

### WR-01: `AspNetSessionState` has no expiry cleanup job — the table will grow unbounded

**File:** `QuestBoard.Service/Program.cs:160-168`
**Issue:** `AddDistributedSqlServerCache` inserts/updates rows in `AspNetSessionState` on every
session write, refreshing `ExpiresAtTime`/`SlidingExpirationInSeconds`, but the package itself
never deletes expired rows — that responsibility is explicitly left to the host application
(see Microsoft's `SqlServerCache` docs, which ship a companion cleanup stored-proc/scheduled-task
sample for exactly this reason). The migration even creates `Index_ExpiresAtTime`
(`AddSessionStateTable.cs:26`) specifically to support such a cleanup query, but no job ever
issues it. Every authenticated browser session (24h idle timeout, `Program.cs:177`) leaves a
permanent row once it expires. Over the lifetime of a long-running deployment this table grows
without bound, degrading the `AspNetSessionState` PK/index and, more importantly, retaining
stale session payloads (which may include `ActiveGroupId` and other session state) indefinitely.
**Fix:** Add a small recurring Hangfire job alongside `DailyReminderJob` that deletes expired
rows, e.g.:
```csharp
public class SessionCleanupJob(QuestBoardContext context)
{
    public async Task ExecuteAsync(CancellationToken token)
    {
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM [dbo].[AspNetSessionState] WHERE [ExpiresAtTime] < SYSUTCDATETIME()",
            token);
    }
}
```
and register it in `Program.cs` next to the existing `RecurringJob.AddOrUpdate<DailyReminderJob>`
call (e.g. hourly: `"0 * * * *"`).

### WR-02: `PartitionedRateLimiter<int>` singleton is never disposed

**File:** `QuestBoard.Service/Program.cs:144-153`
**Issue:** `PartitionedRateLimiter<TKey>` implements `IAsyncDisposable`/`IDisposable` (it owns a
`ConcurrentDictionary` of per-partition limiters, each with internal timers for
`AutoReplenishment`). Registering it via `AddSingleton(_ => PartitionedRateLimiter.Create<int, string>(...))`
means the DI container holds the only reference, but nothing calls `Dispose`/`DisposeAsync` on
application shutdown — `AddSingleton` for an instance produced by a factory delegate does register
it for disposal by the container in ASP.NET Core (root `ServiceProvider.Dispose()` disposes owned
singletons), so this is *not* a leak in practice, but it means any exception thrown during shutdown
disposal is silently absorbed by the generic host and not surfaced. This is a low-risk item; flagging
because rate limiters with internal replenishment timers are exactly the kind of resource where a
missed dispose is easy to introduce later (e.g. if this registration is ever changed to
`AddSingleton<PartitionedRateLimiter<int>>(instance)` with a pre-built instance passed by value,
which does *not* get auto-disposed by some DI containers). No fix required now, but worth a
one-line comment noting the auto-dispose behavior is relied upon.
**Fix:** Add a comment: `// Disposed automatically by the root ServiceProvider on shutdown (factory-created singleton).`

### WR-03: `EditUser`'s rate-limit lease is acquired but the `HasKey`/`Name` update is never rolled back on 429

**File:** `QuestBoard.Service/Controllers/Admin/AdminController.cs:181-199`
**Issue:** `await userService.UpdateAsync(user)` (line 188) persists `Name`/`HasKey`/(non-email)
changes to the database *before* the rate-limit check on line 194. If the rate limit is exceeded,
the method returns `429` immediately without calling `RedirectToAction`, but the `Name`/`HasKey`
changes made above have already been committed. This isn't incorrect per se (those fields should
update regardless), but the response the admin sees (a raw `429` `Content(...)` response, not a
redirect back to `Users` with a partial-success message) gives no indication that everything
*except* the email-change/notification succeeded. An admin retrying the whole form 5 minutes later
may believe nothing happened the first time.
**Fix:** After a 429 on the email-change branch, still redirect to `Users` with an informative
`TempData["Error"]` (e.g. "Name/HasKey updated, but the email-confirmation email could not be sent —
too many requests. Try again later.") instead of returning a bare `429 Content` response, so the UI
reflects the partial success consistently with the rest of the controller's UX pattern.

### WR-04: Migration's `Down()` does not recreate any prior state and silently no-ops if index/table already dropped

**File:** `QuestBoard.Repository/Migrations/20260701163850_AddSessionStateTable.cs:31-34`
**Issue:** Minor, but the `Up()` guards table creation with `IF NOT EXISTS` (defensive against a
pre-existing table from manual ops work), while `Down()` uses `DROP TABLE IF EXISTS` — consistent
guard style, good. However `Down()` provides no way to distinguish "this migration's table" from a
table that may have been created out-of-band before this migration ran (matching the `IF NOT EXISTS`
comment's own scenario). If an operator manually pre-created `AspNetSessionState` for some reason
and then later rolls back this migration, `Down()` will destroy it with no way to know it wasn't
this migration's own creation. Low likelihood, but the asymmetry between `Up()`'s "leave it alone if
it already exists" caution and `Down()`'s unconditional drop is worth calling out.
**Fix:** Acceptable as-is for a session-state table (no user data), but add a one-line comment in
`Down()` noting the intentional asymmetry, e.g. `// Session data is disposable; unlike Up(), we don't
guard against a pre-existing table here.`

## Info

### IN-01: `SendConfirmationEmail`'s rate-limit lease acquired before validating the user exists or is unconfirmed

**File:** `QuestBoard.Service/Controllers/Admin/AdminController.cs:282-303`
**Issue:** The `AttemptAcquire(userId)` call happens before `userService.GetByIdAsync(userId)` and
before the `user.EmailConfirmed` check. This means a typo'd/non-existent `userId`, or repeatedly
clicking resend for an *already-confirmed* user, still consumes the 3/hour budget for that userId —
budget is burned even though no email would ever be sent. This is a minor UX/robustness nit (an
attacker could exhaust a real user's budget by POSTing arbitrary non-existent-then-real IDs, but
since this endpoint is `[Authorize(Policy = "AdminOnly")]`, the practical impact is limited to admin
self-footgun, not an external threat).
**Fix:** Move the `GetByIdAsync` and `EmailConfirmed` checks before the `AttemptAcquire` call so the
budget is only spent on requests that would actually enqueue an email.

### IN-02: Duplicated antiforgery-cookie-refresh boilerplate across three test helper closures

**File:** `QuestBoard.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs:228-243, 330-354, 379-399`
**Issue:** The pattern `GetAsync(...) -> ExtractAntiForgeryTokenAsync -> Remove/Add Cookie header ->
CreateFormContentWithAntiForgeryToken` is repeated near-verbatim in `PostSendConfirmationEmailAsync`,
the local `PostEditUserWithEmailChangeAsync`, and the local `PostCreateUserAsync`. This is test code
(explicitly out of BLOCKER/WARNING scope per review rules unless it affects reliability), but it's
worth flagging as a maintenance cost — any future change to the antiforgery cookie-refresh contract
requires updating three near-identical copies.
**Fix:** Extract a shared helper (e.g. `AntiForgeryHelper.PostWithFreshTokenAsync(client, url, getUrl, formValues)`)
usable across all three call sites and future tests in this file.

### IN-03: `EditUser_EmailChange_ExceedingRateLimit_ShouldReturn429` and `SendConfirmationEmail_ExceedingRateLimit_ShouldReturn429` share the rate-limit budget space via the underlying singleton but rely purely on distinct-userId isolation

**File:** `QuestBoard.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs:249-273, 320-366`
**Issue:** Both tests correctly avoid `ClearDatabaseAsync` and rely on unique auto-incremented
userIds for isolation from the process-wide `PartitionedRateLimiter<int>` singleton (well
documented in the surrounding comments). However, `SendConfirmationEmail` and `EditUser`'s
email-change branch share the *same* rate-limit key namespace (`email-resend:{userId}`, per
`Program.cs:146`) — a test that calls both endpoints against the same target userId (none currently
does, but nothing prevents a future test from doing so) would see cross-endpoint budget bleed that
isn't obvious from either test in isolation. Not a current bug, just a latent test-suite trap.
**Fix:** Add a one-line comment near the `PartitionedRateLimiter<int>` registration in `Program.cs`
(or in the test file) noting that `SendConfirmationEmail` and `EditUser`'s email-change path share
one budget per target userId — so tests exercising both against the same userId must account for
combined request counts.

---

_Reviewed: 2026-07-01_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
