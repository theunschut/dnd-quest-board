# Technology Stack

**Project:** D&D Quest Board — Milestone 2: Refactor + Feature Expansion
**Researched:** 2026-04-15
**Scope:** Best practices for the existing ASP.NET Core 8 MVC stack; no framework replacement

---

## Current Stack Assessment

The stack itself is sound. The problems are in how the layers are wired together and what libraries
are used for email. The NuGet package version mix (EF Core 9 on .NET 8, Identity 8) is functional
but inconsistent — addressed in recommendations below.

---

## Recommendations

### 1. Dependency Direction — Fix the Domain→Repository Violation

**Confidence: HIGH** (verified against official Microsoft architecture guidance and ardalis/CleanArchitecture reference template)

**Current state (wrong):**
```
EuphoriaInn.Domain.csproj
  <ProjectReference Include="..\EuphoriaInn.Repository\..." />
```
Domain knows about Repository entities, which makes the dependency arrow point outward from the core.

**Correct state:**
```
Service    → Domain  (Service references Domain)
Repository → Domain  (Repository references Domain for interfaces)
Domain     → nothing (Domain has zero project references)
```

**What this means concretely:**

- Repository interfaces (`IQuestRepository`, `IUserRepository`, etc.) must live in `EuphoriaInn.Domain/Interfaces/` — they already do, which is correct.
- `EuphoriaInn.Domain.csproj` must remove `<ProjectReference>` to `EuphoriaInn.Repository`.
- `EuphoriaInn.Repository.csproj` must add `<ProjectReference>` to `EuphoriaInn.Domain` — Repository implements Domain interfaces.
- `EntityProfile.cs` currently lives in Domain and maps Repository entities (`*Entity` types) to Domain models. After the fix it must move to Repository, because Domain cannot reference entity types.
- `BaseService<TModel, TEntity>` in Domain uses the generic `TEntity` type parameter that comes from Repository — this coupling must be broken. Services in Domain should operate on Domain models only; mapping is the Repository layer's responsibility.

**Why it matters:** As long as Domain references Repository, you cannot test Domain services in isolation (every test drags in EF Core), and you cannot swap the persistence layer without touching Domain code.

**References:**
- [Microsoft ISE Clean Architecture boilerplate](https://devblogs.microsoft.com/ise/next-level-clean-architecture-boilerplate/)
- [ardalis/CleanArchitecture reference template](https://github.com/ardalis/CleanArchitecture)
- [Microsoft common web app architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

---

### 2. Options Pattern — Replace IConfiguration.GetSection() with IOptions\<T\>

**Confidence: HIGH** (verified against official Microsoft docs, updated March 2026)

**Current state (wrong):**

`EmailService` reads configuration by calling `configuration.GetSection("EmailSettings")["SmtpServer"]`
on every method invocation — six string lookups per email send, repeated across two methods, with
no validation.

**Correct approach — IOptions\<T\>:**

```csharp
// EmailSettings.cs (new file in Domain or Service)
public class EmailSettings
{
    public const string Section = "EmailSettings";

    [Required] public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    [Required] public string SmtpUsername { get; set; } = string.Empty;
    [Required] public string SmtpPassword { get; set; } = string.Empty;
    [Required] public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "D&D Quest Board";
}

// Program.cs registration
builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();  // fails fast at startup if config is missing

// EmailService.cs — inject IOptions<EmailSettings>
public class EmailService(IOptions<EmailSettings> emailOptions, ILogger<EmailService> logger)
{
    private readonly EmailSettings _settings = emailOptions.Value;
    // ...
}
```

**Which IOptions interface to use:**

| Interface | Lifetime | Reloads at runtime | Use when |
|-----------|----------|-------------------|----------|
| `IOptions<T>` | Singleton | No | Default. Static config like SMTP credentials. |
| `IOptionsSnapshot<T>` | Scoped | Yes (per request) | Config that changes, consumed by scoped/transient services |
| `IOptionsMonitor<T>` | Singleton | Yes (continuous) | Singleton services that need live reloads |

For `EmailService`, `IOptions<T>` is correct — SMTP credentials do not change at runtime.

**Apply the same pattern to `SecurityConfiguration`** — but per the project requirements, `SecurityConfiguration` is dead code that should be removed entirely. Do not convert it; delete it.

**References:**
- [Options pattern in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-8.0)
- [Options pattern in .NET — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)

---

### 3. Identity Lockout Configuration

**Confidence: HIGH** (verified against official Microsoft LockoutOptions API docs)

**Current state:** The project does not pass `lockoutOnFailure: true` to `PasswordSignInAsync`, so the
`AccessFailedCount` column is never incremented and lockout never triggers regardless of how many
failed attempts occur.

**What needs to change:**

**Step 1 — Configure LockoutOptions in Program.cs (or via AddIdentity lambda):**

```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    // Lockout
    options.Lockout.AllowedForNewUsers = true;       // default: true
    options.Lockout.MaxFailedAccessAttempts = 5;     // default: 5 — requirement says 5
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // default: 5 min; requirement says 15

    // Password
    options.Password.RequiredLength = 8;             // default: 6 — requirement says 8
});
```

**Step 2 — Enable lockout at the call site:**

```csharp
// AccountController.cs or wherever PasswordSignInAsync is called
var result = await _signInManager.PasswordSignInAsync(
    email,
    password,
    isPersistent: false,
    lockoutOnFailure: true);   // THIS is what wires lockout to failed attempts
```

Without `lockoutOnFailure: true`, the `LockoutOptions` configuration has no effect.

**LockoutOptions defaults (official):**

| Property | Default | Requirement |
|----------|---------|-------------|
| `AllowedForNewUsers` | `true` | `true` |
| `MaxFailedAccessAttempts` | `5` | `5` |
| `DefaultLockoutTimeSpan` | `5 minutes` | `15 minutes` |

Only `DefaultLockoutTimeSpan` requires a non-default value. `MaxFailedAccessAttempts` is already 5
by default — still set it explicitly for clarity and auditability.

**References:**
- [LockoutOptions Class — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.lockoutoptions?view=aspnetcore-8.0)
- [Configure ASP.NET Core Identity — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-9.0)

---

### 4. Image Cropping — Client-Side Crop with Cropper.js, Base64 Upload

**Confidence: MEDIUM** (Cropper.js well-established; version confirmed from official site; pattern verified from multiple sources)

**Recommendation: Cropper.js 2.x — client-side crop, base64 POST to server**

This is the right approach for this codebase because:
- No server-side image manipulation library needed (no SixLabors.ImageSharp, no System.Drawing)
- Works with the existing vanilla JS + Bootstrap 5 stack — no new frontend framework
- Stays consistent with the project's no-npm / CDN-loaded philosophy

**Cropper.js version situation:**

The official Cropper.js site (fengyuanchen.github.io/cropperjs/) currently shows **v2.1.1**, which
is a major rewrite with a different API. However, most existing ASP.NET Core tutorials and CDN
packages reference **v1.5.13** (still maintained, still widely deployed). Version 1.5.13 has the
more documented integration pattern for MVC.

**Recommendation: Use Cropper.js 1.6.2** — the last stable 1.x release, available on cdnjs, with
the battle-tested API. The 2.x rewrite has limited documentation for the specific "crop then upload
base64" pattern this feature needs. Re-evaluate 2.x when documentation matures.

**Integration pattern:**

```html
<!-- In view — add to _Layout or page-specific scripts section -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.2/cropper.min.css" />
<script src="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.2/cropper.min.js"></script>
```

```javascript
// JS: initialize cropper when user selects a file
const input = document.getElementById('avatarInput');
input.addEventListener('change', function (e) {
    const file = e.target.files[0];
    const reader = new FileReader();
    reader.onload = function (evt) {
        document.getElementById('cropImage').src = evt.target.result;
        // open Bootstrap modal, then init cropper on the <img>
        const cropper = new Cropper(document.getElementById('cropImage'), {
            aspectRatio: 1,
            viewMode: 1
        });
        document.getElementById('confirmCrop').addEventListener('click', function () {
            const canvas = cropper.getCroppedCanvas({ width: 256, height: 256 });
            document.getElementById('croppedImageData').value = canvas.toDataURL('image/jpeg', 0.85);
        });
    };
    reader.readAsDataURL(file);
});
```

```csharp
// Controller action — receive base64, decode, save
[HttpPost]
public async Task<IActionResult> UpdateAvatar(string croppedImageData)
{
    // Strip "data:image/jpeg;base64," prefix
    var base64 = croppedImageData.Split(',')[1];
    var bytes = Convert.FromBase64String(base64);
    var fileName = $"{Guid.NewGuid()}.jpg";
    var path = Path.Combine(_env.WebRootPath, "uploads", "avatars", fileName);
    await System.IO.File.WriteAllBytesAsync(path, bytes);
    // save fileName to user record
}
```

**No server-side image library needed** — the crop and resize happen entirely in the browser via
HTML5 Canvas before the bytes are sent. The server just stores the already-cropped JPEG.

**File size:** A 256×256 JPEG at 0.85 quality is typically 15–40 KB — well within normal form POST
limits. No changes to Kestrel `MaxRequestBodySize` required.

**References:**
- [Cropper.js official site](https://fengyuanchen.github.io/cropperjs/)
- [Image Cropping with Cropper.js in ASP.NET Core — Medium](https://karaoz-onr.medium.com/image-cropping-and-uploading-to-server-with-cropper-js-in-asp-net-core-56afb0ed1b6f)
- [CropperUploadDemo — GitHub](https://github.com/lampo1024/CropperUploadDemo)

---

### 5. Email Library — Migrate from System.Net.Mail.SmtpClient to MailKit

**Confidence: HIGH** (Microsoft explicitly marks SmtpClient as not recommended; MailKit is Microsoft's stated alternative)

**Current state (problematic):**

`EmailService.cs` uses `System.Net.Mail.SmtpClient`, which Microsoft documents as:

> "We don't recommend using SmtpClient for new development because SmtpClient doesn't support many modern protocols."

Additionally, the current implementation:
- Re-reads configuration on every call (resolved by IOptions pattern above)
- Uses deprecated `SmtpClient` with known async issues (async method wraps blocking I/O)
- Duplicates connection setup across two methods

**Recommended replacement: MailKit 4.15.1**

MailKit is Microsoft's officially stated alternative. It:
- Has proper async SMTP (no thread pool blocking)
- Supports OAUTH2, DKIM, and modern auth requirements (relevant for Gmail)
- Is the standard in the .NET ecosystem (80M+ NuGet downloads)
- Works identically against Gmail's SMTP with App Passwords

**Package:** `MailKit` 4.15.1 — install in `EuphoriaInn.Domain` (where EmailService lives currently)

Note: After the dependency direction fix, `EmailService` should move to `EuphoriaInn.Service` or
stay in Domain depending on whether it needs repository access. It does not — it is a pure
outbound notification service — so it can stay in Domain or move to Service. Either is acceptable;
Service is the more conventional placement for infrastructure concerns.

**Replacement implementation pattern:**

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class EmailService(IOptions<EmailSettings> emailOptions, ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = emailOptions.Value;

    private async Task SendAsync(MimeMessage message)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendQuestFinalizedEmailAsync(string toEmail, string playerName,
        string questTitle, string dmName, DateTime questDate)
    {
        if (string.IsNullOrEmpty(_settings.SmtpUsername)) { /* log and return */ return; }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(playerName, toEmail));
        message.Subject = $"Quest Finalized: {questTitle}";
        message.Body = new TextPart("plain") { Text = BuildFinalizedBody(playerName, questTitle, dmName, questDate) };

        try { await SendAsync(message); }
        catch (Exception ex) { logger.LogError(ex, "Failed to send finalized email for {Quest}", questTitle); }
    }
}
```

**Migration effort:** Low. The email body strings are unchanged. The only code change is swapping
`System.Net.Mail` types for MailKit/MimeKit types and consolidating connection setup into one
private method.

**References:**
- [Microsoft SmtpClient docs — "not recommended"](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-9.0)
- [MailKit GitHub](https://github.com/jstedfast/MailKit)
- [MailKit NuGet — 4.15.1](https://www.nuget.org/packages/mailkit/)
- [SmtpClient is obsolete — Jonathan Crozier](https://jonathancrozier.com/blog/smtpclient-is-obsolete-the-new-way-to-send-emails-from-your-net-app)

---

## Package Version Summary

Current packages that need attention:

| Package | Current | Recommended | Reason |
|---------|---------|-------------|--------|
| `System.Net.Mail.SmtpClient` (built-in) | n/a | Remove — use MailKit | Marked not recommended by Microsoft |
| `MailKit` | not installed | 4.15.1 | Official SmtpClient replacement |
| `Microsoft.AspNetCore.Identity` | 2.3.1 (Domain) | Remove from Domain | After dependency fix, Domain won't need this directly |
| `Microsoft.Extensions.Configuration.Binder` | 9.0.6 (Domain) | Remove from Domain | Replace IConfiguration with IOptions<T>; binder not needed in Domain |
| EF Core (`Microsoft.EntityFrameworkCore`) | 9.0.6 | No change | EF Core 9 on .NET 8 is supported and correct |
| Identity EF (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`) | 8.0.11 | No change | Stays in Repository |
| `AutoMapper` | 14.0.0 | No change | Current stable |

**Note on EF Core 9 + .NET 8:** This is intentional and supported. EF Core 9 targets .NET 8+ as its
minimum runtime. The version mismatch vs. `Identity.EntityFrameworkCore` 8.0.11 is cosmetically
inconsistent but functionally fine — they target different package lines.

---

## What Stays the Same

- ASP.NET Core 8 MVC — no change
- SQL Server + EF Core 9 — no change
- ASP.NET Core Identity 8 — no change
- AutoMapper 14 — no change
- Bootstrap 5.3 / jQuery 3.6 / Font Awesome 6.4 via CDN — no change
- Docker deployment — no change

---

## Installation

```bash
# Add MailKit to the project that hosts EmailService (Domain currently; Service after refactor)
dotnet add EuphoriaInn.Domain package MailKit --version 4.15.1

# OR if EmailService moves to Service project:
dotnet add EuphoriaInn.Service package MailKit --version 4.15.1
```

No additional packages are required for:
- IOptions<T> — in `Microsoft.Extensions.Options`, included transitively via ASP.NET Core
- Identity lockout — already in `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- Cropper.js — loaded via CDN, no NuGet package

---

## Sources

- [ardalis/CleanArchitecture](https://github.com/ardalis/CleanArchitecture)
- [Microsoft ISE Clean Architecture Boilerplate](https://devblogs.microsoft.com/ise/next-level-clean-architecture-boilerplate/)
- [Options pattern in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-8.0)
- [LockoutOptions — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.lockoutoptions?view=aspnetcore-8.0)
- [Configure ASP.NET Core Identity — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration)
- [System.Net.Mail.SmtpClient — Microsoft Learn (not recommended notice)](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-9.0)
- [MailKit on NuGet — 4.15.1](https://www.nuget.org/packages/mailkit/)
- [MailKit GitHub](https://github.com/jstedfast/MailKit)
- [Cropper.js official site](https://fengyuanchen.github.io/cropperjs/)
- [SmtpClient is obsolete — Jonathan Crozier](https://jonathancrozier.com/blog/smtpclient-is-obsolete-the-new-way-to-send-emails-from-your-net-app)

---

*Research: 2026-04-15*
