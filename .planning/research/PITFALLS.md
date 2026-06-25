# Pitfalls Research — Milestone 4: Hangfire + Email Notifications

**Domain:** Adding Hangfire background jobs and HTML email templates to an existing ASP.NET Core 10 MVC app (SQL Server, EF Core, single Docker container, Resend SMTP relay)
**Researched:** 2026-06-25
**Confidence:** HIGH — all critical pitfalls verified against official Hangfire docs, GitHub issues, and official ASP.NET Core documentation

---

## Critical Pitfalls

### Pitfall 1: Scoped DbContext Injected Directly into a Hangfire Job Class

**What goes wrong:**
Hangfire job instances are resolved from DI once and reused across invocations, or resolved in a scope that does not match the HTTP request lifecycle. If a job class has `DbContext` (or any domain service that wraps one) in its constructor, the `DbContext` instance may be shared across concurrent job executions, disposed prematurely, or kept alive across unrelated operations. The runtime symptom is `InvalidOperationException: A second operation was started on this context instance before a previous operation completed` or `ObjectDisposedException`.

**Why it happens:**
The existing codebase registers all services as `Scoped` (correct for HTTP requests). Developers copy this pattern directly into a Hangfire job class, not realizing that Hangfire creates its own DI scope per-job execution — but only if you opt into it via the `UseActivator` extension. Without explicit scope management, the job's injected `DbContext` outlives or conflicts with the job's execution unit.

**How to avoid:**
Inject `IServiceScopeFactory` into the job constructor (it is a singleton — safe to inject). Inside the job method, create an explicit scope and resolve the service from it:

```csharp
public class SessionReminderJob(IServiceScopeFactory scopeFactory)
{
    public async Task ExecuteAsync(CancellationToken token)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var questService = scope.ServiceProvider.GetRequiredService<IQuestService>();
        // use questService — DbContext is properly scoped to this invocation
    }
}
```

Alternatively, call `.AddHangfire(...).AddHangfireServer()` with `.UseDefaultActivator()` replaced by a custom `IJobActivator` that creates a new scope per invocation — this makes standard constructor injection safe for job classes.

**Warning signs:**
- `InvalidOperationException` referencing "second operation" in Hangfire logs
- Intermittent `NullReferenceException` inside job execution when email list query returns unexpected results
- `ObjectDisposedException` on `QuestBoardContext`

**Phase to address:** Hangfire infrastructure setup phase (first phase that registers Hangfire and creates the job class skeleton).

---

### Pitfall 2: Default 10 Automatic Retries Cause Duplicate Emails

**What goes wrong:**
Hangfire retries failed jobs 10 times by default with exponential back-off. If a job sends N emails and then fails (e.g., the DB update that marks the job done throws), Hangfire retries and sends N emails again. Players receive multiple copies of the same reminder. This is worse for digest jobs: a player confirmed for 3 quests gets 3 digest emails, each sent multiple times.

**Why it happens:**
Email sends are not atomic with database state. The `EmailService` swallows exceptions internally (see current implementation — it catches and logs), so the send succeeds silently, but if any subsequent operation throws, Hangfire sees a failed job and retries the whole method.

**How to avoid:**
Design the job for idempotency before the first line of email sending:

1. Track send status in the database. Add a `ReminderSentAt` timestamp to the quest or a separate `EmailSentLog` table. Before sending any email in the job, query whether a reminder was already sent for this quest+date+player combination today.
2. Check before send, not after. The guard must happen inside the job method so retries skip already-sent notifications:

```csharp
if (quest.ReminderSentAt?.Date == DateTime.UtcNow.Date)
    return; // already sent today, skip silently
```

3. Reduce retry count for email jobs. Decorate the job method with `[AutomaticRetry(Attempts = 3)]` to limit blast radius if the idempotency guard itself has a bug.

4. Mark sent before calling email, or accept exactly-once cannot be guaranteed. Use a database transaction to atomically set `ReminderSentAt` and verify; if the transaction commits, the email is safe to send.

**Warning signs:**
- Players report receiving the same email twice in the same day
- Hangfire dashboard shows a job that previously succeeded being retried again
- `ReminderSentAt` or similar guard column not present in migrations

**Phase to address:** Reminder job implementation phase — the idempotency guard must be part of the initial job design, not retrofitted later.

---

### Pitfall 3: Hangfire Recurring Job Fires Immediately on First Registration

**What goes wrong:**
When `RecurringJob.AddOrUpdate` is called on startup for a job that has never run before (or whose cron expression changes), Hangfire treats the `LastExecutionTime` as null/past and immediately enqueues the job. For a daily reminder that should fire at 08:00, the first app start triggers a reminder send at whatever time the container starts — potentially at 03:00 after a Docker redeploy.

**Why it happens:**
Hangfire calculates "is it time to run?" by comparing `LastExecutionTime` + cron schedule against `DateTime.UtcNow`. A null `LastExecutionTime` makes the job overdue. This is documented behavior with multiple open GitHub issues (HangfireIO/Hangfire#1373, #1448, #1797) and no official fix.

**How to avoid:**
Two options — choose one:

Option A (preferred): Make the job logic harmless when run at odd hours. The reminder job queries "quests finalized for tomorrow" — if there are none, it exits silently. This is naturally safe even on unexpected firing, so no extra protection is needed.

Option B: Suppress the startup execution by calling `RecurringJob.Trigger` only from a separate one-time startup hook rather than relying on `AddOrUpdate` timing behavior.

Do not try to manually set `LastExecutionTime` in the HangFire schema directly — Hangfire schema writes are unsupported outside of Hangfire APIs and will be overwritten.

**Warning signs:**
- Email reminders arrive in the middle of the night after a deployment
- Hangfire dashboard shows the recurring job's first execution at a time that does not match the cron schedule
- Container restart logs show job execution within 30 seconds of startup

**Phase to address:** Hangfire infrastructure setup phase — job logic must be written to be safe at any execution time before the recurring registration is added.

---

### Pitfall 4: Hangfire Dashboard Exposed Without Authentication in Production

**What goes wrong:**
Hangfire's default dashboard policy allows access only from localhost. When the app runs inside Docker on a reverse-proxied server, `localhost` often matches the container's loopback interface — which means the dashboard is reachable from the host without authentication if port 8080 is even briefly exposed, or via the reverse proxy if headers are forwarded. Hangfire exposes full job arguments (serialized objects including player IDs, email addresses, and quest titles) in the dashboard UI.

**Why it happens:**
Hangfire's built-in `LocalRequestsOnlyAuthorizationFilter` checks `context.Request.IsLocal`, which depends on `HttpContext.Connection.RemoteIpAddress`. In Docker behind a reverse proxy, this IP is often `::1` or `127.0.0.1` (the proxy's loopback), making every request appear local.

**How to avoid:**
Replace the default authorization filter with a custom `IDashboardAuthorizationFilter` that checks ASP.NET Core Identity roles:

```csharp
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Admin");
    }
}
```

Register with:
```csharp
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});
```

Do not rely on `LocalRequestsOnlyAuthorizationFilter` in the Docker deployment.

**Warning signs:**
- `/hangfire` accessible without logging in
- Dashboard shows job arguments containing player email addresses
- Reverse proxy logs show requests to `/hangfire` from external IPs returning 200

**Phase to address:** Hangfire infrastructure setup phase — the dashboard must be locked before the route is registered; it cannot be added as an afterthought.

---

### Pitfall 5: Hangfire SQL Server Schema Conflicts with EF Core Startup Migration

**What goes wrong:**
The app calls `context.Database.Migrate()` on startup (auto-applied). Hangfire SQL Server storage also runs its own schema installer (`Install.sql`) on startup by default. In a fresh database, both run sequentially and succeed. In an already-running production database (container restart, redeploy), Hangfire's schema installer attempts `CREATE SCHEMA [HangFire]` which can fail with `SqlException: There is already an object named 'Schema' in the database` if a partial install occurred previously, crashing the app before it starts serving requests.

**Why it happens:**
Hangfire's schema installer is not idempotent by default in all code paths (issue HangfireIO/Hangfire#1586). The `PrepareSchemaIfNecessary` flag defaults to `true`, triggering schema creation on every startup.

**How to avoid:**
Hangfire's schema installer is `IF NOT EXISTS` guarded in modern versions (1.7+) so this is usually safe, but verify with explicit options:

```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        PrepareSchemaIfNecessary = true,  // keep true; modern Hangfire handles idempotency
        SchemaName = "HangFire"           // explicit prevents surprises
    }));
```

Do NOT use `Hangfire.EntityFrameworkCore` (the `sergezhigunov` package) — it is a third-party package that merges Hangfire tables into the EF Core migration chain. This adds maintenance overhead and breaks if Hangfire updates its schema independently. Use the official `Hangfire.SqlServer` package with its own schema management.

**Warning signs:**
- App fails to start after a container restart with `SqlException` referencing `HangFire` schema
- EF migrations succeed but app crashes before serving the first request
- Docker healthcheck fails repeatedly after redeploy

**Phase to address:** Hangfire infrastructure setup phase — test a cold-start AND a warm-restart (stop container, start again) before declaring setup complete.

---

### Pitfall 6: Manual DM Trigger and Recurring Job Send Reminders Concurrently to the Same Players

**What goes wrong:**
A DM clicks "Send Reminder" at 07:55. The recurring daily job fires at 08:00. Both enqueue a reminder job for the same quest. Both jobs read the database, find the same confirmed players, and send emails. Players receive two copies within minutes.

**Why it happens:**
`DisableConcurrentExecution` prevents two instances of the same job method from running simultaneously, but it does not prevent two different enqueued instances of the same method from queuing up and running one after the other. The idempotency guard from Pitfall 2 (`ReminderSentAt` check) is the correct defence — a second job execution within the same calendar day sees the timestamp set by the first and exits.

**How to avoid:**
The `ReminderSentAt` idempotency guard (Pitfall 2) is the primary fix. Additionally, set `DisableConcurrentExecution` on the job method to prevent simultaneous execution (locks the Hangfire row for the duration):

```csharp
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public async Task SendRemindersForQuestAsync(int questId) { ... }
```

Note: `DisableConcurrentExecution` does not work with in-memory storage — use SQL Server storage (as planned).

**Warning signs:**
- Players report two reminder emails received within minutes of each other
- Hangfire dashboard shows two "Succeeded" runs for the same quest on the same day
- `ReminderSentAt` column absent from the data model

**Phase to address:** Reminder job implementation phase — both `DisableConcurrentExecution` and `ReminderSentAt` guard must be in the initial implementation.

---

### Pitfall 7: Razor View Rendering Fails Inside a Background Job (No HttpContext)

**What goes wrong:**
The obvious approach for HTML email templates is to use `IRazorViewEngine` or `IRazorPageActivator` to render a `.cshtml` view to a string, then pass that string to the SMTP client. This works in controller actions. Inside a Hangfire background job, `IHttpContextAccessor.HttpContext` is `null`, causing `NullReferenceException` inside the Razor rendering pipeline when it tries to access request-scoped services (URL helpers, route data, anti-forgery tokens).

**Why it happens:**
The MVC Razor rendering engine depends on `HttpContext` being available via `IHttpContextAccessor`. Background job threads have no HTTP request context, so `HttpContext` is always null.

**How to avoid:**
Two reliable approaches for this stack:

Option A (simplest, recommended): Use a dedicated email template library that does not depend on `HttpContext`. RazorLight (NuGet: `RazorLight`) compiles `.cshtml` files using the Razor engine without requiring ASP.NET Core's request pipeline. Templates can be embedded resources or file-system paths. This is the standard approach documented for .NET email rendering outside of HTTP contexts.

Option B: Use C# string interpolation or a simple `StringBuilder`-based template for the small number of email types in this project. For two email types (finalized + reminder), this is maintainable and removes a dependency.

Do NOT attempt to fake an `HttpContext` by newing one up — this leads to partial rendering and missing route data.

**Warning signs:**
- `NullReferenceException` with stack trace through `Microsoft.AspNetCore.Mvc.Razor` inside a Hangfire job log
- Email body renders as empty string
- Template rendering works in dev (where `HttpContext` may be populated) but fails in Docker (where jobs run truly off-request)

**Phase to address:** HTML email template phase — the rendering strategy must be chosen before any template is written.

---

### Pitfall 8: Resend Stats Dashboard Assumes a Direct Aggregate API Endpoint (There Isn't One)

**What goes wrong:**
The PROJECT.md specifies "live sent/bounced/failed counts from Resend API." The natural assumption is that Resend has a `GET /stats` or `GET /summary` endpoint returning aggregate counts. It does not. Resend's API provides per-email retrieval (`GET /emails/{id}`) and list endpoints, but no server-side aggregate. The dashboard UI on resend.com aggregates these, but the API does not expose that aggregation.

**Why it happens:**
Resend is positioned as a transactional email API, not an analytics platform. Their analytics are available via their web dashboard and via webhooks feeding into external systems — not via a polling GET endpoint.

**How to avoid:**
Build the stats page using one of two approaches:

Option A (recommended for this scale): Call `GET /emails` (the list endpoint) and aggregate counts in application code. Filter by date range, count by `last_event` field values (`delivered`, `bounced`, `failed`). This works for 100 emails/day without hitting rate limits.

Option B: Configure Resend webhooks to push events (`email.sent`, `email.delivered`, `email.bounced`) to a local endpoint and store counts in a SQL table. Higher accuracy, requires a new inbound endpoint and migration.

The stats dashboard phase plan must explicitly describe which approach is used. Option A is simpler but requires an API key (not just the SMTP relay credentials). The current `EmailSettings` model has `SmtpUsername`/`SmtpPassword` but no `ApiKey` field — this field needs adding.

**Warning signs:**
- Phase plan references `GET /resend/stats` or similar — this endpoint does not exist
- Code calling `HttpClient` to Resend API fails with 404 on an assumed aggregate endpoint
- Stats dashboard shows 0 for all metrics because the wrong endpoint is called

**Phase to address:** Stats dashboard phase — the data source strategy must be defined in the phase plan before any HTTP client code is written.

---

### Pitfall 9: Digest Batching Groups Quests by Player ID but Uses Application Local Time Instead of UTC

**What goes wrong:**
The digest job is supposed to send one email per player who has multiple quests on the same calendar day. "Same day" means different things depending on timezone. If the job groups quests by `FinalizedDate.Date` using `DateTime.Now.Date` (local server time), players in a different timezone receive the wrong grouping, or quests near midnight are split across two digest emails. More concretely: the Docker container runs UTC; a Dutch player's "tomorrow" is UTC+2.

**Why it happens:**
`DateTime.Now` in a Docker Linux container is always UTC. `FinalizedDate` in the database may be stored in local time if the original finalization code used `DateTime.Now` rather than `DateTime.UtcNow`. The two do not match when the developer runs locally on Windows (UTC+1/+2) and the container runs on Linux (UTC+0).

**How to avoid:**
Audit the current `FinalizedDate` storage: check whether existing quests store UTC or local time. Use `DateTime.UtcNow` consistently throughout the reminder job. Group quests by `FinalizedDate.Date` normalized to UTC. For the Dutch audience in this project, UTC+1/+2 means "tomorrow" at 08:00 UTC is actually "tomorrow" for players — acceptable without full timezone support.

The reminder job's "find quests for tomorrow" query must use:
```csharp
var tomorrow = DateTime.UtcNow.Date.AddDays(1);
var quests = await questService.GetQuestsByFinalizedDateAsync(tomorrow);
```

Not `DateTime.Now` or `DateTime.Today`.

**Warning signs:**
- Reminders fire for today's quests, not tomorrow's, in the Docker environment
- Digest email groups quests incorrectly across calendar day boundary
- `FinalizedDate` values in database are 1-2 hours off from expected UTC times

**Phase to address:** Reminder job implementation phase — the UTC/local date boundary must be in the acceptance criteria.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Use in-memory Hangfire storage | No schema changes, faster to wire up | Jobs lost on container restart; `DisableConcurrentExecution` does not work; no dashboard persistence | Never — SQL Server is already present |
| Skip `ReminderSentAt` idempotency column | Saves one migration | Duplicate emails on any job retry | Never — adds one nullable `DateTime` column |
| Hardcode Resend API key in appsettings.json | Simpler setup | Key leaked in git history if committed | Never — use environment variable via `EmailSettings__ResendApiKey` |
| Use `DateTime.Now` in reminder job | Simpler code | Wrong day boundary in Docker (UTC vs local) | Never |
| Render email body as plain string interpolation | Eliminates RazorLight dependency | Harder to maintain multi-line HTML | Acceptable for MVP if templates stay simple (2 email types) |
| Register Hangfire job classes as Scoped in DI | Consistent with other services | DbContext may be shared across concurrent jobs if scope not managed in job method | Never — use `IServiceScopeFactory` inside the job |

---

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Hangfire + EF Core | Inject `IQuestService` directly into job constructor and use it across the full method | Inject `IServiceScopeFactory`, create scope inside job method, resolve `IQuestService` from scope |
| Hangfire + SQL Server | Leave `PrepareSchemaIfNecessary = true` without testing warm restart | Test stop+start of Docker container; Hangfire 1.8+ handles idempotency but verify explicitly |
| Hangfire Dashboard + Docker reverse proxy | Rely on `LocalRequestsOnlyAuthorizationFilter` | Implement `IDashboardAuthorizationFilter` checking `httpContext.User.IsInRole("Admin")` |
| Resend API + stats | Call a non-existent aggregate endpoint | Call `GET /emails` list endpoint and aggregate in app code; add `ResendApiKey` to `EmailSettings` |
| Resend SMTP + API stats | Assume SMTP credentials are separate from API credentials | The SMTP password is the API key — the same key works for REST API calls; add a named `ApiKey` property to `EmailSettings` for HTTP client code |
| RazorLight + background job | Use `IHttpContextAccessor` inside the template model or helpers | RazorLight renders without `HttpContext`; pass all model data explicitly as a typed model class |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Reminder job loads all quests, filters in C# | Job is slow; DbContext allocates large result sets | Add `WHERE FinalizedDate = @tomorrow` to the repository query | 100+ quests in the database |
| Stats page calls `GET /emails` on every page load without caching | Dashboard is slow; Resend rate-limit hit | Cache the result in-memory for 5 minutes (`IMemoryCache`) | Multiple admin users refreshing simultaneously |
| Digest job N+1: loads player details per quest in a loop | One DB roundtrip per confirmed player | Load all relevant quest+player data in a single `.Include()` query | 10+ quests for the same day |

---

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Hangfire dashboard with no auth filter | Any authenticated user (Player role) can view and re-trigger jobs; job args may contain email addresses | Implement `IDashboardAuthorizationFilter` requiring `Admin` role; test with a Player-role user account |
| Resend API key stored in `appsettings.json` (committed) | Key leaked to git; Resend account used for spam | Store in `.env` file (already in `.gitignore`); inject via `EmailSettings__ResendApiKey` env var in `docker-compose.yml` |
| `/hangfire` route not restricted at reverse proxy | Dashboard accessible to external users if reverse proxy forwards all paths | Rely on the auth filter as the primary control; consider also denying `/hangfire` at the reverse proxy level |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Hangfire setup:** Dashboard visited at `/hangfire` as a non-Admin user — must return 403, not the dashboard
- [ ] **Hangfire setup:** Container stopped and restarted — app must start without `SqlException` from schema conflicts; recurring job must not send emails at restart
- [ ] **Reminder job:** Player confirmed for 2 quests on the same day receives exactly 1 digest email (not 2 separate emails)
- [ ] **Reminder job:** Job executed twice in sequence (simulated retry) — player receives exactly 1 email total, not 2
- [ ] **Reminder job:** No quests finalized for tomorrow — job exits silently, no error in Hangfire dashboard
- [ ] **HTML templates:** Email renders correctly in Gmail, Outlook web, and mobile Gmail — table-based layout, inline styles only (no `class=` attributes)
- [ ] **Stats dashboard:** Shows 0 correctly when no emails sent (not an error state or exception)
- [ ] **Stats dashboard:** `ResendApiKey` environment variable absent — dashboard shows friendly "API key not configured" message, not an unhandled exception
- [ ] **Docker env vars:** `docker-compose.yml` updated with `EmailSettings__ResendApiKey` placeholder (commented, consistent with existing SMTP var pattern)

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Duplicate emails sent (Pitfall 2, 6) | LOW | Add `ReminderSentAt` guard via migration; redeploy; notify players of error; no data loss |
| Dashboard accessible to non-admins (Pitfall 4) | LOW | Hot-fix auth filter in `Program.cs`; redeploy; no data leaked if caught quickly |
| Scoped DbContext crash (Pitfall 1) | MEDIUM | Rewrite job class to use `IServiceScopeFactory`; job failures leave no data corruption — jobs are just failed in Hangfire dashboard |
| Hangfire schema startup crash (Pitfall 5) | LOW | Manually verify schema exists in SQL Server; if partial, run Hangfire install script manually once; set `PrepareSchemaIfNecessary = false` temporarily |
| Wrong Resend stats endpoint (Pitfall 8) | LOW | Swap to list+aggregate approach; no data loss; dashboard shows correct counts after fix |
| UTC/local date bug (Pitfall 9) | LOW | Change `DateTime.Now` to `DateTime.UtcNow` in one query; redeploy; no migration needed |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| 1 — Scoped DbContext in Hangfire job | Hangfire infrastructure setup | Unit test: resolve job class from DI scope, call method, assert no `InvalidOperationException` |
| 2 — Duplicate emails on retry | Reminder job implementation | Integration test: call job method twice for same quest+date; assert exactly 1 email sent |
| 3 — Recurring job fires on startup | Hangfire infrastructure setup | Acceptance criterion: container restart does not trigger reminder email send |
| 4 — Dashboard without auth | Hangfire infrastructure setup | Integration test: request `/hangfire` as Player-role user; assert 403 |
| 5 — Schema conflict on warm restart | Hangfire infrastructure setup | Manual test: `docker-compose stop && docker-compose up` — app must reach healthy state |
| 6 — Manual trigger + recurring collision | Reminder job implementation | Integration test: enqueue job twice concurrently; assert 1 email per player |
| 7 — Razor rendering without HttpContext | HTML template phase | Unit test: render template in test without `IHttpContextAccessor`; assert non-empty HTML string |
| 8 — Resend aggregate endpoint assumption | Stats dashboard phase | Phase plan must name the exact API endpoint before any HTTP client code is written |
| 9 — UTC vs local date in digest query | Reminder job implementation | Integration test: set `FinalizedDate` to UTC midnight tomorrow; assert job finds the quest |

---

## Sources

- [Hangfire — Dealing with Exceptions (official docs)](https://docs.hangfire.io/en/latest/background-processing/dealing-with-exceptions.html) — default 10 retry behavior
- [Hangfire — Best Practices (official docs)](https://docs.hangfire.io/en/latest/best-practices.html) — idempotency, minimal arguments
- [Hangfire — Using Dashboard (official docs)](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html) — default local-only access, `IDashboardAuthorizationFilter`
- [Hangfire — Using SQL Server (official docs)](https://docs.hangfire.io/en/latest/configuration/using-sql-server.html) — `PrepareSchemaIfNecessary`, schema management
- [HangfireIO/Hangfire#1373 — Recurrent job executes on app restart (GitHub)](https://github.com/HangfireIO/Hangfire/issues/1373) — immediate execution on `AddOrUpdate`
- [HangfireIO/Hangfire#1797 — Cron change causes immediate execution (GitHub)](https://github.com/HangfireIO/Hangfire/issues/1797) — confirmed behavior
- [HangfireIO/Hangfire#1960 — Job executed twice despite DisableConcurrentExecution (GitHub)](https://github.com/HangfireIO/Hangfire/issues/1960) — limitations of the attribute
- [Securing Hangfire Dashboard with Custom Auth Policy — Sahan Sera dev blog](https://sahansera.dev/securing-hangfire-dashboard-with-endpoint-routing-auth-policy-aspnetcore/) — role-based auth filter pattern
- [Prevent a Hangfire job from running when already active — Tim Deschryver](https://timdeschryver.dev/blog/prevent-a-hangfire-job-from-running-when-it-is-already-active) — `DisableConcurrentExecution` patterns and limits
- [Using Razor templates to render HTML emails in ASP.NET Core — End Point Dev, 2025](https://www.endpointdev.com/blog/2025/08/using-razor-templates-to-render-html-emails-in-asp-net/) — Razor outside HTTP context
- [Building a Resend analytics dashboard — Tinybird](https://www.tinybird.co/blog/building-a-resend-analytics-dashboard) — webhook-based aggregate stats; no direct aggregate API endpoint confirmed
- [Resend SMTP documentation](https://resend.com/docs/send-with-smtp) — SMTP logs not available; same API key used for SMTP and API calls
- [ASP.NET Core — Access HttpContext (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-context) — `HttpContext` null in background threads causes `NullReferenceException`

---

*Pitfalls research for: Milestone 4 — Hangfire + HTML Email Notifications (ASP.NET Core 10, SQL Server, Docker)*
*Researched: 2026-06-25*
