# Stack Research — Omphalos Integration

**Researched:** 2026-06-18
**Scope:** Net-new stack additions for HMAC SSO, admin settings storage, and cross-app HTTP calls.

---

## Important: Both Apps Are Already on .NET 10

The `.csproj` files show `<TargetFramework>net10.0</TargetFramework>` in all four Quest Board projects and all four Omphalos projects. CLAUDE.md says "ASP.NET Core 8" but the actual code has been upgraded. This matters for one item (see HTTP Client section). All BCL and ASP.NET Core APIs referenced below are available on .NET 10.

---

## HMAC Signing (Quest Board)

**Verdict: No NuGet package needed. `HMACSHA256` is in the BCL.**

`System.Security.Cryptography.HMACSHA256` has been in the .NET BCL since .NET 1.1 and is part of `System.Security.Cryptography`, which ships in the runtime itself — not a separate NuGet package. On .NET 10 it is in `System.Runtime` (the umbrella pack that all SDK projects reference implicitly).

Quest Board's Domain project already references `System.Security.Cryptography.Xml` (version 10.0.7 in `EuphoriaInn.Domain.csproj`) — but that package is for XML digital signature operations (the `SignedXml` class). **HMACSHA256 is not from that package.** It is from the core runtime. The existing `System.Security.Cryptography.Xml` package is unrelated and not needed for HMAC signing; if it has no other use it could be removed, but that is not a concern for this milestone.

**Implementation location:** A new `OmphalosDeepLinkService` in `EuphoriaInn.Domain/Services/` is the correct home. It takes the shared secret (loaded from the DB-backed admin settings) and produces a signed URL.

**Token design recommendation:** The signed payload should be a URL query string containing `username`, `questId` (optional), `exp` (Unix epoch seconds, 5-minute window), and `sig` (hex-encoded HMACSHA256 of the canonical payload string using the shared secret). This is a MAC over a string, not a full JWT — simpler, no library needed, no key-wrapping overhead.

```csharp
// All in BCL — zero packages
using System.Security.Cryptography;
using System.Text;

var key = Encoding.UTF8.GetBytes(sharedSecret);
var payload = $"username={username}&questId={questId}&exp={exp}";
var sig = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)));
```

`HMACSHA256.HashData` (static, allocation-free) is available from .NET 6+, confirmed available on .NET 10. Confidence: HIGH.

---

## Token Validation (Omphalos)

**Verdict: Same BCL crypto stack. No new NuGet package needed. One structural addition required.**

Omphalos already uses `System.Security.Cryptography` implicitly (it is part of the runtime) and already has `Microsoft.IdentityModel.Tokens` 8.19.1 and `System.IdentityModel.Tokens.Jwt` 8.19.1 in `Omphalos.Services.csproj` for its existing JWT auth. The HMAC validation for Quest Board deep links does **not** require `Microsoft.IdentityModel` — it is a raw HMAC-over-string check, same BCL call:

```csharp
// Validation in Omphalos SSO endpoint
var expectedSig = Convert.ToHexString(HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payload)));
var valid = CryptographicOperations.FixedTimeEquals(
    Convert.FromHexString(receivedSig),
    Convert.FromHexString(expectedSig));
```

`CryptographicOperations.FixedTimeEquals` (BCL since .NET Core 2.1) prevents timing attacks on the comparison. Confidence: HIGH.

**What needs to be added to Omphalos:**

1. A new `GET /api/auth/sso` endpoint in `AuthEndpoints.cs`. It receives the signed token parameters from Quest Board via query string, validates the HMAC, auto-provisions a user if needed, and issues the standard `omphalos_token` JWT cookie then redirects.
2. The shared secret must be injected via configuration (`QuestBoardSecret` env var) — **not stored in Omphalos's DB**. The constraint from PROJECT.md is "env var for Omphalos."
3. The SSO endpoint must be `AllowAnonymous()` — it performs its own HMAC authentication, not cookie auth.

**No new NuGet packages required in Omphalos.**

---

## Admin Settings Storage (Quest Board)

**Verdict: Plain EF Core entity. No standard package exists or is needed.**

There is no NuGet package for "DB-backed key-value config" that is worth pulling in. The existing ASP.NET Core `IConfiguration` system is file/env-var backed; making it DB-backed requires either a custom `IConfigurationProvider` (complex, loaded at startup before DI is ready) or a plain service that reads a settings table on demand.

**The correct pattern for this use case is a plain EF Core entity + service**, consistent with how the rest of Quest Board's configuration works (typed `EmailSettings` record bound from `IOptions<EmailSettings>`).

**Proposed structure:**

```
EuphoriaInn.Repository/Entities/AppSettingEntity.cs   — { Key (PK, string), Value (string) }
EuphoriaInn.Domain/Models/AppSetting.cs               — { Key, Value }
EuphoriaInn.Domain/Interfaces/IAppSettingService.cs   — GetAsync(key), SetAsync(key, value)
EuphoriaInn.Domain/Services/AppSettingService.cs      — implementation
```

The `AppSettingEntity` table acts as a key-value store. Two keys are needed for this milestone: `Omphalos:Url` and `Omphalos:SharedSecret`. The service exposes typed helpers (`GetOmphalosUrlAsync`, `GetOmphalosSecretAsync`) wrapping the generic `GetAsync`.

**Why not `IConfiguration` custom provider?** It initializes before DI and requires a database connection at startup, which causes issues if the DB is not yet migrated. The service approach is readable at any point in a request, consistent with how `EmailService` reads `EmailSettings`, and lazily loaded (avoids startup brittleness).

**Why not a dictionary-in-JSON column?** The table approach lets migrations add new keys with defaults; a JSON column requires manual JSON manipulation and loses EF change tracking clarity.

**Migration:** A new EF Core migration adds `AppSettings` table. No seed data needed — the admin configures values via the new Settings page. The app must gracefully handle missing keys (Omphalos URL absent = hide the "Open DM Tool" link).

---

## HTTP Client (Quest Board to Omphalos)

**Verdict: Use `IHttpClientFactory` via `AddHttpClient()`. One line in Program.cs. No new package.**

`System.Net.Http.IHttpClientFactory` ships in `Microsoft.Extensions.Http`, which is included in the `Microsoft.NET.Sdk.Web` SDK (i.e., in `EuphoriaInn.Service`). The Quest Board service project targets `net10.0` with `Sdk="Microsoft.NET.Sdk.Web"`. `AddHttpClient()` is available without any additional NuGet reference.

Currently Quest Board has **no** `AddHttpClient()` call in `Program.cs` and no `IHttpClientFactory` injection anywhere in production code (confirmed by grep — zero matches). This means the DI registration is missing — it must be added.

**Registration (one line in Program.cs):**

```csharp
builder.Services.AddHttpClient("omphalos");
```

Or a typed client `OmphalosHttpClient` that wraps `IHttpClientFactory.CreateClient("omphalos")` and configures the base URL per-call from `IAppSettingService`.

**Why `IHttpClientFactory` over `new HttpClient()`?**
- `new HttpClient()` per-request causes socket exhaustion (well-documented .NET anti-pattern).
- `IHttpClientFactory` manages handler lifetimes (default 2-minute DNS refresh) and is the standard ASP.NET Core recommendation since .NET Core 2.1.
- Named or typed clients allow the base URL to be set from DB settings without threading issues.

**Scope of use in Milestone 3:** The actual SSO flow is a **browser redirect** — Quest Board generates a signed URL and redirects the browser to Omphalos (`RedirectResult`). No server-to-server HTTP call is required for the SSO itself. `IHttpClientFactory` is registered now as the foundation for future bidirectional calls (Omphalos → Quest Board is listed in PROJECT.md as a future milestone feature). If nothing calls it in M3 it costs nothing at runtime.

---

## Omphalos JWT/CORS Changes

**Verdict: CORS needs no changes for Milestone 3. JWT config needs no changes. A new SSO endpoint must be added.**

### CORS

Current config in `Program.cs`:

```csharp
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
```

The deep-link SSO flow is a **browser redirect**, not an XHR/fetch call. The browser navigates to Omphalos's SSO URL directly (`window.location.href = signedUrl`). Browser navigations (302 redirect → GET) are **not subject to CORS preflight**. Therefore, the SSO endpoint itself does not require CORS changes.

After the SSO endpoint issues the JWT cookie and redirects to the React SPA, the React app makes subsequent XHR calls. Those already work under the existing `AllowedOrigins` config as long as Quest Board's origin is listed there. Quest Board must be added to `AllowedOrigins` in Omphalos's config — not a code change, a deployment config change.

**No CORS code changes required for Milestone 3.**

### JWT Config

The existing JWT validation in `Program.cs` reads `omphalos_token` from a cookie and validates issuer/audience/lifetime/signing key. The SSO endpoint will **issue** a new `omphalos_token` cookie using the same `GenerateToken(user)` path already in `AuthService`. No changes to `TokenValidationParameters` or the `OnMessageReceived` event are needed.

**SameSite cookie concern:** `AuthEndpoints.cs` sets `SameSite = SameSiteMode.Lax` on the `omphalos_token` cookie. `Lax` allows the cookie to be sent on top-level GET navigations (which is what a redirect produces), and allows it to be included in subsequent same-site requests. The SSO flow navigates the browser to Omphalos, Omphalos sets the cookie, then Omphalos serves the SPA. No cross-site cookie sending is required. `Lax` is correct. No change needed.

### What does need to change in Omphalos

1. **New `GET /api/auth/sso` endpoint** in `AuthEndpoints.cs` (or a new `SsoEndpoints.cs`):
   - Reads `username`, `questId` (optional), `exp`, `sig` from query string
   - Recomputes HMAC using `QuestBoardSecret` config value
   - Validates expiry (reject if `exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds()`)
   - Finds user by `username`; if not found, creates one with `UserRole.Player` (auto-provision)
   - Issues `omphalos_token` cookie via existing `GenerateToken(user)` path in `AuthService`
   - Redirects to `/sessions/{questId}` or `/` if no questId

2. **New `QuestBoardSecret` config key** read in `Program.cs` (env var, not DB). Fail-fast if missing at startup (same pattern as `Jwt:Secret`).

3. **`GameSession` quest-board linking** — currently `GameSession.Id` is a `string` (user-defined title slug) and `SessionMetadata` has no `QuestBoardQuestId` field. A nullable `QuestBoardQuestId` int column is needed on `GameSession` to find/create the session for a given quest. This requires a new Omphalos migration.

---

## Summary: Net New Dependencies

| Package / API | Layer | Already Present | Action Needed |
|---|---|---|---|
| `System.Security.Cryptography.HMACSHA256` | Quest Board: Domain | Yes — BCL, part of runtime | None — use directly |
| `System.Security.Cryptography.HMACSHA256` | Omphalos: Services | Yes — BCL, part of runtime | None — use directly |
| `System.Security.Cryptography.Xml` (NuGet) | Quest Board: Domain | Yes (v10.0.7) | Unrelated to HMAC; no change |
| `Microsoft.Extensions.Http` (`IHttpClientFactory`) | Quest Board: Service | Included in `Microsoft.NET.Sdk.Web` | Add `builder.Services.AddHttpClient()` to `Program.cs` |
| `Microsoft.IdentityModel.Tokens` | Omphalos: Services | Yes (v8.19.1) | No change — HMAC validation uses BCL only |
| `AppSettingEntity` (EF Core table) | Quest Board: Repository | No | New entity + migration + service (Domain + Repository) |
| `QuestBoardSecret` config key | Omphalos | No | Add to env vars and `Program.cs` startup validation |
| `QuestBoardQuestId` column on `GameSession` | Omphalos: Repository | No | New nullable int column + Omphalos EF migration |
| CORS policy code change | Omphalos | N/A | None required; Quest Board origin added to `AllowedOrigins` config only |
| New `/api/auth/sso` endpoint | Omphalos: Web | No | New endpoint in `AuthEndpoints.cs` |

**Net-new NuGet packages: zero.** All cryptographic primitives are BCL. `IHttpClientFactory` is in the SDK. The only code additions are: a new EF entity + migration in Quest Board, a new service + interface in Quest Board Domain, one `AddHttpClient()` line in Quest Board's `Program.cs`, a new Omphalos SSO endpoint, a config key in Omphalos's env vars, and a new nullable column + migration in Omphalos.
