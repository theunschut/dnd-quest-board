# Requirements: D&D Quest Board — Milestone 3: Omphalos Integration

**Defined:** 2026-06-18
**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

---

## v1 Requirements

### Admin Settings (Phase 10 — Quest Board)

- [ ] **SETT-01**: Admin can navigate to a Settings page from the Admin navbar dropdown
- [ ] **SETT-02**: Settings page has input fields for Omphalos URL and shared secret
- [ ] **SETT-03**: Shared secret field renders as `type="password"` (masked in the UI)
- [ ] **SETT-04**: Submitting the form with the secret field blank preserves the existing secret — empty input = keep existing value, never overwrite with empty string
- [ ] **SETT-05**: An "Integration Enabled" checkbox controls whether all Omphalos UI elements are visible and the SSO redirect is active; when unchecked, no Omphalos buttons or links appear and `LaunchOmphalos` returns 404
- [ ] **SETT-06**: Settings are persisted in an `IntegrationSettingsEntity` table (single-row upsert) with columns `OmphalosUrl`, `OmphalosSharedSecret`, `IsEnabled`
- [ ] **SETT-07**: Settings page is protected by the `AdminOnly` authorization policy
- [ ] **SETT-08**: An EF Core migration creates the `IntegrationSettings` table

### Navigation + Token Generation (Phase 11 — Quest Board)

- [x] **NAV-01**: `_Layout.cshtml` renders an `OmphalosNavItem` View Component in the DM navbar dropdown; shows an "Open Omphalos" link only when integration is enabled and `OmphalosUrl` is configured
- [x] **NAV-02**: "Open Omphalos" navbar link opens the Omphalos base URL in a new tab (plain navigation link — no SSO token)
- [x] **NAV-03**: Quest Detail page shows an "Open Session Notes" button when integration is enabled, `OmphalosUrl` is set, and the current user is a DM or Admin
- [x] **NAV-04**: Quest Manage page shows the same "Open Session Notes" button under the same conditions as NAV-03
- [x] **NAV-05**: When integration is disabled or `OmphalosUrl` is not configured, neither the navbar link nor the quest page buttons appear
- [x] **TOKEN-01**: `IIntegrationTokenService` (Domain layer) generates a signed redirect URL given quest ID, quest title, and DM username
- [x] **TOKEN-02**: The HMAC-SHA256 canonical message is the query string `expiry={unix_ts}&questId={id}&questTitle={url_encoded_title}&username={lower}` with keys in alphabetical order — questId must be in the MAC to prevent token substitution across quests
- [x] **TOKEN-03**: Tokens expire after 300 seconds (5 minutes) from generation time
- [x] **TOKEN-04**: Username is normalized to lowercase before inclusion in both the MAC message and the URL parameter
- [x] **TOKEN-05**: A new `QuestController.LaunchOmphalos(int id)` GET action generates the signed URL and returns `Redirect(signedUrl)`; returns 404 when integration is disabled

### SSO Endpoint (Phase 20 — Omphalos)

- [ ] **SSO-01**: Omphalos exposes `GET /api/sso/open-quest` accepting query parameters `questId`, `questTitle`, `username`, `expiry`, `sig`
- [ ] **SSO-02**: Endpoint validates the HMAC-SHA256 signature using the `QUEST_BOARD_SECRET` env var; invalid or missing signature returns HTTP 400
- [ ] **SSO-03**: Tokens with `expiry` more than 300 seconds in the past are rejected with HTTP 400
- [ ] **SSO-04**: If no Omphalos user matching the normalised username exists, a `UserRole.Player` account is auto-provisioned with a randomly-generated unusable password; existing accounts are not modified
- [ ] **SSO-05**: Endpoint finds an existing `GameSession` by `ExternalQuestId` or creates one with `Title = questTitle` from the token and a server-generated session ID
- [ ] **SSO-06**: On success, the endpoint issues the Omphalos JWT httpOnly cookie and redirects the browser to the session page
- [ ] **SSO-07**: `IAuthService.GenerateToken(User)` is promoted from private method to the `IAuthService` interface so `SsoService` can call it
- [ ] **SSO-08**: If `QUEST_BOARD_SECRET` is not set in the environment, the endpoint returns HTTP 503 with a descriptive message — Omphalos remains fully functional as a standalone app
- [ ] **SSO-09**: The Omphalos JWT cookie `SameSite` attribute is configurable via an `OMPHALOS_SAMESITE` env var (defaults to `Lax`; when set to `None`, `Secure = true` is also applied)

### Quest-Session Linking (Phase 20 — Omphalos)

- [ ] **LINK-01**: `GameSession` entity has a nullable `int? ExternalQuestId` column with a unique partial index (non-null values only)
- [ ] **LINK-02**: An EF Core migration adds `ExternalQuestId` and its unique index to the `game_sessions` table in PostgreSQL
- [ ] **LINK-03**: Find-or-create logic in `SsoService` queries by `ExternalQuestId` first; only creates a new session if no match is found

---

## v2 Requirements (Future)

- **NAV-EXT-01**: Omphalos → Quest Board reverse API call (e.g., pull quest details into session view) — bidirectional foundation in place; full implementation future milestone
- **CROP-01–05**: Profile picture avatar crop (deferred from Milestone 2)
- **BUG-01**: DM can add new dates to an existing quest (issue #94)
- **BUG-02**: Profile images ≤ 5MB do not return HTTP 413 (issue #91)

---

## Out of Scope

| Feature | Reason |
|---------|--------|
| OAuth / OIDC SSO | Overkill for self-hosted group app; HMAC shared-secret is sufficient |
| Omphalos → Quest Board API calls | Bidirectional foundation laid in M3; full reverse implementation deferred |
| User account linking UI (explicit) | Auto-provisioning by username match is sufficient; no manual linking step needed |
| Nonce store for replay protection | Short 5-minute expiry acceptable for private group app; nonce store adds stateful complexity |
| Pre-flight Omphalos health check | Adds latency; button visibility gating (IsEnabled + OmphalosUrl) is sufficient for MVP |
| Admin user management, quest management | Existing Admin panel features; not modified in this milestone |

---

## Traceability

| Requirement | Phase | Repo | Status |
|-------------|-------|------|--------|
| SETT-01 | Phase 10 | Quest Board | Pending |
| SETT-02 | Phase 10 | Quest Board | Pending |
| SETT-03 | Phase 10 | Quest Board | Pending |
| SETT-04 | Phase 10 | Quest Board | Pending |
| SETT-05 | Phase 10 | Quest Board | Pending |
| SETT-06 | Phase 10 | Quest Board | Pending |
| SETT-07 | Phase 10 | Quest Board | Pending |
| SETT-08 | Phase 10 | Quest Board | Pending |
| NAV-01 | Phase 11 | Quest Board | Complete (11-02) |
| NAV-02 | Phase 11 | Quest Board | Complete (11-02) |
| NAV-03 | Phase 11 | Quest Board | Complete (11-01 backend + 11-02 view) |
| NAV-04 | Phase 11 | Quest Board | Complete (11-01 backend + 11-02 view) |
| NAV-05 | Phase 11 | Quest Board | Complete (11-01 LaunchOmphalos 404 + 11-02 view gating) |
| TOKEN-01 | Phase 11 | Quest Board | Complete (11-01) |
| TOKEN-02 | Phase 11 | Quest Board | Complete (11-01) |
| TOKEN-03 | Phase 11 | Quest Board | Complete (11-01) |
| TOKEN-04 | Phase 11 | Quest Board | Complete (11-01) |
| TOKEN-05 | Phase 11 | Quest Board | Complete (11-01) |
| SSO-01 | Phase 20 | Omphalos | Pending |
| SSO-02 | Phase 20 | Omphalos | Pending |
| SSO-03 | Phase 20 | Omphalos | Pending |
| SSO-04 | Phase 20 | Omphalos | Pending |
| SSO-05 | Phase 20 | Omphalos | Pending |
| SSO-06 | Phase 20 | Omphalos | Pending |
| SSO-07 | Phase 20 | Omphalos | Pending |
| SSO-08 | Phase 20 | Omphalos | Pending |
| SSO-09 | Phase 20 | Omphalos | Pending |
| LINK-01 | Phase 20 | Omphalos | Pending |
| LINK-02 | Phase 20 | Omphalos | Pending |
| LINK-03 | Phase 20 | Omphalos | Pending |

**Coverage:**
- v1 requirements: 30 total
- Mapped to phases: 30/30
- Unmapped: 0

---
*Requirements defined: 2026-06-18*
