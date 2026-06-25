# Milestones

## v1.0 — Milestone 2: Refactor + Feature Expansion

**Status:** Complete (Phase 8 avatar crop deferred to future)
**Completed:** 2026-06-17
**Phases:** 1–7, 9 complete (Phase 8 deferred)

### What Shipped

- **Phase 1:** Layer dependency fix — Domain compiles without Repository reference
- **Phase 2:** Email & service consolidation — business logic in services, typed EmailSettings
- **Phase 3:** Code quality & dead code — magic numbers, naming, dead methods removed
- **Phase 4:** Security hardening — account lockout, password minimum, HasKey admin-only, .env gitignored
- **Phase 5:** Shop filter & sort — rarity filter, price sort, URL-persistent state
- **Phase 6:** Follow-up quest — DMs create part-2 quest from finalized quest, players pre-approved
- **Phase 7:** DM profile page — photo, bio, directory links, admin edit
- **Phase 9:** Shop pagination — server-side paging (12/page), stacked search/filter/sort

### What Was Deferred

- **Phase 8:** Profile picture avatar crop — no strong demand, SkiaSharp Docker risk; revisit in future milestone

---

## v1.1 — Milestone 3: Mobile Version

**Status:** Complete
**Completed:** 2026-06-25
**Phases:** 12–19 complete

### What Shipped

- **Phase 12:** Mobile infrastructure — `MobileDetectionMiddleware` + `IViewLocationExpander` + `_Layout.Mobile.cshtml` + `mobile.css`
- **Phase 13:** Core player views — quest board card list + quest details mobile
- **Phase 14:** Calendar — iPhone-style agenda view replacing 7-column desktop grid
- **Phase 15:** DM views — Quest Create, Manage, and DM Profile on mobile
- **Phase 16:** Account & browse — Login, Register, Profile, Shop, Guild Members mobile
- **Phase 17:** Character & player views — GuildMembers/Details, Create, Edit, Players/Index mobile
- **Phase 18:** DM editing & secondary quest views — Quest/Edit, CreateFollowUp, QuestLog/Details, DM EditProfile mobile
- **Phase 19:** Admin & shop management views — Shop/Details, Admin/*, ShopManagement/* mobile

---

## v2.0 — Milestone 3: Omphalos Integration

**Status:** In progress
**Started:** 2026-06-18

### Goal

DMs can open Omphalos session notes for any quest with one click — navigated automatically into the correct session, authenticated via a short-lived signed token.

### Phases

- Phase 10: Admin Settings (Quest Board)
- Phase 11: External Tool Navigation + Deep Link (Quest Board)
- Phase 12: SSO Endpoint + Quest-Session Linking (Omphalos)

---
*Updated: 2026-06-25*
