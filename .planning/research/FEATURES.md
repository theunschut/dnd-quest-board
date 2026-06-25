# Features Research — Omphalos Integration

**Researched:** 2026-06-18
**Confidence:** HIGH — based entirely on direct source-code inspection of both repos

---

## Admin Settings Page

### Table stakes

- Single form page at `/Admin/Settings` protected by `"AdminOnly"` policy (same as existing AdminController)
- Two fields: Omphalos Base URL (text/url input) and Shared Secret (password input with show/hide toggle)
- GET loads current values from DB; POST saves and redirects back with success flash via TempData
- AntiForgery token on the POST (matches all existing admin form actions)
- Secret field rendered as `<input type="password">` so it is not visible in the browser by default
- URL field rendered as `<input type="url">` so mobile keyboards offer `.com` and the browser can warn on invalid format
- Both fields nullable — if URL is empty the integration is treated as unconfigured; no validation error required for blank
- If URL is set but secret is blank, that is a valid save (warn in UX but do not block)

### Differentiators

- "Reveal secret" eye-icon toggle on the secret field (purely JS, zero server round-trips) — prevents typos while keeping it hidden by default
- "Test connection" button that fires a GET to `{OmphalosUrl}/api/auth/me` from the server and shows a success/fail badge — catches wrong URL before DMs hit a broken link in production
- Placeholder text on URL field: `https://your-omphalos-instance` to make format expectation clear

### UX Notes

- The existing AdminController follows a strict card pattern (`modern-card`, `modern-card-header`, `modern-card-body`); Settings view must use the same structure for visual consistency
- Button row: Cancel (secondary, back to Users) on left; Save (primary, `fa-save`) on right — matches EditUser.cshtml layout exactly
- Add "Settings" to the Admin navbar dropdown alongside "User Management" and "Quest Management" (already in `_Layout.cshtml` at lines 37–48)
- DB-backed storage: a new `AppSettingsEntity` table with `Key varchar(100) PK` and `Value nvarchar(max)` is the simplest approach — no separate typed entity needed. Two rows: `"OmphalosUrl"` and `"OmphalosSharedSecret"`. Alternatively a single `IntegrationSettingsEntity` with two typed columns is cleaner to query but requires migration changes if more settings are added later. **Recommendation: single typed entity `IntegrationSettingsEntity` with columns `OmphalosUrl` and `OmphalosSharedSecret`** — simpler access pattern, one EF entity, one migration, no key-string magic.
- Service interface: `IIntegrationSettingsService` with `GetAsync()` and `SaveAsync(url, secret)` — matches existing service naming conventions

---

## External Tool Navigation

### Table stakes

- DM navbar dropdown ("Dungeon Master" menu, lines 54–78 in `_Layout.cshtml`) gets a new item: `<i class="fas fa-external-link-alt me-2"></i>Open DM Tool` — links to the deep-link generation action, not directly to Omphalos
- Item is only shown when `OmphalosUrl` is configured and non-empty; inject `IIntegrationSettingsService` into the layout or use a `ViewComponent` to check this at render time
- Quest Detail page (accessible to all) and Quest Manage page (DM-only) get an "Open Session Notes" button — visually a `btn btn-outline-info` with `fa-external-link-alt` icon
- Both links target the same Quest Board action: `GET /Quest/OpenSession/{questId}` — that action generates the token and issues a redirect to Omphalos
- Button on Quest Manage page appears only when `OmphalosUrl` is configured; on Quest Detail it also requires DM/Admin role to avoid exposing the SSO flow to players

### Differentiators

- When Omphalos is not configured, render the button as disabled with tooltip "Omphalos not configured" rather than hiding it — gives admins a visual cue that the setting needs filling in
- New browser tab (`target="_blank"`) so the DM does not lose their Quest Board context

---

## HMAC Deep Link / Token Format

### Token payload

Minimum required fields (query string parameters, all URL-encoded):

| Parameter | Type | Value |
|-----------|------|-------|
| `username` | string | Quest Board `User.Name` (normalized) — used for user matching in Omphalos |
| `questId` | int | Quest Board `Quest.Id` — used for session find-or-create |
| `questTitle` | string | `Quest.Title` — used to pre-fill session title on creation |
| `exp` | long | Unix timestamp (UTC seconds) — recommended window: 60–120 seconds |
| `sig` | string | HMAC-SHA256 over the concatenated canonical form of the other params, hex-encoded or base64url-encoded |

Canonical string for HMAC input (alphabetical key order, `&`-separated, no sig param):
```
exp={exp}&questId={questId}&questTitle={questTitle}&username={username}
```

Do NOT include anything sensitive (passwords, emails) in the token — username alone is sufficient for user matching.

### URL format

```
{OmphalosBaseUrl}/api/auth/sso?username={u}&questId={id}&questTitle={t}&exp={ts}&sig={hmac}
```

The token lives entirely in the query string. No JWT wrapper needed — JWT is already Omphalos's internal session mechanism. Adding a JWT-signed deep-link token would add a dependency on shared JWT config between apps; HMAC over query params keeps both apps decoupled and uses only the shared secret.

### Table stakes

- Quest Board generates the HMAC using `HMACSHA256` from `System.Security.Cryptography` — no NuGet addition needed
- Token is generated server-side in an action (`GET /Quest/OpenSession/{questId}`) and the response is a `Redirect(url)` — the token is never stored
- Expiry of 60–120 seconds is enough for a browser redirect; any longer and replay risk grows
- Omphalos validates: signature matches, expiry not passed, username is non-empty, questId is positive integer — reject with 400/401 otherwise
- After successful validation, Omphalos sets its JWT httpOnly cookie (`omphalos_token`, 30-day expiry matching existing `TokenCookieOptions`) and redirects to `/#session-{sessionId}` so the React SPA opens directly on the right session

---

## Cross-App SSO / Auto-Provisioning

### Table stakes

Omphalos SSO endpoint (`POST /api/auth/sso` — new, added to `AuthEndpoints.cs`):

1. Parse and validate query-string token (sig, exp, username, questId)
2. Look up user by username: `IUserRepository.GetByUsernameAsync(username)`
3. If user does not exist: create with `UserRole.Player`, a random bcrypt-hashed password (never used — SSO users do not log in with password)
4. Find-or-create the GameSession for this quest (see Quest-Session Linking below)
5. Generate JWT via existing `AuthService.GenerateToken(user)` (already private — extract or duplicate)
6. Set `omphalos_token` cookie with existing `TokenCookieOptions`
7. Return redirect URL pointing to the session in the React SPA: `/#session-{sessionId}` or return `{ redirectUrl }` JSON and let the client redirect

### User matching strategy

**Match on `username` (case-insensitive).** This is confirmed in PROJECT.md ("Username-based user matching — assumes same username in both apps"). Omphalos `UserConfiguration` has `HasIndex(u => u.Username).IsUnique()` so a `GetByUsernameAsync` is exact-match on an indexed column.

Do NOT match on email: Omphalos `User` entity has no email field. Matching on a Guid or external ID would require schema changes on both sides. Username match requires zero schema changes on either side for the SSO user-lookup path.

### Edge cases

| Case | Handling |
|------|---------|
| Username does not exist in Omphalos | Auto-create with `UserRole.Player`; random unusable password |
| Username exists, different Guid | Normal login — just issue JWT; no merge needed |
| Username collision (two Quest Board users with same name) | Impossible — Quest Board enforces unique usernames via ASP.NET Core Identity (`IdentityDbContext` unique `UserName` index) |
| Token expired | Return 401; Quest Board should surface an error message to DM |
| Signature invalid | Return 401; log at Warning level |
| Omphalos is down | Quest Board redirect goes nowhere — DM sees browser error; no fallback needed (both apps are standalone) |
| DM logs in normally to Omphalos separately | Existing normal login still works; SSO is additive |
| Auto-provisioned user wants to use Omphalos standalone later | Admin must set a password for them via Omphalos admin panel (`POST /api/admin/users`) |

---

## Quest-Session Linking

### Table stakes

**On the Quest Board side:** Add `ExternalSessionId string?` column to `QuestEntity` (and domain `Quest` model). This stores the Omphalos `GameSession.Id` string (format `session-{timestamp}` as seen in `App.jsx` line 50: `` id: `session-${Date.now()}` ``).

**On the Omphalos side (SSO endpoint logic):**

Find-or-create algorithm:
1. Parse `questId` from token
2. Add `ExternalQuestId string?` column to `GameSession` entity — this is the reliable lookup key
3. Query: `db.GameSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.ExternalQuestId == questId.ToString())`
4. If session exists: return its `Id`
5. If session does not exist: create via `repo.UpsertAsync(...)` with `ExternalQuestId = questId.ToString()`, `Title = questTitle`

**Storing the session ID back on Quest Board:** Quest Board does NOT need to store the Omphalos session ID at this milestone. The link is always re-derived: Quest Board passes `questId`, Omphalos looks it up by `ExternalQuestId`. Storing `ExternalSessionId` on Quest Board is a differentiator (enables "session exists" badge), not table stakes.

### Omphalos session structure (from source)

`GameSession` entity (`Omphalos.Domain/Entities/GameSession.cs`):

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Client-generated, format `session-{timestamp}` (from `App.jsx`) |
| `UserId` | `Guid` | FK to `User` — sessions are per-user (DM owns their sessions) |
| `Title` | `string` | Max 500 chars |
| `DateCreated` | `long` | Unix millisecond timestamp |
| `DateModified` | `long` | Unix millisecond timestamp |
| `SessionLog` | `JsonDocument?` | Rich text / block-editor JSON; stored as `jsonb` in PostgreSQL |
| `SessionNotes` | `string?` | Plain text notes field |
| `Metadata` | `SessionMetadata` | Owned entity — stored inline, all fields nullable strings |
| `Characters` | collection | Per-session character snapshots |
| `Locations` | collection | Per-session location records |
| `Encounters` | collection | Per-session encounter records |

`SessionMetadata` fields (all `string?`): `Location`, `DateTime`, `Weather`, `NpcsMet`, `TreasureAcquired`, `PlotPoints`, `PartyLevelChange`, `NextSessionHooks`.

Sub-resources (`Characters`, `Locations`, `Encounters`) are created by the DM inside Omphalos after navigating there — not populated on creation from Quest Board.

### What quest data to pass

When Omphalos creates a new session from the SSO endpoint, populate:

| Session field | Value |
|---------------|-------|
| `Id` | `` session-{questId}-{unixMs} `` — deterministic prefix, unique suffix |
| `UserId` | Guid of the Omphalos user (just found or created) |
| `Title` | `questTitle` from token (Quest Board `Quest.Title`) |
| `DateCreated` | `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` |
| `DateModified` | same as DateCreated |
| `SessionLog` | `null` |
| `SessionNotes` | `null` |
| `Metadata` | empty `SessionMetadata()` |
| `ExternalQuestId` | `questId.ToString()` — new column, the permanent lookup key |

The existing `SessionService.UpsertAsync` / `ISessionRepository.UpsertAsync` path handles insert vs update — the SSO endpoint can call the repository directly (or add a thin `FindOrCreateByExternalQuestIdAsync` method) rather than going through the full `UpsertSessionRequest` DTO path.

---

## Complexity Notes

**Build order dependencies:**

1. **Admin Settings must be built first.** Navigation links and SSO both read `OmphalosUrl` from settings. Nothing else can be shown or tested until this works.
2. **Navigation can be built independently of SSO** once settings exist. The DM navbar link reads the URL; it does not need the token generation logic yet.
3. **Token generation (Quest Board side) and SSO endpoint (Omphalos side) can be built in parallel** — they share only the shared-secret value and the agreed token format above.
4. **Quest-session linking on the Omphalos side is part of the SSO endpoint** — not a separate phase. The find-or-create runs inside the same SSO handler.
5. **`ExternalQuestId` migration on Omphalos** must land before the SSO endpoint goes to production. Nullable column add — zero-downtime migration.
6. **`ExternalSessionId` on Quest Board** (storing Omphalos session ID back) is optional at this milestone. Defer unless a "session exists" indicator on the Quest Manage page is explicitly required.

**Non-obvious complexity:**

- Omphalos `AuthService.GenerateToken` is `private` (line 37 of `AuthService.cs`). The SSO endpoint either duplicates the JWT generation inline, or extracts `GenerateToken(User user)` to a new method on `IAuthService` (e.g., `GenerateTokenAsync(User user)`). Best approach: add `Task<string> GenerateTokenAsync(User user)` to `IAuthService` and make `AuthService.GenerateToken` call it.
- Omphalos `GameSession.Id` is a client-assigned string — no DB auto-generation. The SSO endpoint must supply a valid, unique ID when creating. `` session-{questId}-{unixMs} `` is deterministic but unique per creation attempt. The `ExternalQuestId` column is the real lookup key.
- Quest Board shared secret is stored in the DB via `IntegrationSettingsEntity` and loaded at request time. It must NOT be cached for the full app lifetime — a settings change must take effect on the next request. Use `IIntegrationSettingsService` injected as `Scoped`, not `Singleton`.
- Omphalos shared secret is read from `IConfiguration` (env var). No caching concern.
- The `omphalos_token` cookie uses `SameSite=Lax`. The SSO endpoint receives a redirect from Quest Board (cross-origin navigation), so the browser will send the Set-Cookie response and store it correctly under `Lax` mode (Lax allows cookies to be set on top-level navigations). If apps are on entirely different domains with no common parent, verify the browser does not block the redirect — this is a self-hosted deployment concern, not a code concern.
