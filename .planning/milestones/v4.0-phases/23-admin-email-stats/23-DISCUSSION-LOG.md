# Phase 23: Admin Email Stats - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-26
**Phase:** 23-admin-email-stats
**Areas discussed:** Page placement, Stats scope & display, Error/degraded state UX, Data freshness

---

## Page Placement

| Option | Description | Selected |
|--------|-------------|----------|
| Standalone /Admin/EmailStats page | New AdminController action + view; follows existing pattern | ✓ |
| Embed at top of Users page | Stats section injected into existing Users view; fewer clicks | |
| Separate EmailAdmin controller | New controller just for email admin; overkill for one page | |

**User's choice:** Standalone /Admin/EmailStats page

---

| Option | Description | Selected |
|--------|-------------|----------|
| Add link in admin nav / sidebar | Makes it discoverable; other admin pages are already linked there | ✓ |
| No nav link — direct URL only | Simpler, but admin must remember the URL | |
| You decide | Match existing admin navigation pattern | |

**User's choice:** Add link in admin nav / sidebar

---

## Stats Scope & Display

| Option | Description | Selected |
|--------|-------------|----------|
| Last 30 days | Filter by created_at ≥ today-30d; meaningful window for 17-member group | ✓ |
| All-time | No date filter; grows unbounded over time | |
| Configurable window | Dropdown (7d / 30d / 90d); adds complexity not needed at this scale | |

**User's choice:** Last 30 days

---

| Option | Description | Selected |
|--------|-------------|----------|
| Sent + Delivered + Bounced + Failed counts only | 4 summary cards matching roadmap success criteria; at-a-glance health | ✓ |
| Summary counts + last 10 emails table | Counts at top + recent email list; more detail, more work | |
| You decide | Show what makes sense given Resend API data shape | |

**User's choice:** Sent + Delivered + Bounced + Failed counts only

---

## Error / Degraded State UX

| Option | Description | Selected |
|--------|-------------|----------|
| Alert banner with setup instructions | Yellow/orange banner with instructions to add ResendApiKey; no exception page | ✓ |
| Redirect to admin home with TempData error | Same pattern as other admin errors; loses page context | |
| You decide | Match existing admin error handling pattern | |

**User's choice:** Alert banner with setup instructions (for missing key)

---

| Option | Description | Selected |
|--------|-------------|----------|
| Same alert banner with generic error message | "Could not fetch email stats — check your API key and try again." No raw exception details | ✓ |
| Show partial data with an error note | Display what was retrieved before error; unlikely given all-or-nothing HTTP call | |
| You decide | Use whatever degraded-state pattern fits the codebase | |

**User's choice:** Same alert banner with a generic error message (for API failures)

---

## Data Freshness

| Option | Description | Selected |
|--------|-------------|----------|
| Pull live on every page load | No cache; always accurate; trivially low traffic at 17 members | |
| Cache for 5 minutes in IMemoryCache | Avoids repeated API calls; IMemoryCache already registered | ✓ |
| You decide | — | |

**User's choice:** Cache for 5 minutes in IMemoryCache

---

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — Refresh button that forces a new API call | Admin can get fresh data on demand; ?force=true query param or POST redirect | ✓ |
| No — cache expires naturally after 5 minutes | Simpler; admin waits up to 5 min for fresh data | |
| You decide | — | |

**User's choice:** Yes — a Refresh button that forces a new API call

---

## Claude's Discretion

None — all areas had explicit user choices.

## Deferred Ideas

None — discussion stayed within phase scope.
