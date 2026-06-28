# Phase 23: Admin Email Stats - Context

**Gathered:** 2026-06-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Add an admin-only email stats page at `/Admin/EmailStats` that shows live sent, delivered, bounced, and failed counts for the last 30 days, pulled from the Resend REST API via typed `HttpClient` with Bearer token authentication — no Resend SDK added.

</domain>

<decisions>
## Implementation Decisions

### Page Placement
- **D-01:** New standalone action `AdminController.EmailStats` at `/Admin/EmailStats` — follows the existing pattern (Users, Quests actions are already in AdminController)
- **D-02:** Add a nav link to the stats page in the admin sidebar/nav so it is discoverable without knowing the URL

### Stats Scope & Display
- **D-03:** Show 4 summary stat cards: **Sent**, **Delivered**, **Bounced**, **Failed** counts — matches STATS-01 and the roadmap success criteria; no individual email list
- **D-04:** Time range is **last 30 days** — filter by `created_at >= today-30d` when calling Resend's `GET /emails`; Resend returns individual records, aggregation is done client-side

### Error / Degraded State UX
- **D-05:** When `ResendApiKey` is missing from config: render the stats page shell with a **yellow/orange alert banner** — "ResendApiKey not configured — add it to appsettings.json to enable stats." No stack trace or exception page
- **D-06:** When the Resend API call fails (network error, 401, 429, etc.): render the same page shell with a **red alert banner** — "Could not fetch email stats — Resend API returned an error. Check your API key and try again." No raw exception details shown

### Data Freshness & Caching
- **D-07:** Cache the fetched stats in **`IMemoryCache` for 5 minutes** — avoids repeated Resend API calls on rapid admin refreshes; IMemoryCache is already registered in the project
- **D-08:** Provide a **Refresh button** on the stats page that clears the cache key and forces a new Resend API call — allows the admin to get fresh data on demand (recommended: `?force=true` query param or POST to a refresh action, then redirect back)

### Pre-Decided (from prior phases)
- **D-09:** `ResendApiKey` is added as a new property on the existing `EmailSettings` record in `EuphoriaInn.Domain/Models/EmailSettings.cs`; read via `IOptions<EmailSettings>` — same binding pattern used for SMTP credentials
- **D-10:** Resend API call uses `HttpClient` with `Authorization: Bearer {ResendApiKey}` and calls `GET https://api.resend.com/emails` — no Resend SDK package added to the project
- **D-11:** Page is protected by the existing `"AdminOnly"` authorization policy (Phase 20 pattern)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase Requirements
- `.planning/REQUIREMENTS.md` §STATS-01 — The single active requirement for this phase: live sent/delivered/bounced/failed counts from Resend API via plain HttpClient

### Architecture & Patterns
- `.planning/ROADMAP.md` §Phase 23 — Goal, success criteria (4 items), and "UI hint: yes"
- `.planning/codebase/ARCHITECTURE.md` — Layer structure; HttpClient service belongs in Service layer; EmailSettings in Domain
- `.planning/codebase/CONVENTIONS.md` — Naming patterns, controller/view conventions, IOptions usage

### Existing Code to Extend
- `EuphoriaInn.Domain/Models/EmailSettings.cs` — Add `ResendApiKey` property here
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — Add `EmailStats` action here; follows `[Authorize(Policy = "AdminOnly")]` pattern already set at class level

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController` (`[Authorize(Policy = "AdminOnly")]` at class level) — new `EmailStats` action added here; no separate controller needed
- `EmailSettings` record — already has SMTP config; just needs `ResendApiKey` string property added
- `IMemoryCache` — already registered in DI; inject into the service/controller that fetches stats
- Modern-card pattern (`modern-card`, `modern-card-header`, `modern-card-body` CSS classes) — use for the stats page layout and the 4 stat cards

### Established Patterns
- `IOptions<EmailSettings>` — how all email config is read; follow this for `ResendApiKey`
- `IServiceScopeFactory` pattern for Hangfire jobs (Phase 20 decision) — not relevant here (no background job), but worth knowing if a background refresh is ever added
- TempData success/error banners — used in other admin actions; alert banner for error state follows the same visual language

### Integration Points
- `AdminController` → new `EmailStats(GET)` + optional `RefreshEmailStats(POST)` actions
- `appsettings.json` / `appsettings.Development.json` — add `ResendApiKey` under `EmailSettings` section (already has SmtpServer, SmtpPort, etc.)
- Admin nav partial — add link to `/Admin/EmailStats`

</code_context>

<specifics>
## Specific Ideas

- The 4 stat cards should be visually distinct by category (e.g., sent=blue, delivered=green, bounced=orange, failed=red) — consistent with the D&D theme's color usage in other admin views
- Refresh button: simplest viable approach is a query param `?force=true` on the GET action that clears the cache key before fetching; no separate POST endpoint required

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 23-admin-email-stats*
*Context gathered: 2026-06-26*
