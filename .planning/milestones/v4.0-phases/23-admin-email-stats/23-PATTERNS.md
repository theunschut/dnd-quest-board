# Phase 23: Admin Email Stats - Pattern Map

**Mapped:** 2026-06-26
**Files analyzed:** 6
**Analogs found:** 5 / 6

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `EuphoriaInn.Domain/Models/EmailSettings.cs` | model | config | self (existing file, extend) | exact |
| `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` | controller | request-response | self (existing file, extend) | exact |
| `EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs` | viewmodel | request-response | `EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs` | role-match |
| `EuphoriaInn.Service/Views/Admin/EmailStats.cshtml` | view | request-response | `EuphoriaInn.Service/Views/Admin/Quests.cshtml` | role-match |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | layout | request-response | self (existing file, extend) | exact |
| `EuphoriaInn.Service/appsettings.json` | config | — | self (existing file, extend) | exact |

---

## Pattern Assignments

### `EuphoriaInn.Domain/Models/EmailSettings.cs` (model, config)

**Analog:** self — existing record, add one property

**Existing record structure** (lines 1-12):
```csharp
namespace EuphoriaInn.Domain.Models;

public record EmailSettings
{
    public string SmtpServer { get; init; } = "smtp.gmail.com";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "D&D Quest Board";
    public string AppUrl { get; init; } = string.Empty;
}
```

**Change:** Append `public string ResendApiKey { get; init; } = string.Empty;` as the last property before the closing brace.

---

### `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` (controller, request-response)

**Analog:** self — existing controller, add `EmailStats` action and extend primary constructor

**Constructor pattern** (line 11):
```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminController(IUserService userService, IQuestService questService, IIdentityService identityService, IEmailService emailService) : Controller
```

Extend to add `IHttpClientFactory httpClientFactory`, `IOptions<EmailSettings> emailOptions`, `IMemoryCache cache` to the primary constructor parameter list.

**Existing HttpGet action pattern** (lines 13-39):
```csharp
[HttpGet]
public async Task<IActionResult> Users()
{
    var allUsers = await userService.GetAllAsync();
    // ... build viewmodels ...
    return View(sortedUsers);
}
```

**TempData error pattern** (lines 229-231):
```csharp
TempData["Error"] = $"Failed to send confirmation email to {user.Name}. Please try again.";
return RedirectToAction(nameof(Users));
```

**New EmailStats action — copy this pattern** (from RESEARCH.md Pattern 2):
```csharp
[HttpGet]
public async Task<IActionResult> EmailStats(bool force = false, CancellationToken token = default)
{
    var apiKey = emailOptions.Value.ResendApiKey;

    if (string.IsNullOrWhiteSpace(apiKey))
        return View(EmailStatsViewModel.MissingKey());

    if (!force && cache.TryGetValue("resend-email-stats", out EmailStatsViewModel? cached) && cached != null)
        return View(cached);

    cache.Remove("resend-email-stats");

    var (viewModel, error) = await GetResendStatsAsync(apiKey, token);

    if (error)
        return View(EmailStatsViewModel.ApiError());

    cache.Set("resend-email-stats", viewModel,
        new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    return View(viewModel);
}
```

**Private pagination helper** — nested private records + async method (from RESEARCH.md Pattern 3):
```csharp
private record ResendEmailListResponse(
    [property: JsonPropertyName("data")] List<ResendEmailRecord> Data);

private record ResendEmailRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("last_event")] string LastEvent);

private async Task<(EmailStatsViewModel stats, bool error)> GetResendStatsAsync(
    string apiKey, CancellationToken token)
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
            var url = afterId == null ? "emails?limit=100" : $"emails?limit=100&after={afterId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
                return (new EmailStatsViewModel(), true);

            var json = await response.Content.ReadAsStringAsync(token);
            var result = JsonSerializer.Deserialize<ResendEmailListResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Data == null || result.Data.Count == 0) break;

            bool reachedCutoff = false;
            foreach (var email in result.Data)
            {
                if (email.CreatedAt < cutoff) { reachedCutoff = true; break; }
                switch (email.LastEvent)
                {
                    case "sent": sent++; break;
                    case "delivered": case "opened": case "clicked": delivered++; break;
                    case "bounced": bounced++; break;
                    case "failed": failed++; break;
                }
            }

            if (reachedCutoff || result.Data.Count < 100) hasMore = false;
            else afterId = result.Data[^1].Id;
        }

        return (new EmailStatsViewModel
        {
            Sent = sent, Delivered = delivered, Bounced = bounced,
            Failed = failed, AsOf = DateTime.UtcNow
        }, false);
    }
    catch
    {
        return (new EmailStatsViewModel(), true);
    }
}
```

**Required using statements to add at top of file:**
```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using EuphoriaInn.Domain.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
```

---

### `EuphoriaInn.Service/ViewModels/AdminViewModels/EmailStatsViewModel.cs` (viewmodel, request-response)

**Analog:** `EuphoriaInn.Service/ViewModels/AdminViewModels/UserManagementViewModel.cs`

**Analog namespace + class pattern** (lines 1-13):
```csharp
using EuphoriaInn.Domain.Models;

namespace EuphoriaInn.Service.ViewModels.AdminViewModels;

public class UserManagementViewModel
{
    public User User { get; set; } = new();
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsAdmin { get; set; }
    public bool IsDungeonMaster { get; set; }
    public bool IsPlayer { get; set; }
    public bool EmailConfirmed { get; set; }
}
```

**New EmailStatsViewModel — follow this namespace + class pattern:**
```csharp
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

    public static EmailStatsViewModel MissingKey() => new() { IsMissingKey = true };
    public static EmailStatsViewModel ApiError() => new() { IsApiError = true };
}
```

No `using` statements needed (no domain model dependency for this ViewModel).

---

### `EuphoriaInn.Service/Views/Admin/EmailStats.cshtml` (view, request-response)

**Analog:** `EuphoriaInn.Service/Views/Admin/Quests.cshtml`

**Modern-card wrapper pattern** (lines 10-17):
```html
<div class="card modern-card">
    <div class="card-header modern-card-header">
        <h2 class="mb-0">
            <i class="fas fa-scroll text-danger me-2"></i>
            Quest Management
        </h2>
    </div>
    <div class="card-body modern-card-body">
```

**Model directive pattern** (lines 1-8):
```cshtml
@model EuphoriaInn.Service.ViewModels.AdminViewModels.EmailStatsViewModel

@{
    ViewData["Title"] = "Email Stats";
}
```

**Alert banner pattern** — use Bootstrap alert classes (consistent with TempData banners elsewhere):
```html
@if (Model.IsMissingKey)
{
    <div class="alert alert-warning" role="alert">
        <i class="fas fa-exclamation-triangle me-2"></i>
        ResendApiKey not configured — add it to appsettings.json to enable stats.
    </div>
}
else if (Model.IsApiError)
{
    <div class="alert alert-danger" role="alert">
        <i class="fas fa-times-circle me-2"></i>
        Could not fetch email stats — Resend API returned an error. Check your API key and try again.
    </div>
}
```

**4-stat-card grid** — use Bootstrap `row`/`col` inside `modern-card-body`, color per category:
```html
<div class="row g-3">
    <div class="col-sm-6 col-md-3">
        <div class="card text-bg-primary text-center p-3">
            <div class="fs-1 fw-bold">@Model.Sent</div>
            <div><i class="fas fa-paper-plane me-1"></i>Sent</div>
        </div>
    </div>
    <div class="col-sm-6 col-md-3">
        <div class="card text-bg-success text-center p-3">
            <div class="fs-1 fw-bold">@Model.Delivered</div>
            <div><i class="fas fa-check-circle me-1"></i>Delivered</div>
        </div>
    </div>
    <div class="col-sm-6 col-md-3">
        <div class="card text-bg-warning text-center p-3">
            <div class="fs-1 fw-bold">@Model.Bounced</div>
            <div><i class="fas fa-exclamation-circle me-1"></i>Bounced</div>
        </div>
    </div>
    <div class="col-sm-6 col-md-3">
        <div class="card text-bg-danger text-center p-3">
            <div class="fs-1 fw-bold">@Model.Failed</div>
            <div><i class="fas fa-times-circle me-1"></i>Failed</div>
        </div>
    </div>
</div>
```

**Refresh button pattern** — use `d-flex justify-content-between` with `<hr>` before button row:
```html
<hr />
<div class="d-flex justify-content-end">
    <a href="/Admin/EmailStats?force=true" class="btn btn-secondary">
        <i class="fas fa-sync-alt me-2"></i>Refresh
    </a>
</div>
```

---

### `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` (layout, request-response)

**Analog:** self — existing dropdown list items (lines 37-53)

**Existing Admin dropdown item pattern** (lines 38-47):
```html
<li>
    <a class="dropdown-item" asp-controller="Admin" asp-action="Users">
        <i class="fas fa-users-cog me-2"></i>User Management
    </a>
</li>
<li>
    <a class="dropdown-item" asp-controller="Admin" asp-action="Quests">
        <i class="fas fa-scroll me-2"></i>Quest Management
    </a>
</li>
```

**New nav item to add** — insert after the `Quests` `<li>` block, before the Background Jobs item (line 48):
```html
<li>
    <a class="dropdown-item" asp-controller="Admin" asp-action="EmailStats">
        <i class="fas fa-envelope-open-text me-2"></i>Email Stats
    </a>
</li>
```

---

### `EuphoriaInn.Service/appsettings.json` (config)

**Analog:** self — existing `EmailSettings` section (lines 15-23):
```json
"EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "",
    "FromName": "D&D Quest Board",
    "AppUrl": ""
}
```

**Change:** Add `"ResendApiKey": ""` as the last property in the `EmailSettings` object. Value stays empty; real value set via `EmailSettings__ResendApiKey` environment variable.

---

### `EuphoriaInn.Service/Program.cs` (config — named HttpClient registration)

**Analog:** self — existing `builder.Services.AddRepositoryServices(...)` block (line 79)

**Named HttpClient registration to add** after `AddDomainServices` call:
```csharp
builder.Services.AddHttpClient("Resend", client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
```

Do NOT set `Authorization` header here — set it per-request inside `GetResendStatsAsync`.

---

## Shared Patterns

### Authorization
**Source:** `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` line 10
**Apply to:** `EmailStats` action (already covered by class-level attribute — no per-action `[Authorize]` needed)
```csharp
[Authorize(Policy = "AdminOnly")]   // class-level — applies to all actions including EmailStats
public class AdminController(...) : Controller
```

### Modern Card Shell
**Source:** `EuphoriaInn.Service/Views/Admin/Quests.cshtml` lines 10-17
**Apply to:** `EmailStats.cshtml`
```html
<div class="card modern-card">
    <div class="card-header modern-card-header">
        <h2 class="mb-0">
            <i class="fas fa-[icon] text-[color] me-2"></i>
            [Page Title]
        </h2>
    </div>
    <div class="card-body modern-card-body">
        <!-- content -->
    </div>
</div>
```

### IOptions<EmailSettings> Binding
**Source:** Already registered in `EuphoriaInn.Domain/Extensions/` via `AddDomainServices`; inject as `IOptions<EmailSettings>` in the primary constructor — same pattern used by any domain service that reads email config. No new DI registration needed.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| (none) | — | — | All 6 files have close analogs or are self-referential extensions |

---

## Metadata

**Analog search scope:** `EuphoriaInn.Service/Controllers/`, `EuphoriaInn.Service/ViewModels/`, `EuphoriaInn.Service/Views/`, `EuphoriaInn.Domain/Models/`
**Files scanned:** 8 (AdminController.cs, Program.cs, EmailSettings.cs, UserManagementViewModel.cs, Quests.cshtml, _Layout.cshtml, appsettings.json, plus Glob listings)
**Pattern extraction date:** 2026-06-26
