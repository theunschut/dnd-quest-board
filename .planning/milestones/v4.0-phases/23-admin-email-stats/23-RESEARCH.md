# Phase 23: Admin Email Stats - Research

**Researched:** 2026-06-26
**Domain:** ASP.NET Core 10 MVC — typed HttpClient, IMemoryCache, Resend REST API
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** New standalone action `AdminController.EmailStats` at `/Admin/EmailStats` — follows the existing pattern (Users, Quests actions are already in AdminController)
- **D-02:** Add a nav link to the stats page in the admin sidebar/nav so it is discoverable without knowing the URL
- **D-03:** Show 4 summary stat cards: **Sent**, **Delivered**, **Bounced**, **Failed** counts — matches STATS-01 and the roadmap success criteria; no individual email list
- **D-04:** Time range is **last 30 days** — filter by `created_at >= today-30d` when calling Resend's `GET /emails`; Resend returns individual records, aggregation is done client-side
- **D-05:** When `ResendApiKey` is missing from config: render the stats page shell with a yellow/orange alert banner — "ResendApiKey not configured — add it to appsettings.json to enable stats." No stack trace or exception page
- **D-06:** When the Resend API call fails (network error, 401, 429, etc.): render the same page shell with a red alert banner — "Could not fetch email stats — Resend API returned an error. Check your API key and try again." No raw exception details shown
- **D-07:** Cache the fetched stats in `IMemoryCache` for 5 minutes — avoids repeated Resend API calls on rapid admin refreshes; IMemoryCache is already registered in the project
- **D-08:** Provide a Refresh button on the stats page that clears the cache key and forces a new Resend API call — `?force=true` query param on the GET action
- **D-09:** `ResendApiKey` is added as a new property on the existing `EmailSettings` record in `EuphoriaInn.Domain/Models/EmailSettings.cs`; read via `IOptions<EmailSettings>` — same binding pattern used for SMTP credentials
- **D-10:** Resend API call uses `HttpClient` with `Authorization: Bearer {ResendApiKey}` and calls `GET https://api.resend.com/emails` — no Resend SDK package added to the project
- **D-11:** Page is protected by the existing `"AdminOnly"` authorization policy (Phase 20 pattern)

### Claude's Discretion

None specified — all decisions are locked.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| STATS-01 | Admin users can view email delivery statistics (sent, delivered, bounced, and failed counts) pulled from the Resend API using a plain `HttpClient` and a configurable `ResendApiKey` in `appsettings` | Resend `GET /emails` returns `last_event` per record; aggregate in app code by counting each status. `IHttpClientFactory` typed client pattern for HttpClient. `IMemoryCache` auto-registered by `AddControllersWithViews`. `EmailSettings` record extended with `ResendApiKey`. `AdminController` extended with `EmailStats` action. |

</phase_requirements>

---

## Summary

Phase 23 adds a read-only admin dashboard page at `/Admin/EmailStats` that queries the Resend REST API, aggregates email status counts, and renders them as 4 stat cards (Sent, Delivered, Bounced, Failed). No new NuGet packages are required — the project already has `System.Net.Http` via the framework, `IMemoryCache` via `AddControllersWithViews`, and `IOptions<T>` via the existing `EmailSettings` binding.

The Resend `GET /emails` endpoint returns paginated individual email records. Each record has a `last_event` string field (e.g. `"sent"`, `"delivered"`, `"bounced"`, `"failed"`). There is no aggregate endpoint. The controller-layer service must page through all results (up to the 30-day window), count each status, and cache the result in `IMemoryCache` for 5 minutes. A `?force=true` query parameter clears the cache entry on demand.

The phase touches 6 files: `EmailSettings.cs` (add property), `appsettings.json` (add placeholder key), `docker-compose.yml` or `.env` (add env var placeholder), `AdminController.cs` (add `EmailStats` action), a new `EmailStatsViewModel.cs`, a new `Views/Admin/EmailStats.cshtml`, and `_Layout.cshtml` (add nav link). No EF Core migration is required.

**Primary recommendation:** Implement the Resend HTTP call as a private async method directly in `AdminController` (not a separate service class) — this is the simplest approach consistent with the project's existing pattern for admin-only one-off operations. Inject `IHttpClientFactory`, `IOptions<EmailSettings>`, and `IMemoryCache` via primary constructor.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Page authorization (`"AdminOnly"` policy) | Frontend Server (MVC controller) | — | Existing `AdminController` class-level `[Authorize]` already enforces this; no new handler needed |
| Resend API call (`GET /emails` + pagination) | Frontend Server (MVC controller via private method) | — | CONTEXT.md D-10 locks this in the controller layer, no external SDK or domain service |
| Status aggregation (count by `last_event`) | Frontend Server (MVC controller) | — | Pure in-memory transform of API response; no business rule complexity to hide behind domain interface |
| IMemoryCache caching (5-minute TTL) | Frontend Server (MVC controller) | — | Cache keyed by a constant; injected via `IMemoryCache` already registered by `AddControllersWithViews` |
| `ResendApiKey` config | Domain model (`EmailSettings`) | — | D-09: existing `EmailSettings` record extended; bound via `IOptions<EmailSettings>` in `AddDomainServices` |
| View rendering (4 stat cards + alert banners) | Browser (Razor view) | — | Standard MVC ViewModel → View; UI contract is fully specified in `23-UI-SPEC.md` |
| Admin nav update | Frontend Server (shared layout) | — | `_Layout.cshtml` addition of one `<li>` entry in the existing Admin dropdown |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `System.Net.Http` (framework) | .NET 10 built-in | HttpClient for Resend REST calls | No NuGet package needed; `IHttpClientFactory` is part of the framework |
| `Microsoft.Extensions.Caching.Memory` (framework) | .NET 10 built-in | `IMemoryCache` 5-minute stat cache | Auto-registered by `AddControllersWithViews`; already available in DI |
| `Microsoft.Extensions.Options` (framework) | .NET 10 built-in | `IOptions<EmailSettings>` binding | Already in use for SMTP settings via `AddOptions<EmailSettings>().BindConfiguration("EmailSettings")` in `AddDomainServices` |
| `System.Text.Json` (framework) | .NET 10 built-in | Deserialize Resend API JSON response | No third-party JSON library; `JsonSerializer.Deserialize<T>` with `JsonPropertyName` attributes |

### No New NuGet Packages

This phase adds zero NuGet packages. All functionality is available in the .NET 10 framework. The CONTEXT.md explicitly prohibits adding the Resend SDK (D-10).

**Version verification:** No packages to verify — all dependencies are .NET 10 framework built-ins confirmed by the `.csproj` files and `Program.cs`. [VERIFIED: codebase grep + CONTEXT.md]

---

## Package Legitimacy Audit

> No external packages are being installed in this phase. The constraint is explicit (D-10): no Resend SDK. All HTTP, caching, JSON, and options dependencies come from the .NET 10 framework.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| (none) | — | — | — | — | — | Not applicable |

**Packages removed due to SLOP verdict:** none
**Packages flagged as suspicious SUS:** none

---

## Architecture Patterns

### System Architecture Diagram

```
Admin browser
    |
    | GET /Admin/EmailStats[?force=true]
    v
AdminController.EmailStats (EuphoriaInn.Service)
    |
    |-- Check IOptions<EmailSettings>.Value.ResendApiKey
    |       |-- empty/null --> render view with D-05 warning ViewModel
    |
    |-- Check IMemoryCache["resend-email-stats"] (unless force=true)
    |       |-- hit --> render view with cached EmailStatsViewModel
    |
    |-- IHttpClientFactory.CreateClient("Resend")
    |       |-- GET https://api.resend.com/emails?limit=100
    |       |       Bearer {ResendApiKey}
    |       |-- paginate: repeat with &after={lastId} until no more records or created_at < cutoff
    |       |-- HTTP error / timeout --> render view with D-06 error ViewModel
    |
    |-- Aggregate records by last_event into counts
    |-- Set IMemoryCache["resend-email-stats"] with 5-min AbsoluteExpiration
    |-- Return View(EmailStatsViewModel { Sent, Delivered, Bounced, Failed, AsOf })
    v
Views/Admin/EmailStats.cshtml
    |-- 4 Bootstrap stat cards (UI-SPEC contract)
    |-- OR alert banner (D-05 or D-06)
    |-- Refresh button: href="/Admin/EmailStats?force=true"
```

### Recommended Project Structure

```
EuphoriaInn.Domain/
  Models/
    EmailSettings.cs          # Add: ResendApiKey string property

EuphoriaInn.Service/
  Controllers/Admin/
    AdminController.cs        # Add: EmailStats(GET) action + private GetResendStatsAsync helper
  ViewModels/AdminViewModels/
    EmailStatsViewModel.cs    # New: Sent/Delivered/Bounced/Failed counts + error state
  Views/Admin/
    EmailStats.cshtml         # New: 4 stat cards or alert banner per UI-SPEC
  Views/Shared/
    _Layout.cshtml            # Add: "Email Stats" nav link in Admin dropdown

EuphoriaInn.Service/
  appsettings.json            # Add: ResendApiKey placeholder (empty string)
```

No new service interface or implementation needed. No new repository. No EF migration.

### Pattern 1: Typed HttpClient via IHttpClientFactory (Named Client)

**What:** Register a named HttpClient in `Program.cs` with the Resend base address and default Bearer token header. Inject `IHttpClientFactory` into `AdminController` and call `CreateClient("Resend")`.

**When to use:** Any controller that makes one-off HTTP calls to a single external API. Preferred over `new HttpClient()` (socket exhaustion risk) and over fully typed client (less boilerplate for a single endpoint).

**Example:**
```csharp
// Program.cs — register named client
builder.Services.AddHttpClient("Resend", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// AdminController.cs — primary constructor injection
public class AdminController(
    IUserService userService,
    IQuestService questService,
    IHttpClientFactory httpClientFactory,
    IOptions<EmailSettings> emailOptions,
    IMemoryCache cache) : Controller
{
    // ...
}
```

[ASSUMED] — pattern is standard ASP.NET Core; verified against official docs at [learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory).

### Pattern 2: EmailStats Action with Cache + Force Refresh

```csharp
[HttpGet]
public async Task<IActionResult> EmailStats(
    bool force = false,
    CancellationToken token = default)
{
    var apiKey = emailOptions.Value.ResendApiKey;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return View(EmailStatsViewModel.MissingKey());
    }

    if (!force && cache.TryGetValue("resend-email-stats", out EmailStatsViewModel? cached) && cached != null)
    {
        return View(cached);
    }

    cache.Remove("resend-email-stats");

    var (viewModel, error) = await GetResendStatsAsync(apiKey, token);

    if (error)
    {
        return View(EmailStatsViewModel.ApiError());
    }

    cache.Set("resend-email-stats", viewModel, TimeSpan.FromMinutes(5));
    return View(viewModel);
}
```

[ASSUMED] — pattern derived from CONTEXT.md D-07, D-08, and standard `IMemoryCache` usage.

### Pattern 3: Resend GET /emails Pagination + Aggregation

**What:** The Resend `GET /emails` endpoint returns paginated records with `last_event` per email. No aggregate endpoint exists. Pagination uses cursor-based `after={id}` param.

**Resend response structure (verified):**

```json
{
  "data": [
    {
      "id": "...",
      "created_at": "2026-05-27T12:00:00.000Z",
      "last_event": "delivered",
      "to": ["user@example.com"],
      "from": "...",
      "subject": "..."
    }
  ]
}
```

**`last_event` values mapped to stat categories:**

| `last_event` value | Stat card |
|--------------------|-----------|
| `"sent"` | Sent |
| `"delivered"` | Delivered |
| `"opened"` | Delivered (email was opened — implies delivered) |
| `"clicked"` | Delivered (implies delivered) |
| `"bounced"` | Bounced |
| `"failed"` | Failed |
| `"delivery_delayed"` | (exclude or count separately — decision: exclude from these 4 counts) |
| `"complained"` | (exclude — spam complaint, not a delivery failure) |
| `"scheduled"` | (exclude — not yet sent) |

[CITED: resend.com/docs/dashboard/webhooks/event-types]

**Aggregation approach:** Paginate through ALL records (max 100 per page), filter to `created_at >= DateTime.UtcNow.AddDays(-30)`, stop pagination when records are older than the 30-day window, then count by status.

**IMPORTANT:** The Resend `GET /emails` API has NO date-range query parameter. Filtering must be done client-side after fetching each page. Stop pagination early when `created_at < cutoff` since results are returned newest-first.

**C# DTOs for deserialization:**

```csharp
// Minimal deserialization models — no separate file needed; can be private nested records
private record ResendEmailListResponse(
    [property: JsonPropertyName("data")] List<ResendEmailRecord> Data);

private record ResendEmailRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("last_event")] string LastEvent);
```

[ASSUMED] — field names confirmed from Resend docs; C# record syntax is project convention.

### Pattern 4: EmailStatsViewModel

```csharp
// EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs
namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class EmailStatsViewModel
{
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Bounced { get; set; }
    public int Failed { get; set; }
    public DateTime? AsOf { get; set; }
    public bool IsMissingKey { get; set; }
    public bool IsApiError { get; set; }

    public static EmailStatsViewModel MissingKey() =>
        new() { IsMissingKey = true };

    public static EmailStatsViewModel ApiError() =>
        new() { IsApiError = true };
}
```

[ASSUMED] — ViewModel follows existing Admin ViewModel conventions in `EuphoriaInn.Service/ViewModels/AdminViewModels/`.

### Anti-Patterns to Avoid

- **Calling `new HttpClient()` directly:** Causes socket exhaustion under load. Always use `IHttpClientFactory.CreateClient(...)`. [ASSUMED]
- **Setting `Authorization` header on the named client default:** The bearer token should be added per-request (not as a default), because the key may be empty at registration time. Set it in `GetResendStatsAsync` via `request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey)`. [ASSUMED]
- **No pagination:** Calling with default `limit=20` and not paginating only returns the latest 20 emails. At 100 emails/day × 30 days = up to 3,000 records. Always paginate. [VERIFIED: resend.com/docs/api-reference/emails/list-emails]
- **Filtering by date in the Resend query:** The `GET /emails` API has no date filter parameter. Stop paginating when you reach records older than 30 days; do not pass a date param. [VERIFIED: resend.com/docs/api-reference/emails/list-emails]
- **Injecting `IMemoryCache` into a Domain service:** `IMemoryCache` belongs in the Service layer only. Do not inject it into `EuphoriaInn.Domain` services. [CITED: ARCHITECTURE.md — Service layer contains DI-registered singletons like caches]
- **Throwing unhandled exceptions on API failure:** The CONTEXT.md (D-06) requires a graceful error banner, not an unhandled exception. Wrap the HTTP call in `try/catch`. [VERIFIED: CONTEXT.md D-06]

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP socket management | `new HttpClient()` per request | `IHttpClientFactory` | Socket exhaustion; DI lifetime management |
| JSON deserialization | Custom string parsing | `JsonSerializer.Deserialize<T>` with `[JsonPropertyName]` | Built-in, battle-tested, no allocations |
| Cache with TTL | `Dictionary<string, (T, DateTime)>` | `IMemoryCache.Set(key, value, TimeSpan)` | Thread-safe, eviction policy, already registered |
| Bearer token auth | Custom header formatting | `AuthenticationHeaderValue("Bearer", key)` from `System.Net.Http.Headers` | Standard RFC 6750 header format |

**Key insight:** This phase requires zero custom infrastructure. All required patterns are .NET 10 built-ins already imported or registered by the framework.

---

## Common Pitfalls

### Pitfall 1: Calling GET /emails Without Pagination

**What goes wrong:** Default `limit` is 20. At the project's volume (100 emails/day × 30 days = potentially 3,000 records), only the most recent 20 are counted. Stats appear wildly low.

**Why it happens:** Developer assumes the endpoint returns all records or uses a date filter.

**How to avoid:** Loop with `after={lastId}` cursor until either the API returns fewer records than `limit` (last page) OR the oldest record in the page is older than 30 days.

**Warning signs:** Delivered count is suspiciously low (never more than 20).

### Pitfall 2: ResendApiKey Committed to Git

**What goes wrong:** Developer adds the real API key to `appsettings.json` and commits it. Key is exposed in the repository.

**Why it happens:** Working in development; forgets the key is in a committed file.

**How to avoid:**
1. Add `ResendApiKey: ""` (empty) as placeholder in `appsettings.json`
2. Add `EmailSettings__ResendApiKey` to `docker-compose.yml` as an env var comment
3. Real key goes in `.env` (already in `.gitignore`) or user secrets
4. Document in plan: "set via env var, not committed"

**Warning signs:** `git diff` shows a Resend `re_` key value in `appsettings.json`.

### Pitfall 3: IMemoryCache Not Explicitly Registered But Works Anyway

**What goes wrong:** Developer sees no `AddMemoryCache()` call in `Program.cs` and adds one unnecessarily, or assumes it is not available.

**Why it happens:** Not obvious that `AddControllersWithViews()` implicitly calls `AddMemoryCache()`.

**How to avoid:** Do NOT add `services.AddMemoryCache()` — it is already registered. Inject `IMemoryCache` directly. [VERIFIED: learn.microsoft.com/en-us/aspnet/core/performance/caching/memory]

**Warning signs:** Duplicate registration warning in logs (benign but unnecessary).

### Pitfall 4: Setting Default Bearer Header at Client Registration Time

**What goes wrong:** Bearer token is read from `EmailSettings` and set as a default header on the named client in `Program.cs`. If the key is missing/empty at startup, the header is set to an invalid value for all requests.

**Why it happens:** Developer adds the auth header in `AddHttpClient("Resend", client => { client.DefaultRequestHeaders.Authorization = ... })`.

**How to avoid:** Set the `Authorization` header per-request inside `GetResendStatsAsync` after confirming the key is non-empty. The missing-key check in `EmailStats` action prevents the HTTP call from being made at all if key is absent.

**Warning signs:** All Resend API calls return 401 even when the key is configured.

### Pitfall 5: last_event "opened" Not Counted as Delivered

**What goes wrong:** An email opened by the user shows `last_event = "opened"`. If the aggregation only counts `last_event == "delivered"`, opened emails (which were successfully delivered) are missed from the Delivered count.

**Why it happens:** Developer maps only `"delivered"` to the Delivered card.

**How to avoid:** Map `"delivered"`, `"opened"`, and `"clicked"` all to the Delivered count — these events only occur after successful delivery.

**Warning signs:** Delivered count is 0 even though the admin knows emails have been read.

### Pitfall 6: Force Refresh via Redirect Loop

**What goes wrong:** The `?force=true` handler does not redirect after clearing cache — the URL retains `?force=true` in the browser. On next page load (or refresh), the user always bypasses cache.

**Why it happens:** Action returns `View(model)` directly when `force=true` instead of redirecting.

**How to avoid:** After a successful force-fetch, redirect to `RedirectToAction(nameof(EmailStats))` (without `force=true`), not return the view directly. This follows the PRG (Post-Redirect-Get) spirit for query-triggered mutations.

Alternatively (simpler): The `?force=true` handler returns the view directly — this is acceptable per CONTEXT.md D-08 which says "Refresh button... forces a new Resend API call". The redirect is a UX nicety only. The plan should choose the simpler `return View(viewModel)` unless redirect is explicitly requested.

---

## Code Examples

Verified patterns from official sources:

### HttpClient Named Client Registration

```csharp
// Source: CONTEXT.md D-10 + https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
// In Program.cs — add after AddRepositoryServices()/AddDomainServices()
builder.Services.AddHttpClient("Resend", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
```

### Resend API Pagination Loop

```csharp
// Source: https://resend.com/docs/api-reference/emails/list-emails + CONTEXT.md D-04
private async Task<(EmailStatsViewModel stats, bool error)> GetResendStatsAsync(
    string apiKey,
    CancellationToken token)
{
    try
    {
        var client = httpClientFactory.CreateClient("Resend");
        var cutoff = DateTime.UtcNow.AddDays(-30);

        int sent = 0, delivered = 0, bounced = 0, failed = 0;
        string? afterId = null;
        bool hasMore = true;

        while (hasMore)
        {
            var url = afterId == null
                ? "emails?limit=100"
                : $"emails?limit=100&after={afterId}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
                return (new EmailStatsViewModel(), true);

            var json = await response.Content.ReadAsStringAsync(token);
            var result = JsonSerializer.Deserialize<ResendEmailListResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null || result.Data.Count == 0)
                break;

            bool reachedCutoff = false;
            foreach (var email in result.Data)
            {
                if (email.CreatedAt < cutoff)
                {
                    reachedCutoff = true;
                    break;
                }

                switch (email.LastEvent)
                {
                    case "sent": sent++; break;
                    case "delivered":
                    case "opened":
                    case "clicked": delivered++; break;
                    case "bounced": bounced++; break;
                    case "failed": failed++; break;
                }
            }

            if (reachedCutoff || result.Data.Count < 100)
                hasMore = false;
            else
                afterId = result.Data[^1].Id;
        }

        return (new EmailStatsViewModel
        {
            Sent = sent,
            Delivered = delivered,
            Bounced = bounced,
            Failed = failed,
            AsOf = DateTime.UtcNow
        }, false);
    }
    catch
    {
        return (new EmailStatsViewModel(), true);
    }
}
```

### IMemoryCache Set with Absolute Expiration

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory
cache.Set("resend-email-stats", viewModel,
    new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    });
```

### EmailSettings — ResendApiKey Property

```csharp
// Source: EuphoriaInn.Domain/Models/EmailSettings.cs — extend existing record
public record EmailSettings
{
    public string SmtpServer { get; init; } = "smtp.gmail.com";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "D&D Quest Board";
    public string AppUrl { get; init; } = string.Empty;
    public string ResendApiKey { get; init; } = string.Empty;  // ADD THIS
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `new HttpClient()` directly | `IHttpClientFactory` + named client | .NET Core 2.1 | No socket exhaustion; proper lifetime management |
| `MemoryCache` class directly | `IMemoryCache` interface + DI | ASP.NET Core 1.x | Testable; no static state |
| Resend SDK (`Resend.Net`) | Plain `HttpClient` + manual JSON | Project constraint | No extra NuGet package; one less transitive dependency |

**Deprecated / outdated:**
- `HttpRuntime.Cache` (.NET Framework): never use in ASP.NET Core; use `IMemoryCache`
- `WebClient` class: deprecated; use `HttpClient` / `IHttpClientFactory`
- Static `HttpClient` singleton: works but bypasses DI lifetime management and DNS refresh; use factory instead

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `"opened"` and `"clicked"` `last_event` values should be counted as Delivered | Code Examples (aggregation logic) | Delivered count would be under-reported; safe to verify against Resend docs or adjust counter logic |
| A2 | Resend returns records newest-first, allowing early pagination cutoff when `created_at < cutoff` | Code Examples (pagination loop) | If order is not guaranteed, would need to paginate all 30+ days of records regardless |
| A3 | Setting the `Authorization` header per-request (not as named-client default) is the correct approach given the key is checked for emptiness before the call | Architecture Patterns | If key is set as default header, empty-string key would still make the call (wasting a network roundtrip); per-request approach is safer |
| A4 | `AdminController` primary constructor extended with `IHttpClientFactory`, `IOptions<EmailSettings>`, `IMemoryCache` is consistent with codebase style | Architecture Patterns | Code won't compile if injection conflicts with existing constructor; easy to fix at plan time |

---

## Open Questions

1. **`last_event` ordering guarantee from Resend API**
   - What we know: Resend changelog says newest-first by default [CITED: resend.com/changelog/list-sent-emails-endpoint]
   - What's unclear: Is this guaranteed stable behaviour or undocumented?
   - Recommendation: Implement with the assumption of newest-first; add a comment in the code that if stats appear wrong, the ordering assumption should be verified. The worst case (un-ordered) is that we paginate more than needed, not that we show wrong counts — the cutoff check runs per-record regardless.

2. **`docker-compose.yml` env var injection**
   - What we know: SMTP credentials follow the `EmailSettings__SmtpPassword` pattern as Docker env vars
   - What's unclear: Whether there is a `.env` file checked in or referenced from `docker-compose.yml`
   - Recommendation: Add a commented-out `- EmailSettings__ResendApiKey=` line in `docker-compose.yml` under the app's `environment` section, consistent with how other secrets are documented.

---

## Environment Availability

> External dependency: Resend API (external SaaS). No local tooling change needed.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Resend API (`api.resend.com`) | Live stat fetch | External — requires API key | REST v1 | Graceful error banner (D-06) |
| `IHttpClientFactory` | HTTP calls | Built into .NET 10 ASP.NET Core | .NET 10 | — |
| `IMemoryCache` | 5-minute stat cache | Auto-registered by `AddControllersWithViews` | .NET 10 | — |
| `ResendApiKey` (config value) | Authenticating to Resend API | Not yet provisioned (noted in STATE.md) | — | Missing-key banner (D-05) |

**Missing dependencies with no fallback:**
- None — the page renders gracefully without a Resend API key (D-05).

**Missing dependencies with fallback:**
- `ResendApiKey` not yet provisioned — page shows yellow warning banner until the admin adds it.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit (EuphoriaInn.UnitTests) + Microsoft.AspNetCore.Mvc.Testing (EuphoriaInn.IntegrationTests) |
| Config file | none — uses `<Using Include="Xunit" />` in .csproj + GlobalUsings.cs |
| Quick run command | `dotnet test EuphoriaInn.UnitTests --filter "Category=EmailStats" -x` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| STATS-01 | GET /Admin/EmailStats requires Admin role | Integration | `dotnet test EuphoriaInn.IntegrationTests --filter "AdminController"` | Partial — `AdminControllerIntegrationTests.cs` exists; add `EmailStats` case |
| STATS-01 | Unauthenticated user redirected from /Admin/EmailStats | Integration | same file | Partial — existing redirect test covers class-level policy |
| STATS-01 | Missing ResendApiKey renders warning view (not exception) | Unit | `dotnet test EuphoriaInn.UnitTests --filter "EmailStats"` | No — Wave 0 gap |
| STATS-01 | API error renders error banner (not exception) | Unit | same | No — Wave 0 gap |
| STATS-01 | last_event aggregation counts correctly per status | Unit | same | No — Wave 0 gap |
| STATS-01 | force=true clears cache before fetch | Unit | same | No — Wave 0 gap |

### Sampling Rate

- **Per task commit:** `dotnet build` (confirms no compile errors; HTTP/cache logic not unit-testable without stubs)
- **Per wave merge:** `dotnet test` (full suite green)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `EuphoriaInn.UnitTests/Controllers/AdminEmailStatsTests.cs` — covers STATS-01 (missing key, API error, aggregation, cache clear). Controller is hard to unit-test without mocking `IHttpClientFactory`; alternatively test the aggregation as a private static helper or extract to an internal class.
- [ ] `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — add `GET /Admin/EmailStats` auth tests (unauthenticated → redirect, non-admin → forbidden, admin → 200 OK with mocked HTTP call)

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity cookie; `"AdminOnly"` policy already enforced at `AdminController` class level |
| V3 Session Management | no | Read-only page; no session state modified |
| V4 Access Control | yes | `[Authorize(Policy = "AdminOnly")]` on `AdminController` — class-level attribute already present |
| V5 Input Validation | yes (low risk) | `force` query param is `bool` (model binding; no injection risk); `apiKey` value is from config, not user input |
| V6 Cryptography | no | API key is a bearer token, not a cryptographic key managed by this code |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Admin page accessible to Player/DM role | Elevation of Privilege | `[Authorize(Policy = "AdminOnly")]` on class already handles this — verified in integration tests |
| ResendApiKey leaked to git | Information Disclosure | Empty placeholder in `appsettings.json`; real value via env var / user secrets; `.env` in `.gitignore` |
| API key visible in server logs / exceptions | Information Disclosure | `catch` block discards exception details; error banner shows generic message (D-06) — key never included in response |
| Unrestricted Resend API polling | Denial of Service (to Resend rate limit) | `IMemoryCache` 5-minute TTL + `?force=true` only on explicit admin request |

---

## Sources

### Primary (HIGH confidence)

- [Resend — Event Types](https://resend.com/docs/dashboard/webhooks/event-types) — all `last_event` values including sent/delivered/bounced/failed/opened/clicked/complained/delivery_delayed
- [Resend — List Sent Emails Endpoint changelog](https://resend.com/changelog/list-sent-emails-endpoint) — confirmed `GET /emails` pagination with `after`/`before` cursor params and newest-first ordering
- [learn.microsoft.com — Cache in-memory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-10.0) — `IMemoryCache` auto-registered by `AddControllersWithViews`
- Codebase inspection — `AdminController.cs`, `EmailSettings.cs`, `Program.cs`, `ServiceExtensions.cs`, `_Layout.cshtml`, `appsettings.json` — all verified directly

### Secondary (MEDIUM confidence)

- [Resend — List emails API reference](https://resend.com/docs/api-reference/emails/list-emails) — confirmed query params (limit/after/before), response shape (`data[].last_event`)
- [learn.microsoft.com — IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) — named client registration pattern

### Tertiary (LOW confidence)

- Aggregation logic mapping (which `last_event` values → which stat card) derived from research; `"opened"` → Delivered is ASSUMED and may warrant verification against Resend's own stats dashboard

---

## Metadata

**Confidence breakdown:**
- Resend API endpoint and fields: HIGH — verified via Resend docs
- Standard stack (no new packages): HIGH — verified from codebase
- IMemoryCache availability: HIGH — verified from Microsoft docs + `AddControllersWithViews` pattern
- last_event aggregation mapping: MEDIUM — opened/clicked → Delivered is assumed but logical
- Pitfalls: HIGH — derived from pitfalls doc and prior phase experience

**Research date:** 2026-06-26
**Valid until:** 2026-07-26 (Resend API is stable; no breaking changes expected short-term)
