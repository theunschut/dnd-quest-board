# Research Summary - Omphalos Integration (Milestone 3)

**Synthesized:** 2026-06-18
**Sources:** STACK.md, FEATURES.md, ARCHITECTURE.md, PITFALLS.md, PROJECT.md

---

## Executive Summary

Milestone 3 connects two independent, already-functional apps (Quest Board on ASP.NET Core + SQL Server; Omphalos on .NET 10 Minimal API + PostgreSQL) via a browser-redirect SSO flow using a short-lived HMAC-SHA256 signed token. Neither app calls the other API directly in this milestone -- Quest Board generates a signed URL and issues a browser redirect; Omphalos receives and validates that redirect, auto-provisions the DM account on first use, finds or creates the quest session, and issues its JWT cookie. The entire cryptographic layer is BCL (System.Security.Cryptography.HMACSHA256) -- zero net-new NuGet packages required in either repo.

The work breaks cleanly into three sequential Quest Board phases and one parallel Omphalos phase. Phase 10 (Admin Settings) is the sole blocker: it supplies IAdminSettingService which both subsequent Quest Board phases depend on at compile time. Phase 12 (Omphalos SSO) has no code dependency on Quest Board and can begin the moment the token format contract is written down. The two development streams converge only at end-to-end integration testing.

The highest-risk items are all pre-implementation decisions that cannot be deferred: the canonical MAC message format (which fields, in what order, with what encoding), the username normalisation convention, and the deployment topology (same eTLD+1 vs different domains). These three decisions must be locked before any implementation begins; getting them wrong after the fact requires coordinated changes in both repos simultaneously.

---

## Stack Additions

**Net-new NuGet packages: zero.**

| Item | Location | Status | Action |
|------|----------|--------|--------|
| HMACSHA256 / CryptographicOperations | Both repos | Already in BCL runtime | Use directly |
| IHttpClientFactory (Microsoft.Extensions.Http) | Quest Board Service | Included in Microsoft.NET.Sdk.Web | Add AddHttpClient in Program.cs |
| AdminSettingEntity EF table | Quest Board Repository | Does not exist | New EF entity + migration AddAdminSettings |
| QuestBoard:Secret config key | Omphalos | Does not exist | Add env var QuestBoard__Secret; fail-fast on startup |
| ExternalQuestId column on GameSession | Omphalos Repository | Does not exist | New nullable int? column + Omphalos EF migration |
| CORS code changes | Omphalos | N/A | None -- redirect is a browser navigation, not a fetch |

Existing packages unchanged: Microsoft.IdentityModel.Tokens (Omphalos), System.Security.Cryptography.Xml (Quest Board Domain). Both present but unrelated to HMAC token path.

Note: CLAUDE.md says ASP.NET Core 8 but all Quest Board .csproj files target net10.0. Documentation is stale; nothing changes for this milestone.

---

## Feature Design Decisions

### Admin Settings Shape

Use a key-value EF entity (AdminSettingEntity: Key nvarchar(200) PK unique, Value nvarchar(max), UpdatedAt datetime2) rather than a typed two-column entity. The key-value shape avoids a new migration every time a settings key is added in a future milestone.

Two keys for this milestone: OmphalosUrl and OmphalosSharedSecret.

Register IAdminSettingService as Scoped, not Singleton -- the secret must be read from DB per-request so a settings change takes effect immediately without restart.

### Admin UI Secret Handling

Render the secret field as type="password". On GET, leave the field empty -- do not populate from DB. On POST, only overwrite the DB value if the submitted field is non-empty. Empty POST = keep existing value. This prevents accidental secret exposure and is the standard pattern for secret fields.

### Token Format (the cross-app contract)

```
MAC input (canonical, exact):  {username}|{questId}|{expiry}
Algorithm:                      HMAC-SHA256
Key encoding:                   UTF-8 bytes of the shared secret string
Signature encoding:             lowercase hex
Username normalisation:         lowercase both sides
Field encoding:                 URL-encode each field before concatenating
Expiry format:                  Unix timestamp, UTC seconds (never local time)
Token TTL:                      300 seconds (5 minutes)
Grace period on validation:     5-10 seconds (absorbs container clock drift)
```

Query string parameters: username, questId, questTitle, exp, sig.

questTitle is for session pre-fill only and is NOT in the MAC input. The MAC covers identity and routing (username|questId|expiry); presentation data is separate.

### Session Find-or-Create

Omphalos SSO endpoint performs find-or-create by ExternalQuestId (new nullable int? column on GameSession):

1. Query GameSession WHERE UserId = provisioned user AND ExternalQuestId = questId.
2. If found: redirect to that session.
3. If not found: create new session with Id = Guid.NewGuid().ToString(), Title = questTitle, ExternalQuestId = questId.

GameSession.Id is client-generated in the existing Omphalos codebase. The SSO endpoint must supply a valid unique ID on create -- use Guid.NewGuid().ToString().

### Auto-Provisioning Strategy

On first SSO, Omphalos creates a User with:
- Username = normalised (lowercase) username from token
- PasswordHash = BCrypt hash of a cryptographically random password (never usable directly)
- Role = mapped from Quest Board role in the token: DungeonMaster/Admin -> Omphalos Admin; Player -> Omphalos Player

The token payload must include the Quest Board role. Defaulting all provisioned users to Player silently strips DMs of Omphalos permissions.

---

## Architecture Overview

### New Files Per Phase

**Phase 10 -- Admin Settings (Quest Board)**

| File | Layer | Status |
|------|-------|--------|
| EuphoriaInn.Repository/Entities/AdminSettingEntity.cs | Repository | New |
| EuphoriaInn.Domain/Models/AdminSetting.cs | Domain | New |
| EuphoriaInn.Domain/Interfaces/IAdminSettingRepository.cs | Domain | New |
| EuphoriaInn.Domain/Interfaces/IAdminSettingService.cs | Domain | New |
| EuphoriaInn.Domain/Services/AdminSettingService.cs | Domain | New |
| EuphoriaInn.Repository/AdminSettingRepository.cs | Repository | New |
| EuphoriaInn.Service/Controllers/Admin/AdminController.cs | Service | Modified -- add Settings GET/POST |
| EuphoriaInn.Service/ViewModels/AdminViewModels/AdminSettingsViewModel.cs | Service | New |
| EuphoriaInn.Service/Views/Admin/Settings.cshtml | Service | New |
| EuphoriaInn.Repository/Entities/QuestBoardContext.cs | Repository | Modified -- add DbSet + unique index |
| EuphoriaInn.Repository/Automapper/EntityProfile.cs | Repository | Modified -- add mapping |
| EuphoriaInn.Repository/Extensions/ServiceExtensions.cs | Repository | Modified -- register repo |
| EuphoriaInn.Domain/Extensions/ServiceExtensions.cs | Domain | Modified -- register service |
| EF migration: AddAdminSettings | Repository/Migrations | New |

**Phase 11 -- Navigation + Token Generation (Quest Board)**

| File | Layer | Status |
|------|-------|--------|
| EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs | Domain | New |
| EuphoriaInn.Domain/Services/IntegrationTokenService.cs | Domain | New |
| EuphoriaInn.Service/ViewComponents/OmphalosNavItemViewComponent.cs | Service | New |
| EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml | Service | New |
| EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs | Service | Modified -- add LaunchOmphalos action |
| EuphoriaInn.Service/Views/Quest/Details.cshtml | Service | Modified -- conditional button |
| EuphoriaInn.Service/Views/Quest/Manage.cshtml | Service | Modified -- conditional button |
| EuphoriaInn.Service/Views/Shared/_Layout.cshtml | Service | Modified -- invoke OmphalosNavItem component |
| EuphoriaInn.Domain/Extensions/ServiceExtensions.cs | Domain | Modified -- register IntegrationTokenService |

**Phase 12 -- SSO Endpoint + Session Linking (Omphalos)**

| File | Layer | Status |
|------|-------|--------|
| Omphalos.Domain/Entities/GameSession.cs | Domain | Modified -- add int? ExternalQuestId |
| Omphalos.Domain/Interfaces/ISessionRepository.cs | Domain | Modified -- add GetByExternalQuestIdAsync |
| Omphalos.Domain/Interfaces/IAuthService.cs | Domain | Modified -- expose GenerateToken(User) |
| Omphalos.Domain/Interfaces/ISsoService.cs | Domain | New |
| Omphalos.Domain/DTOs/SsoRequest.cs | Domain | New |
| Omphalos.Domain/DTOs/SsoResult.cs | Domain | New |
| Omphalos.Repository/Configurations/GameSessionConfiguration.cs | Repository | Modified -- nullable column + index |
| Omphalos.Repository/Repositories/SessionRepository.cs | Repository | Modified -- implement GetByExternalQuestIdAsync |
| Omphalos.Services/Implementations/AuthService.cs | Services | Modified -- make GenerateToken public |
| Omphalos.Services/Implementations/SsoService.cs | Services | New |
| Omphalos.Web/Endpoints/SsoEndpoints.cs | Web | New |
| Omphalos.Web/Program.cs | Web | Modified -- register ISsoService; MapSsoEndpoints |
| Omphalos EF migration: AddExternalQuestIdToGameSession | Repository/Migrations | New |

### Key Architectural Patterns

**IIntegrationTokenService in Domain, not inline in controller.** HMAC generation reads IAdminSettingService -- that is business logic, not presentation logic. Thin controller principle is enforced throughout this codebase.

**View Component for navbar, not base controller inheritance.** _Layout.cshtml is shared across all controllers. A ViewComponent injects IAdminSettingService directly and fires once per layout render. A base controller approach fires a DB read on every action across every controller.

**ViewBag for quest-page Omphalos flag, not new ViewModel wrapper.** Details and Manage already use ViewBag for five other flags. Adding ViewBag.OmphalosConfigured is consistent with the existing pattern; wrapping the raw Quest domain model is a scope-expanding refactor outside this milestone.

**Asymmetric secret storage is by design.** Quest Board stores the secret in the DB (editable via Admin UI). Omphalos reads it from env var (fail-fast on startup). This matches how each app already manages its own secrets.

---

## Build Order

```
Phase 10: Admin Settings (Quest Board)
    No dependencies -- start immediately.
    |
    Phase 10 merged + auto-migration deployed
    |
    Phase 11: Navigation + Token Generation (Quest Board)
        Hard compile-time dependency on IAdminSettingService from Phase 10.
        Cannot start until Phase 10 is merged.
    |
    Both Phase 11 AND Phase 12 complete
    |
    End-to-end integration test (both containers, shared secret configured)

Phase 12: SSO Endpoint + Session Linking (Omphalos)   [PARALLEL to Phase 11]
    No compile-time dependency on Quest Board.
    Can start immediately after the token format contract is agreed.
```

Practical sequence:
1. Lock the token format contract in writing (before any code)
2. Phase 10 Quest Board Admin Settings -- unblocked
3. Phase 12 Omphalos SSO -- begins in parallel after contract is agreed
4. Phase 11 Quest Board token generation + nav -- begins after Phase 10 merges
5. End-to-end test -- requires Phase 11 and Phase 12 both complete

---

## Watch Out For

**1. Token format contract must be agreed before any implementation**
The MAC canonical string, field encoding (URL-encode each component), signature encoding (lowercase hex), and username normalisation (lowercase both sides) must all be written down as a shared spec before either IntegrationTokenService.cs or SsoService.cs is started. A mismatch discovered after both sides are implemented requires coordinated edits in two repos. Write the spec as a block comment at the top of both files.

**2. Username normalisation -- define once, apply everywhere (Phases 11 + 12)**
Quest Board Identity UserName is mixed-case; Omphalos PostgreSQL comparisons are case-sensitive by default. If Quest Board sends "Theun" and Omphalos has stored "theun", auto-provision creates a duplicate and the unique index throws. Normalise to lowercase in the token payload (Phase 11) and before lookup in SsoService (Phase 12). This must be in the token format spec.

**3. Secret field POST -- empty = keep existing, never overwrite with blank (Phase 10)**
The GET must render the secret field empty. The POST must only call SetValueAsync for the secret if the submitted value is non-empty. Getting this wrong means the first save-without-changing-the-secret silently wipes the secret, breaking all subsequent SSO redirects with no obvious error.

**4. Omphalos missing env var -- null HMAC key must fail-fast (Phase 12)**
Follow the existing Omphalos pattern: builder.Configuration["QuestBoard:Secret"] ?? throw. If the code falls back to null, HMACSHA256 with a zero-length key still produces a valid HMAC -- meaning any forged token computed with the same empty key passes validation. Add QuestBoard__Secret=change-me to .env.example.

**5. SameSite cookie and deployment topology (Phase 12)**
omphalos_token uses SameSite=Lax. This works for redirects between subdomains of the same eTLD+1. If the apps are on different eTLD+1 domains, the cookie is silently blocked. The Secure = true TODO in AuthEndpoints.cs must become a hard requirement -- SameSite=None (needed for cross-eTLD+1) requires Secure=true. Verify deployment topology before Phase 12 begins.

**6. Review the Omphalos migration before applying (Phase 12)**
GameSession has a jsonb column (SessionLog). Npgsql EF Core migrations have historically emitted spurious AlterColumn for jsonb columns when regenerating the model snapshot. After running the migration add command, read the generated Up() method and confirm it contains only AddColumn for external_quest_id. Remove any spurious AlterColumn on SessionLog before applying.

**7. Include Quest Board role in token payload (Phase 12)**
Defaulting all auto-provisioned users to Omphalos UserRole.Player silently strips DMs of DM-only Omphalos permissions. The token must carry the Quest Board role; SsoService maps DungeonMaster/Admin to Omphalos Admin. Define the mapping in the token spec.

---

## Open Questions (Policy Decisions Needed)

These cannot be resolved by research. They require a product or operator decision before implementation begins.

**1. Replay protection: accept residual risk or implement a nonce store?**
A 5-minute token with no nonce store means a captured redirect URL is reusable for up to 5 minutes. For a trusted self-hosted group app this is a known, acceptable risk. A scoped ConcurrentDictionary on Quest Board (no DB writes, cleared on expiry) covers it if nonce tracking is required. Pick one before Phase 11 is specced.

**2. Omphalos unreachable: block the redirect with an inline error, or always redirect and accept a browser dead-end?**
A server-side pre-flight GET {omphalosUrl}/health (2-3s timeout) before issuing the redirect gives the DM an inline error on the Quest Board page. This adds one HTTP call on every "Open Session Notes" click. Alternative: always redirect and accept the browser error. Decision needed.

**3. Integration button when Omphalos is not configured: disabled with tooltip, or hidden entirely?**
Disabled gives admins a visual cue on a freshly deployed instance; hidden keeps the UI clean. Both are defensible. Decision needed before the Settings and Quest views are built.

**4. Quest Board origin in Omphalos AllowedOrigins: Milestone 3 deployment requirement or defer to the bidirectional milestone?**
The SSO redirect flow needs no CORS change. But any future JavaScript API calls will require Quest Board URL in AllowedOrigins. Configure now (deployment concern, not code) or track for the next milestone?

**5. Store ExternalSessionId back on Quest Board QuestEntity this milestone?**
Enables a session-exists badge on the Quest Manage page. Requires a nullable string? ExternalSessionId column and a Quest Board migration. FEATURES.md calls it a differentiator; ARCHITECTURE.md treats it as optional. Include in Milestone 3 or defer?

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Stack (no new packages) | HIGH | Both repos directly inspected; BCL APIs verified |
| Token format design | HIGH | BCL crypto is stable; format is a design choice, not a discovery |
| Quest Board architecture | HIGH | Existing patterns are consistent; new files follow established conventions exactly |
| Omphalos architecture | HIGH | Omphalos source directly inspected; flat structure is straightforward to extend |
| Security pitfalls | HIGH | All pitfalls grounded in actual code and well-documented SSO patterns |
| Deployment topology (SameSite cookie) | MEDIUM | Depends on how the operator deploys; cannot be resolved until deployment config is known |
| React SPA auth guard behaviour | MEDIUM | Auth guard code not fully inspected; flagged as a Phase 12 frontend risk |

**Overall: HIGH confidence on the implementation path. MEDIUM on two deployment-environment assumptions that must be verified before Phase 12 ships.**
