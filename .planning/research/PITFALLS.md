# Pitfalls Research — Omphalos Integration

**Domain:** Cross-app HMAC SSO + redirect-based deep link flow
**Researched:** 2026-06-18
**Confidence:** HIGH — grounded in both codebases read directly; security claims verified against known specs

---

## Token Security

### Pitfall: Replay Attack — Token Reuse Within Validity Window

**Risk:** HMAC tokens are stateless and self-contained. If a valid token URL is captured (e.g., from browser history, logs, a forwarded URL, or a shoulder-surf), an attacker can reuse it for the full expiry window. Because Omphalos verifies the HMAC and expiry but has no memory of tokens already seen, there is no mechanism to detect reuse.

**Prevention:** Add a nonce (random UUID or CSPRNG bytes) to the token payload and persist it to a short-lived store on Quest Board generation. Omphalos marks nonces as consumed on first use. Any subsequent use of the same nonce returns 401. The store only needs to live as long as the token TTL — an in-memory `ConcurrentDictionary<string, DateTime>` with a background sweep is sufficient at self-hosted scale. Alternatively, keep the expiry window very short (60 seconds) and accept the residual risk for a trusted group app.

**Decision point for roadmap:** If nonce tracking is not implemented, the token expiry becomes the only defence. 5–15 minutes is the maximum acceptable window without replay protection; 15+ minutes without a nonce store is a meaningful vulnerability.

**Phase:** Phase 11 (SSO endpoint implementation)

---

### Pitfall: Substitution Attack — Token Valid but Sent to Wrong Context

**Risk:** `questId + username + expiry` in the URL is not enough if the MAC input omits any of them. An attacker who holds a valid token for Quest A can present it to Omphalos claiming it is for Quest B by manipulating the query string *after* the MAC was generated — if questId is in the URL but outside the MAC input. If the MAC message is only `username|expiry`, the questId is unprotected and a valid token for User A on Quest 1 can be replayed against Quest 2's SSO link.

**Prevention:** The MAC message MUST include every field that determines the outcome of the SSO: `username|questId|expiry`. Omphalos must re-derive the MAC from the received URL fields and compare — never trust a field that is in the URL but not in the MAC. Order and delimiter must be fixed and documented as a contract.

Recommended canonical message format:
```
{username}|{questId}|{isoUtcExpiry}
```
All three fields in the HMAC input. Omphalos re-assembles from received query params; any mismatch returns 401.

**Phase:** Phase 11 (token generation in Quest Board) and Phase 20 (validation in Omphalos)

---

### Pitfall: Clock Skew Between Containers Breaks Expiry Checks

**Risk:** Quest Board and Omphalos run in separate containers (and separate runtimes: .NET 8 vs .NET 10). Container clock drift can cause tokens that are still valid according to Quest Board to appear expired to Omphalos, producing intermittent 401s that are hard to debug.

**Prevention:** Always express expiry as a UTC Unix timestamp, never a relative offset. Use `DateTime.UtcNow` exclusively — never `DateTime.Now`. Add a 5–10 second grace period on the Omphalos validator side to absorb minor drift.

**Phase:** Phase 11/12

---

### Pitfall: Token Lifetime Too Short Breaks Legitimate Use

**Risk:** If expiry is 30 seconds but the user has a slow connection, the token may expire before Omphalos processes it. This manifests as intermittent 401s that are hard to reproduce.

**Prevention:** 5 minutes (300 seconds) is the practical minimum for a redirect-based SSO with tolerable replay risk for a private group app. This matches industry patterns (SAML assertions use 5 min by default). Do not go below 2 minutes; do not go above 15 minutes without a nonce store.

**Phase:** Phase 11

---

### Pitfall: Token in URL Is Logged by Reverse Proxy

**Risk:** Server access logs typically include the full URL with query parameters. If the token is in `?token=abc123hmac...`, every nginx/Kestrel access log entry for the SSO endpoint contains a valid (until expiry) authentication token. Log aggregation systems or backups may retain this data beyond the token TTL.

**Prevention:** Keep the token expiry short (5 min) to limit the replay window even if logs are compromised. Configure the reverse proxy to redact the `token` query parameter from logs for the `/api/auth/sso` path. For a private self-hosted app, accept this as a known, documented risk if log redaction is not practical.

**Phase:** Phase 11/12 — deployment/config note; code cannot prevent proxy logging

---

## User Provisioning

### Pitfall: Username Collision — Different Person, Same Username

**Risk:** Quest Board and Omphalos manage usernames independently. If both databases have a user called "Theun" representing different real people (e.g., a manually-created Omphalos test account), the SSO auto-provision silently logs into the wrong account. `UserRepository.GetByUsernameAsync` in Omphalos (seen in code) is a straight equality match with no secondary verification.

**Prevention:** Document that username is the authoritative link — any Omphalos account that already exists with a matching username is assumed to belong to that person. Provide an admin recovery path (admin can rename an Omphalos user if a collision occurs). Do NOT silently overwrite — fail loudly if a collision that cannot be resolved automatically is detected, and require manual admin resolution.

**Phase:** Phase 20 (auto-provision logic in Omphalos)

---

### Pitfall: Usernames With Pipe Characters Break MAC Message Format

**Risk:** The canonical MAC message `{username}|{questId}|{expiry}` uses `|` as a delimiter. If a Quest Board username contains a pipe character, the message is ambiguous and the Omphalos validator cannot safely split it. Quest Board uses ASP.NET Core Identity, which allows pipes in usernames. Omphalos `Username` is `character varying(100)` with no character restriction visible in `UserConfiguration.cs`.

**Prevention:** URL-encode each field component before concatenating into the MAC input, and decode before comparison on the Omphalos side. This is more robust than restricting characters because it is format-independent. Both sides must use the same encoding — document it as part of the token spec. Do not rely on delimiter-based splitting of raw values.

**Phase:** Phase 11 (token generation) — this must become a requirement in the token format spec

---

### Pitfall: Username Case Mismatch Creates Duplicate Accounts

**Risk:** Quest Board Identity normalises usernames to uppercase (`NormalizedUserName`) internally but the display `UserName` may be mixed-case (e.g., "Theun"). Omphalos's `GetByUsernameAsync` performs a case-sensitive equality query against PostgreSQL, which has case-sensitive `text` comparisons by default. If Quest Board issues a token with `username=Theun` and Omphalos has a stored user as `theun`, the lookup fails and auto-provision creates a second Omphalos user called `Theun`, violating the unique index with inconsistent data.

**Prevention:** Normalise username to lowercase in the Quest Board token payload. Normalise to lowercase before lookup on the Omphalos side. The `IX_Users_Username` unique index in Omphalos is currently case-sensitive — ensure the index and all lookups use the same case convention, or add a `LOWER(username)` functional index. Define the normalisation rule as a requirement before the token format is finalised.

**Phase:** Phase 11/12 — must be defined before any code is written for either side

---

### Pitfall: Auto-Provisioned DM Gets Wrong Role in Omphalos

**Risk:** Omphalos `UserRole` is `Player` or `Admin` (`User.cs`). An auto-provisioned DM from Quest Board defaults to `Player`. DM-only Omphalos functionality (e.g., Admin-only endpoints that are added later) would be inaccessible to the provisioned account. The DM will not know why they cannot access things.

**Prevention:** The token payload should include the Quest Board role. Omphalos's SSO endpoint maps Quest Board roles to Omphalos roles on provisioning: `DungeonMaster` or `Admin` → Omphalos `Admin`; `Player` → Omphalos `Player`. Define the mapping explicitly as a requirement; do not leave it implicit.

**Phase:** Phase 20

---

## Secret Management

### Pitfall: Shared Secret Visible in Plain Text in Admin UI

**Risk:** If the Admin Settings view renders the shared secret from the DB directly into an `<input type="text">`, any admin who opens the page, or any screenshot/recording, leaks the secret.

**Prevention:** Render the secret field as `type="password"`. On GET (edit page), render the field empty — not populated from DB. On POST, only write a new value if the field is non-empty; empty = keep existing. This prevents accidental exposure and is the standard pattern for password/secret fields in admin UIs. The Quest Board controller must explicitly NOT overwrite the DB value with an empty string if the admin submits without changing the secret.

**Phase:** Phase 10 (Admin Settings page in Quest Board)

---

### Pitfall: Missing Omphalos Env Var — Null HMAC Key Accepts All Tokens

**Risk:** The Omphalos `.env.example` does not yet include `QUESTBOARD_SHARED_SECRET`. If the Omphalos operator forgets to add it, the SSO endpoint either throws a `NullReferenceException` at first use, or — worse — if the HMAC implementation falls back to an empty/zero-byte key, it may silently accept any forged token computed with the same empty key.

**Prevention:** Apply the same required-or-throw pattern already established in Omphalos `Program.cs`:
```csharp
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required");
```
The shared secret must use the same pattern: fail fast at startup with a clear message, not at the first SSO attempt. Add `QUESTBOARD_SHARED_SECRET=change-me` to `.env.example`.

**Phase:** Phase 20

---

### Pitfall: Shared Secret Leaked via Logs or Exception Messages

**Risk:** If exception middleware logs the full `HttpContext` or request body, or if the settings service logs the admin settings value for debugging, the secret may appear in log output. SQL Server query logs (EF Core verbose logging) may also include the value if it is stored as a plain string and selected in a query.

**Prevention:** Do not log the secret value at any level. Mask the secret in any structured log event. For EF Core, ensure `EnableSensitiveDataLogging()` is not enabled in production (it is already only enabled in development in this codebase pattern).

**Phase:** Phase 10

---

## Omphalos Availability

### Pitfall: Omphalos Down — DM Sees a Generic Browser Error With No Feedback

**Risk:** If Omphalos is unreachable, the DM clicks "Open Session Notes" and is served a generic nginx/browser error page. There is no path back to Quest Board and no explanation. From the DM's perspective the Quest Board button is broken.

**Prevention:** Before issuing the redirect, Quest Board fires a lightweight server-side pre-flight check against the Omphalos health endpoint (`GET {omphalosUrl}/health`, timeout: 2–3 seconds). If it fails, render an inline error on the quest page: "Omphalos is not reachable. Check Admin Settings." The quest detail and signup flows must remain fully functional regardless of this check's outcome. This requirement comes directly from the PROJECT.md constraint: "Both apps must function independently without the other running."

**Phase:** Phase 11 (controller action that generates the SSO link)

---

### Pitfall: HttpClient Instantiated Per-Request Causes Socket Exhaustion

**Risk:** Creating `new HttpClient()` in a controller per request exhausts socket descriptors under load (known .NET issue). For a small group app this is unlikely to surface immediately but is a correctness issue.

**Prevention:** Register `IHttpClientFactory` or a typed `OmphalosHealthClient` in `Program.cs` and inject it. Never instantiate `HttpClient` directly in a controller.

**Phase:** Phase 11

---

### Pitfall: Integration Button Visible When Omphalos Is Not Configured

**Risk:** If the "Open Session Notes" button and DM nav link render regardless of whether `OmphalosUrl` is set in admin settings, DMs will see a broken experience on any fresh deployment before the integration is configured.

**Prevention:** The button and nav link must only render when `OmphalosUrl` is non-null/non-empty. This check must be centralised — a view component or a flag in the ViewModel — so it does not need to be duplicated across the quest detail view, quest manage view, and DM nav partial.

**Phase:** Phase 10/11

---

## CORS / Cookie / Redirect Flow

### Pitfall: CORS Misconception — Server-Side Redirect Is Not a Cross-Origin Request

**Risk:** CORS applies only to `fetch`/`XMLHttpRequest` calls from browser JavaScript. A server-side `return Redirect(url)` from Quest Board's MVC controller sends an HTTP 302 to the browser, which follows it as a full navigation. Browsers do not apply CORS rules to navigations. Quest Board does not call Omphalos's API directly; it sends a redirect. Therefore Omphalos's CORS configuration is irrelevant to the SSO flow itself.

**Implication:** No CORS configuration change is needed in Omphalos for Milestone 3. The existing `AllowedOrigins` config in Omphalos `Program.cs` is the right mechanism for any future JavaScript API calls from Quest Board. Quest Board's public origin must be in that list for any future bidirectional API calls (a future milestone per PROJECT.md).

**Phase:** Not a code blocker for any Milestone 3 phase; document as a note for the future bidirectional milestone

---

### Pitfall: SameSite Cookie Attribute Blocks Cookie Being Set After Cross-Domain Redirect

**Risk:** The `omphalos_token` cookie in `AuthEndpoints.cs` is currently set with `SameSite = SameSiteMode.Lax`. `SameSite=Lax` allows cookies to be set by a top-level navigation (a redirect). This works correctly when both apps are on subdomains of the same eTLD+1 (e.g., `questboard.lan` → `omphalos.lan`). However, if the apps are on entirely different top-level domains, modern browsers classify this as a third-party cookie and block it.

Additionally: the comment `// Secure = true ← enable once behind HTTPS` in `AuthEndpoints.cs` means the cookie is currently not `Secure`. If `SameSite=None` is ever needed (different eTLD+1 domains), `Secure = true` is mandatory — `SameSite=None` without `Secure` is rejected by all modern browsers.

**Prevention:**
- Deploy both apps under the same eTLD+1 domain (e.g., `questboard.yourgroup.example` and `omphalos.yourgroup.example`). `SameSite=Lax` then works for the redirect.
- If they cannot share an eTLD+1: change to `SameSite=None; Secure=true` in Omphalos and ensure HTTPS is configured for both.
- Document the deployment topology requirement explicitly. Flag `Secure = true` as a production requirement (the TODO comment is already there — it must become a tracked requirement, not just a comment).

**Phase:** Phase 20 — the cookie options must be verified against the actual deployment topology before the SSO endpoint ships

---

### Pitfall: React SPA Auth Guard Races the Cookie

**Risk:** The SSO flow is: Quest Board → 302 to Omphalos SSO endpoint → Omphalos sets cookie → 302 to SPA route (e.g., `/sessions/{id}`). The React SPA loads and the auth guard fires immediately. If the guard redirects to `/login` before `/api/auth/me` resolves (because the component renders synchronously before the async check completes), the user lands on the login page despite being authenticated.

**Prevention:** The SPA auth guard must be in a "loading" state while `/api/auth/me` is in-flight, and only redirect to `/login` if the request resolves as unauthenticated. Do not redirect on the initial render before the async check completes. This is an Omphalos React concern; flag it in the phase spec so the frontend implementation handles it.

**Phase:** Phase 20

---

## Migration Safety

### Pitfall: Adding ExternalQuestId — Verify Migration Does Not Disturb jsonb Column

**Risk:** `GameSessions` has a `jsonb` column (`SessionLog`). Npgsql EF Core has historically (pre-9.x) regenerated or altered `jsonb` columns in migrations when the model snapshot is rebuilt, sometimes adding a spurious `ALTER COLUMN` that causes data loss or type coercion errors. EF Core 10 (used by Omphalos) is on Npgsql 10.x, which is more stable in this regard, but it is worth verifying.

**Prevention:** After generating the migration for `ExternalQuestId`, read the generated migration file before applying it. Confirm the `Up` method contains only `migrationBuilder.AddColumn<int>()` (or `AddColumn<string>()`) for `external_quest_id` and nothing else modifying `SessionLog`. If any spurious `AlterColumn` appears on `jsonb` columns, hand-edit the migration to remove it.

**Phase:** Phase 20 (Omphalos migration)

---

### Pitfall: GameSession.Id Is Client-Generated — SSO Endpoint Cannot Assume a Session Exists

**Risk:** Looking at `SessionRepository.UpsertAsync`, `GameSession.Id` is a client-generated string, not a DB-assigned identifier. Sessions are created by the React client, not by the backend. When the SSO endpoint needs to deep-link into a specific quest's session, it cannot assume a session with `ExternalQuestId = questId` already exists. The first-ever click for a quest must create the session server-side.

**Prevention:** The SSO endpoint must implement find-or-create by `ExternalQuestId`:
1. Find a `GameSession` for this user where `ExternalQuestId = questId`
2. If found: redirect to `/sessions/{session.Id}`
3. If not found: create a new session with a server-generated ID (`Guid.NewGuid().ToString()`), set `ExternalQuestId`, redirect to the new session

The response redirect must include the resolved session ID. This is a design requirement, not just a pitfall — make it explicit in the phase spec.

**Phase:** Phase 20

---

### Pitfall: Omphalos Deployed Without ExternalQuestId Migration Applied

**Risk:** Both apps auto-apply migrations on startup. But if Omphalos is deployed with a new image that includes the SSO endpoint *without* the migration already being in the image build, the endpoint will throw `column "external_quest_id" does not exist` on the first SSO call.

**Prevention:** This is already handled by the auto-migrate pattern (`db.Database.MigrateAsync()` before `app.Run()`). The migration runs before any requests are served. The only failure mode is if someone deploys the SSO code without the migration file being in the image — which means a broken build, not a runtime surprise. Document: the migration file must be committed to the Omphalos repo and included in the Docker image before the SSO endpoint is deployed.

**Phase:** Phase 20 — deployment sequence note

---

## Summary Table

| # | Pitfall | Severity | Phase | Prevention |
|---|---------|----------|-------|------------|
| 1 | Token replay — reuse within validity window | High | 11/12 | Nonce store OR short expiry (≤5 min) |
| 2 | Token substitution — questId not in MAC input | High | 11/12 | Include `username|questId|expiry` in MAC; Omphalos re-derives from params |
| 3 | Clock skew breaks expiry validation | Medium | 11/12 | UTC Unix timestamps; 5–10s grace period on Omphalos side |
| 4 | Token lifetime too short — intermittent 401s | Low | 11 | 5-minute minimum TTL |
| 5 | Token in URL logged by reverse proxy | Low | 11/12 | Short expiry; log redaction config (deployment note) |
| 6 | Username collision — wrong Omphalos account matched | Medium | 12 | Document assumption; loud failure; admin recovery path |
| 7 | Pipe character in username breaks MAC message | Medium | 11 | URL-encode all field components before MAC concatenation |
| 8 | Username case mismatch creates duplicate accounts | High | 11/12 | Normalise to lowercase both sides; define in token spec |
| 9 | Auto-provisioned DM gets Player role in Omphalos | Medium | 12 | Include QB role in token; define QB→Omphalos role mapping |
| 10 | Shared secret visible in plain text in admin UI | High | 10 | `type="password"` input; blank POST = keep existing value |
| 11 | Missing Omphalos env var — null HMAC key | High | 12 | Required-or-throw on startup; add to .env.example |
| 12 | Secret leaked via logs or exception messages | Medium | 10 | Never log secret; disable EF sensitive logging in prod |
| 13 | Omphalos down — DM sees broken browser error | Medium | 11 | Server-side pre-flight health check (2–3s timeout) |
| 14 | HttpClient per-request socket exhaustion | Low | 11 | Register via IHttpClientFactory |
| 15 | Integration button visible when not configured | Medium | 10/11 | Render button only when OmphalosUrl is non-empty |
| 16 | CORS confusion — redirect is not a fetch | Low | N/A | Documentation only; no CORS config needed for redirect flow |
| 17 | SameSite=Lax blocks cookie on cross-eTLD+1 redirect | High | 12 | Same eTLD+1 deployment OR SameSite=None+Secure; document topology requirement |
| 18 | React SPA auth guard races the cookie | Medium | 12 | SPA guard stays in loading state until /me resolves |
| 19 | jsonb column disturbed by EF migration regeneration | Medium | 12 | Review generated migration SQL before applying; remove spurious AlterColumn |
| 20 | GameSession.Id is client-generated — SSO must create server-side | High | 12 | Find-or-create by ExternalQuestId; server-generates Id on create |
| 21 | Migration not applied before SSO endpoint first use | Low | 12 | Auto-migrate on startup already handles this; ensure migration is in image |
