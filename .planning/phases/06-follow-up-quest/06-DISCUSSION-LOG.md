# Phase 6: Follow-Up Quest - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-04
**Phase:** 06-follow-up-quest
**Areas discussed:** Form pre-fill scope, Which players qualify, Link display style, Multiple follow-ups

---

## Form Pre-Fill Scope

| Option | Description | Selected |
|--------|-------------|----------|
| Partial copy (CR + PlayerCount + DM; blank Title/Description) | Safest balance — forces distinct title/description, copies numeric fields | |
| Full copy (Title + Description + CR + PlayerCount + DM) | All fields copied; fastest flow for direct sequels; DM edits before saving | ✓ |
| Minimal copy (DM only) | Cleanest slate; barely different from normal Create | |

**User's choice:** Full copy
**Notes:** Title is appended with `" - Part 2"` to reduce the risk of identical quest names on the board. ProposedDates explicitly cleared regardless — clarified during wrap-up that new dates are always required (FOLLOW-03) and should never be inherited.

---

## Which Players Qualify

| Option | Description | Selected |
|--------|-------------|----------|
| IsSelected=true only, reset role to Player | Only chosen players; all imported as Player; DM reassigns roles on Manage page | ✓ |
| IsSelected=true only, preserve original role | Same filter; SignupRole carried over from original | |
| IsSelected non-Spectators, reset to Player | Excludes Spectators; only Player + AssistantDM selected players qualify | |
| All signups, reset to Player | Everyone who expressed interest; defeats pre-approval purpose | |

**User's choice:** IsSelected=true only, reset role to Player
**Notes:** Aligns with GitHub issue #49 intent — "players who did part 1 get a seat in part 2." Roles reset to Player; DM can reassign on Manage page after creation.

---

## Link Display Style

| Option | Description | Selected |
|--------|-------------|----------|
| Inline line in Quest Summary sidebar | Fits existing sidebar pattern; minimal markup; works on both Details and Manage pages | ✓ |
| Alert panel at top of main card body | Bootstrap alert-info; immediately visible but adds visual weight | |
| Dedicated Quest Chain modern-card in sidebar | Prominent labelled section; scales to multi-part chains; more markup | |

**User's choice:** Inline line in Quest Summary sidebar
**Notes:** Wording: "Continues in: [Title]" on original, "Continues from: [Title]" on follow-up. fa-scroll icon. Link goes to Details page of the related quest.

---

## Multiple Follow-Ups

| Option | Description | Selected |
|--------|-------------|----------|
| Chain only — one follow-up per quest | Button hidden once follow-up exists; but follow-up can itself have a follow-up (Part 1 → 2 → 3) | ✓ |
| One-per-quest, no chaining | Hard limit: one follow-up total, follow-ups cannot be followed up | |
| Unrestricted | Any quest can have any number of follow-ups | |

**User's choice:** Chain only — one follow-up per quest
**Notes:** Enforced at application layer. Each quest may have at most one direct follow-up. Chaining (Part 1 → Part 2 → Part 3) is explicitly allowed.

---

## Claude's Discretion

- Whether to add a dedicated `CreateFollowUpAsync` service method or extend an existing one
- Exact HTML/CSS for the sidebar line
- URL route for the follow-up creation action
- Whether to check follow-up existence via repository query or domain model navigation

## Deferred Ideas

None raised during discussion.
