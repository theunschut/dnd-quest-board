# Feature Research

**Domain:** Email notifications for an ASP.NET Core 10 MVC app (D&D quest board)
**Researched:** 2026-06-25
**Confidence:** HIGH (Hangfire, Razor HtmlRenderer verified via Context7 + official docs; Resend API verified via official docs)

---

## Feature Landscape

### Table Stakes (Users Expect These)

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| HTML email body (not plain text) | All modern transactional email looks styled; plain-text feels like an error | LOW | Use `Microsoft.AspNetCore.Components` `HtmlRenderer` + Razor component per template. `IsBodyHtml = true` on `MailMessage`. No extra NuGet needed beyond what is already in net10 SDK. |
| Quest-finalization email upgraded to HTML | Existing send path already works; this is a visual upgrade only | LOW | Drop-in replacement: same `IEmailService.SendQuestFinalizedEmailAsync` signature, body goes from string literal to rendered HTML string. |
| Date-change notification upgraded to HTML | Same as above | LOW | Same pattern as finalization upgrade. |
| Hangfire dashboard at `/hangfire` (admin only) | Ops/admin expect a job monitor; without it background jobs are invisible | LOW | `app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new AdminOnlyFilter() } })`. `AdminOnlyFilter` checks `httpContext.User.IsInRole("Admin")`. |
| 24h session reminder email (automatic) | Players forget sessions; a reminder the day before is standard for any scheduling app | MEDIUM | Hangfire recurring job with cron `"0 9 * * *"` (daily 09:00 server time). Job queries DB for quests where `FinalizedDate.Date == DateTime.UtcNow.Date.AddDays(1)`, sends to all confirmed players. |
| DM manual reminder trigger | DMs want to ping players on demand without waiting for the automatic job | LOW | Controller action calls `IBackgroundJobClient.Enqueue(() => reminderService.SendReminderAsync(questId))`. Reuses same send logic as the automatic job — no separate code path. |

### Differentiators (Competitive Advantage)

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Digest batching: one email per player when confirmed for multiple same-day quests | Prevents inbox spam when a player joins two quests on the same date; polished UX that generic scheduling apps miss | MEDIUM | Requires grouping: fetch all tomorrow's finalized quests → group confirmed players by player ID → for each player with N > 1 quests, send one digest email listing all N quests. For N = 1, send the single-quest reminder template. The digest template must handle a variable-length quest list. |
| Admin email stats dashboard (live from Resend API) | Gives the admin visibility into actual delivery health without leaving the app | HIGH | **Resend has no aggregate stats endpoint.** `GET /emails` returns individual email records (up to 100 per page, cursor-paginated) each with a `last_event` field. Possible `last_event` values: `email.sent`, `email.delivered`, `email.bounced`, `email.failed`, `email.complained`, `email.delivery_delayed`, `email.opened`, `email.clicked`, `email.scheduled`, `email.suppressed`, `email.received`. The admin dashboard must page through all results client-side (or fetch last N) and group/count by `last_event`. This is non-trivial if volume is large; at 100 emails/day it is manageable. Use the official `Resend` NuGet package (v0.5.1, targets net8.0+, compatible with net10.0). |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Webhook receiver for real-time Resend event ingestion | Seems like the "right" way to track delivery status | Requires a public HTTPS endpoint, webhook secret verification, persistent event store, and extra infrastructure — massive complexity for a self-hosted hobby app at 100 emails/day | Aggregate stats by calling `GET /emails` on page load in the admin dashboard (acceptable latency for an internal tool) |
| Per-email delivery tracking stored in DB | Admins want to see "did player X get the email for quest Y?" | Adds a new DB table, background sync job, and display UI — out of proportion to the value for a small group | Show aggregate counts from Resend API; individual failures are visible in the Resend dashboard |
| Opt-out / unsubscribe management | Looks like a legal/compliance feature | Not legally required for internal transactional mail to a known closed group; adds link-in-email complexity | Trust the group; allow players to update their email in profile settings |
| Email open/click tracking pixels | Would add engagement metrics | Resend tracks this (`email.opened`, `email.clicked`) but surfacing it in the app adds complexity with low value for a D&D group | Resend's own dashboard already shows open rates |
| Retry logic inside `IEmailService` | Resilience against transient SMTP failures | Hangfire already retries failed background jobs automatically (default: 10 attempts with exponential backoff) — double-retrying creates duplicate emails | Rely on Hangfire's built-in retry; make `IEmailService` idempotent |

---

## Feature Dependencies

```
[HTML email templates]
    └──required by──> [Quest-finalization HTML upgrade]
    └──required by──> [Session reminder email]
    └──required by──> [Digest email]

[Hangfire infrastructure (AddHangfire + AddHangfireServer + SQL Server storage)]
    └──required by──> [24h automatic reminder (recurring job)]
    └──required by──> [DM manual reminder trigger (fire-and-forget enqueue)]
    └──required by──> [Hangfire dashboard]

[24h automatic reminder]
    └──extended by──> [Digest batching]
        (digest is a variation of the reminder send path — same infrastructure, different grouping logic)

[DM manual reminder trigger]
    └──reuses──> [24h automatic reminder send logic]
        (both call the same ReminderService method — only the trigger differs)

[Admin email stats dashboard]
    └──requires──> [Resend SDK (NuGet: Resend 0.5.1)]
    └──independent of──> [Hangfire infrastructure]
```

### Dependency Notes

- **HTML templates required first:** The reminder and digest features depend on having styled templates. Build templates before wiring up the jobs.
- **Hangfire infrastructure is a single setup block:** `AddHangfire`, `AddHangfireServer`, `UseHangfireDashboard` all go in `Program.cs` together. Do this once before any jobs are registered.
- **Digest extends reminder, not replaces it:** The digest grouping logic wraps the single-quest reminder path. Implement single-quest reminder first, then layer digest grouping on top.
- **DM manual trigger is nearly free once recurring job exists:** The recurring job and manual trigger share one `ReminderService.SendReminderAsync(int questId)` method. The only difference is how it is invoked (scheduled vs. enqueued on demand).
- **Admin stats dashboard is independent:** It only calls the Resend REST API; it has no dependency on Hangfire or the reminder features.

---

## MVP Definition

### Launch With (Phase 1 of this milestone)

- [ ] Hangfire infrastructure installed and wired (SQL Server storage, `AddHangfireServer`, dashboard at `/hangfire` admin-only) — unblocks all background job features
- [ ] HTML email templates (Razor components via `HtmlRenderer`) — unblocks all styled email sends
- [ ] Quest-finalization email upgraded to HTML — highest user-visible payoff, low risk

### Add After Validation (Phase 2)

- [ ] 24h automatic session reminder (recurring job `"0 9 * * *"`) — requires Hangfire and HTML templates
- [ ] DM manual reminder trigger — requires recurring job infrastructure (same send logic)
- [ ] Digest batching — add after single-quest reminder is working

### Complete in Final Phase

- [ ] Admin email stats dashboard — independent, self-contained, does not block other features

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Hangfire infrastructure setup | LOW (invisible) | LOW | P1 (enables everything else) |
| HTML email templates | HIGH | LOW | P1 |
| Quest-finalization email HTML upgrade | HIGH | LOW | P1 |
| Date-change email HTML upgrade | MEDIUM | LOW | P1 |
| 24h automatic session reminder | HIGH | MEDIUM | P1 |
| DM manual reminder trigger | MEDIUM | LOW | P1 |
| Digest batching | MEDIUM | MEDIUM | P2 |
| Admin email stats dashboard | LOW | HIGH | P2 |
| Hangfire dashboard (admin-only) | LOW | LOW | P1 (included with Hangfire setup) |

---

## Implementation Reference

### HTML Email Templates (Razor Components)

The idiomatic ASP.NET Core 8+ approach (applies equally to net10):

1. Create a Razor component (`.razor`) per email type — inherits `ComponentBase`, accepts a `[Parameter]` view model
2. Create a layout component inheriting `LayoutComponentBase` for shared header/footer
3. In `IEmailService` implementation, inject `HtmlRenderer` (registered via `builder.Services.AddRazorComponents()` or directly as `HtmlRenderer`)
4. Render: call `htmlRenderer.Dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(...)))`
5. Pass the resulting HTML string as `mailMessage.Body` with `IsBodyHtml = true`

No third-party library required. `HtmlRenderer` ships in `Microsoft.AspNetCore.Components` which is already in net10.

Known limitation: `HtmlRenderer` ignores `@layout` directives (GitHub issue dotnet/aspnetcore#55068). Use a wrapper component that includes the layout component explicitly, or inline shared header/footer in each template.

### Hangfire Setup (net10, SQL Server)

NuGet packages needed in the Service project:
- `Hangfire.AspNetCore` 1.8.23
- `Hangfire.SqlServer` 1.8.23

`Program.cs` registration:

```csharp
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();
```

Hangfire creates its own schema tables in the existing SQL Server database automatically (`PrepareSchemaIfNecessary` defaults to `true`). No separate connection string needed unless desired.

Dashboard authorization (admin-only):

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AdminOnlyDashboardFilter() }
});

public class AdminOnlyDashboardFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.IsInRole("Admin");
    }
}
```

### Recurring Reminder Job (cron expression)

Daily at 09:00 server time:

```csharp
RecurringJob.AddOrUpdate<IReminderService>(
    "session-reminder-daily",
    svc => svc.SendDueRemindersAsync(),
    "0 9 * * *");
```

`SendDueRemindersAsync` queries for quests where `FinalizedDate.Date == DateTime.UtcNow.Date.AddDays(1)`, then groups confirmed players and sends (single or digest).

### Manual DM Trigger (fire-and-forget)

Controller receives `questId`, injects `IBackgroundJobClient`:

```csharp
_backgroundJobClient.Enqueue<IReminderService>(svc => svc.SendReminderForQuestAsync(questId));
```

Identical send logic to the recurring job — just targeted at one quest.

### Resend API for Admin Stats Dashboard

No aggregate endpoint exists. Implementation approach:

1. Call `GET https://api.resend.com/emails?limit=100` with `Authorization: Bearer {apiKey}` header
2. Check `has_more`; if true, paginate with `before` cursor until all records fetched (or cap at a rolling window, e.g., last 7 days by filtering `created_at` after fetching)
3. Group results by `last_event` value and count
4. Display counts for: `email.delivered`, `email.bounced`, `email.failed`, `email.complained`

Use `Resend` NuGet package (v0.5.1) or raw `HttpClient`. The `Resend` package does not expose a dedicated stats method — it wraps the same `GET /emails` endpoint.

At 100 emails/day, 7 days = ~700 records = 7 pages of 100. Acceptable for a synchronous admin dashboard load.

---

## Sources

- Hangfire recurring jobs + SQL Server setup: [Hangfire Documentation (Context7)](https://context7.com/hangfireio/hangfire.documentation/llms.txt) — HIGH confidence
- Hangfire dashboard authorization: [Hangfire Dashboard docs](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html) — HIGH confidence
- Hangfire NuGet version: [NuGet Gallery — Hangfire.AspNetCore 1.8.23](https://www.nuget.org/packages/Hangfire.AspNetCore/) — HIGH confidence
- HtmlRenderer for email templates: [End Point Dev — Using Razor Templates (2025)](https://www.endpointdev.com/blog/2025/08/using-razor-templates-to-render-html-emails-in-asp-net/) — HIGH confidence
- HtmlRenderer layout limitation: [dotnet/aspnetcore#55068](https://github.com/dotnet/aspnetcore/issues/55068) — MEDIUM confidence (open GitHub issue)
- Resend `GET /emails` endpoint fields and `last_event` values: [Resend API reference — List Sent Emails](https://resend.com/changelog/list-sent-emails-endpoint) + [Resend webhook event types](https://resend.com/docs/dashboard/webhooks/event-types) — HIGH confidence
- Resend has no aggregate stats endpoint: verified via [Resend llms.txt](https://resend.com/docs/llms.txt) — HIGH confidence
- Resend NuGet package (v0.5.1, net8.0+): [NuGet Gallery — Resend](https://www.nuget.org/packages/Resend/) — HIGH confidence

---

*Feature research for: Milestone 4 Email Notifications — D&D Quest Board*
*Researched: 2026-06-25*
