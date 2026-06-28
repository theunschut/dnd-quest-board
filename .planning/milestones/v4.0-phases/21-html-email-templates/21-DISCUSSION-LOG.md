# Phase 21: HTML Email Templates - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-26
**Phase:** 21-html-email-templates
**Areas discussed:** Email visual style, Date-changed email scope, Reminder template content, IEmailRenderService wiring

---

## Email visual style

| Option | Description | Selected |
|--------|-------------|----------|
| D&D themed | Parchment-toned, medieval colors, quest board branding | ✓ |
| Clean modern | Simple table layout, brand colors, no background images | |
| Minimal plain-styled | White background, logo, single accent color | |

**User's choice:** D&D themed — specifically matching the quest card aesthetic from the app. User referenced the existing quest card design (parchment scroll with Cinzel font, dark brown text, gold accents, wax seal).

| Poster usage option | Selected |
|---------------------|----------|
| Header banner | |
| Full background attempt | ✓ |

**Notes:** User wants the poster image as a full background. Acknowledged that Outlook blocks background images but noted users can whitelist the sender. User shared a screenshot of the quest card for visual reference: portrait parchment scroll, gold CR badge top-left, Cinzel title, italic description, DM/date/players in bold below, wax seal bottom-left.

| CR badge option | Selected |
|-----------------|----------|
| Yes — include CR badge | ✓ |
| No — omit CR | |

**Notes:** Include the gold CR badge in finalization and reminder emails, matching the quest card design.

| Text area style | Selected |
|----------------|----------|
| Parchment/cream tone | ✓ (via matching app colors) |

**Notes:** User confirmed: "similar to the fonts, colors etc. that are now used on the questboard." — Cinzel font, dark brown (#1a0f08), gold (#FFD700/#ffc107), parchment (#F4E4BC).

---

## Date-changed email scope

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — upgrade it too | All three email types get HTML in Phase 21 | ✓ |
| No — defer to later | Date-changed stays plain-text | |

**User's choice:** Yes — upgrade SendQuestDateChangedEmailAsync to HTML alongside the finalization email.

---

## Reminder template content

| Data element | Selected |
|--------------|----------|
| Quest name + DM name + date/time | ✓ |
| Quest description excerpt | — (full description instead) |
| Confirmed players list | ✓ |
| Link back to quest page | ✓ |

**User's choice:** Quest name, DM name, date/time, **full quest description** (~500 words, not truncated), confirmed players list, link back to quest page.

**Notes on players list:** Character names preferred, with fallback to player name when no character assigned.

---

## IEmailRenderService wiring

| Option | Description | Selected |
|--------|-------------|----------|
| QuestService calls render then send separately | Clean separation, generic SendAsync method | |
| IEmailService handles rendering internally | Mixed concerns inside IEmailService | |
| All emails via Hangfire jobs | QuestService enqueues, job renders and sends | ✓ |

**User's choice:** All outbound emails go through Hangfire fire-and-forget jobs. QuestService enqueues a job; the job resolves IEmailRenderService + IEmailService via IServiceScopeFactory, renders HTML, checks dedup guard, then sends.

**Rationale (user's words):** "The messages are queued and will be send whenever the app has time to do so. Also, on a reboot, the mails will be send anyway."

| Date-changed via Hangfire | Selected |
|---------------------------|----------|
| Yes — all emails via Hangfire | ✓ |
| No — date-changed stays synchronous | |

---

## Claude's Discretion

- Exact wax seal image selection (fixed or randomized per questId hash)
- Whether typed IEmailService methods are removed or kept as deprecated wrappers
- Exact inline CSS for email templates
- Hangfire job naming conventions for email jobs

---

## Deferred Ideas

None — discussion stayed within phase scope.
