# Phase 22: Session Reminders - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-26
**Phase:** 22-session-reminders
**Areas discussed:** Idempotency tracking, DM manual trigger, Digest template, CRON timezone

---

## Idempotency Tracking

| Option | Description | Selected |
|--------|-------------|----------|
| Column on Quest entity | `ReminderSentAt (DateTime?)` on Quest — simple, one flag per quest | |
| ReminderLog table | New `(QuestId, PlayerId, SentAt)` table — per-player granularity, survives partial failures | ✓ |

**User's choice:** ReminderLog table

| Option | Description | Selected |
|--------|-------------|----------|
| One row per quest+player | `(QuestId, PlayerId, SentAt)` — check per player before send | ✓ |
| One row per email sent | `(QuestId, SentAt, RecipientEmail)` — less precise for retry | |

**User's choice:** One row per quest+player

---

## DM Manual Trigger

| Option | Description | Selected |
|--------|-------------|----------|
| Bypass idempotency guard | Always enqueue regardless of ReminderLog | |
| Respect it, warn if already sent | Check log first; if all sent, show warning + confirm | ✓ |

**User's choice:** Respect idempotency, warn if already sent

| Option | Description | Selected |
|--------|-------------|----------|
| Confirmed players only | Only players selected at finalization | |
| Yes + Maybe voters | All players who voted Yes or Maybe, No voters excluded | ✓ |

**Notes:** User explained: "Yes + Maybe because they are combined in a table view. No voters are separated."

| Option | Description | Selected |
|--------|-------------|----------|
| Success toast / TempData message | Stays on Manage page, "Reminder queued for X players." | ✓ |
| Redirect with success message | Redirect to Quest Details with success banner | |

| Option | Description | Selected |
|--------|-------------|----------|
| Finalized section only | Button visible only when `IsFinalized = true` | ✓ |
| Always visible | Always shown, disabled with tooltip when not finalized | |

**Notes:** User specified: "put it near the other buttons (open quest, followup, refresh)"

---

## Digest Template

| Option | Description | Selected |
|--------|-------------|----------|
| New DigestReminder.razor | Separate component, `IList<QuestSummary>` parameter | |
| Extend SessionReminder.razor | Accept optional list — risks breaking Phase 21 component | |
| Skip digest entirely | Drop EMAIL-04 and REMIND-02 | ✓ |

**User's choice:** Skip digest entirely

**Notes:** User raised that same-day quests have never happened in one year of use: "I suppose it will never happen. So the whole digest mail feature becomes obsolete." Confirmed when asked: "Skip digest entirely."

---

## CRON Timezone

| Option | Description | Selected |
|--------|-------------|----------|
| 09:00 UTC | Server UTC = 10:00/11:00 Dutch time | |
| 09:00 server local | LXC server uses local CET/CEST | ✓ |

**Notes:** User clarified: "The app runs on a dedicated LXC server, not in docker. I would suggest the reminder is sent 24 hours before the chosen date on the quest... No need to use timezones if the server time is used right?"

| Option | Description | Selected |
|--------|-------------|----------|
| Fixed daily job, looks for quests tomorrow | Daily at 09:00, `FinalizedDate.Date == DateTime.Today.AddDays(1)` | ✓ |
| Hourly job, ±30min window around 24h mark | More precise but more infrastructure | |

**Notes:** User confirmed: "Yeah it's way easier to just run at a fixed time a day before ignoring the time of the quests."

**Final CRON:** `"0 9 * * *"` (09:00 server local time)

---

## Claude's Discretion

- Exact column names and indexes on ReminderLog
- Whether DailyReminderJob enqueues per-quest SessionReminderJobs or processes inline
- Button label ("Send Reminder", "Remind Players", etc.)
- `forceResend` implementation approach
- Controller action name for DM trigger endpoint

## Deferred Ideas

- EMAIL-04 (Digest email) — dropped; same-day quests never occurred in one year
- REMIND-02 (Digest send behavior) — dropped with EMAIL-04
