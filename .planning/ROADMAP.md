# Roadmap: D&D Quest Board — Milestone 3: Mobile Version

## Overview

Milestone 3 delivers a purpose-built mobile experience by adding `.Mobile.cshtml` view variants alongside all existing desktop views. No controllers, ViewModels, repositories, or domain services are modified — every change is strictly additive to the Service layer's Views directory and static assets. A single middleware + view-expander infrastructure block (Phase 12) must land first; once in place each subsequent phase delivers independently verifiable mobile views.

## Phases

**Phase Numbering:**
Continues from previous milestones (Phases 1–9: Milestone 2; Phases 10–11: Milestone 3 Omphalos Integration). Milestone 3 Mobile Version starts at Phase 12.
- Integer phases (12, 13, 14…): Planned milestone work
- Decimal phases (12.1, 12.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 12: Mobile Infrastructure** - Wire mobile detection middleware, view-location expander, mobile layout shell, and mobile.css baseline — zero user-visible change until mobile views are added *(Plan 01 complete: middleware + expander + registration; Plan 02 complete: _Layout.Mobile.cshtml + _ViewStart conditional routing + integration tests; Plan 03 complete: mobile.css 44px touch targets + MobileCssTests — Phase 12 fully done)*
- [x] **Phase 13: Core Player Views** - Quest board and quest detail pages on mobile with tap-friendly card list, voting controls, and quest log (completed 2026-06-24)
- [ ] **Phase 14: Calendar** - Agenda/list view replacing the 7-column desktop grid — the highest-complexity structural adaptation in this milestone
- [ ] **Phase 15: DM Views** - Quest Create, Quest Manage, and DM Profile pages adapted for touch-screen input
- [ ] **Phase 16: Account & Browse** - Login, Register, Profile, Shop, and Guild Members pages usable on small screens

## Phase Details

### Phase 12: Mobile Infrastructure
**Goal**: The mobile detection pipeline is live; every mobile request routes through `_Layout.Mobile.cshtml` and can resolve `.Mobile.cshtml` view variants with no action required in individual views or controllers
**Depends on**: Nothing (first Milestone 3 Mobile phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05, INFRA-06
**Success Criteria** (what must be TRUE):
  1. Visiting any page from a mobile User-Agent (e.g. iPhone Safari) returns an HTML response that includes the offcanvas nav element; the same page from a desktop browser does not include that element
  2. A `.Mobile.cshtml` file placed alongside any existing view is served automatically on mobile requests and never served on desktop requests — without touching the view's controller action
  3. A desktop browser visiting any route sees no change to layout, markup, or styles compared to before Phase 12
  4. `mobile.css` is loaded on mobile pages; touch targets in that stylesheet are at minimum 44px in height
**Plans**: Plan 01 (complete: MobileDetectionMiddleware + MobileViewLocationExpander + Program.cs registration), Plan 02 (complete: _Layout.Mobile.cshtml + _ViewStart conditional routing + INFRA-02/04/05 integration tests), Plan 03 (complete: mobile.css baseline + MobileCssTests — all INFRA requirements satisfied)
**UI hint**: yes

### Phase 13: Core Player Views
**Goal**: Players browsing the quest board and checking quest details on a phone can read, navigate, and interact without pinching, zooming, or horizontal scrolling
**Depends on**: Phase 12
**Requirements**: HOME-01, HOME-02, HOME-03, HOME-04, QVIEW-01, QVIEW-02, QVIEW-03
**Success Criteria** (what must be TRUE):
  1. The quest board on mobile shows a vertical card list — no poster/parchment images — and each card displays title, challenge rating, DM name, and status
  2. A quest the logged-in player has signed up for shows a visible indicator (badge or icon) on its card; no wax seal imagery is used
  3. Tapping a quest card navigates to Quest Details; tapping a DM's own quest navigates to Quest Manage
  4. Quest Details voting buttons (Yes / No / Maybe) are at least 44px tall and spaced to avoid accidental taps
  5. The participant list on Quest Details renders as a stacked single-column list (name + character + role per row) rather than a horizontal table
**Plans**: Plan 01 (test stubs: MobileViewsTests.cs), Plan 02 (Home/Index.Mobile.cshtml + home.mobile.css), Plan 03 (Quest/Details.Mobile.cshtml + quests.mobile.css), Plan 04 (QuestLog/Index.Mobile.cshtml + quest-log.mobile.css)
**UI hint**: yes

### Phase 14: Calendar
**Goal**: Players can see which quests are scheduled this month on a phone without the 7-column grid overflowing the screen
**Depends on**: Phase 12
**Requirements**: CAL-01, CAL-02, CAL-03, CAL-04, CAL-05
**Success Criteria** (what must be TRUE):
  1. The mobile calendar renders as a vertical agenda list, not a 7-column day grid
  2. Each agenda entry shows a day label (e.g. SATURDAY, JUNE 14), the quest name, and the time
  3. Days with no quests are not rendered; only days that have at least one quest appear
  4. Tapping any quest entry in the agenda navigates to that quest's Details page
  5. The _Calendar partial used inside Quest Details renders as a vertical per-date list with tap-friendly Yes/No/Maybe vote buttons — replacing both the broken desktop grid (Choose a Date) and the Phase 13 simplified quest-level buttons (Update Your Vote)
**Notes**:
  - CAL-05 requires creating `Views/Shared/_Calendar.Mobile.cshtml` — the MobileViewLocationExpander serves it automatically on mobile; no controller changes needed
  - CAL-05 also requires replacing the custom 3-button block in `Details.Mobile.cshtml` (Update Your Vote section) with the same `@await Html.PartialAsync("_Calendar", calendarMonth)` call already used in the Choose a Date section
**Plans**: TBD
**UI hint**: yes

### Phase 15: DM Views
**Goal**: Dungeon Masters can create quests, manage player selection, and present their profile page entirely from a phone without form controls overflowing or requiring horizontal scroll
**Depends on**: Phase 12
**Requirements**: DMVIEW-01, DMVIEW-02, DMVIEW-03
**Success Criteria** (what must be TRUE):
  1. The Quest Create form on mobile is a single-column layout; date/time inputs use native touch pickers and all fields are reachable by vertical scroll only
  2. Quest Manage on mobile lets a DM select or deselect players and tap Finalize without any horizontal overflow
  3. The DM Profile page on mobile shows bio text and the profile photo in a single-column layout at a readable font size without requiring zoom
**Plans**: TBD
**UI hint**: yes

### Phase 16: Account & Browse
**Goal**: Players can log in, register, edit their profile, browse the shop, and view the guild directory from a phone without layout breakage
**Depends on**: Phase 12
**Requirements**: ACCT-01, ACCT-02, ACCT-03, BROWSE-01, BROWSE-02
**Success Criteria** (what must be TRUE):
  1. The Login and Register forms on mobile are full-width single-column; input fields and the submit button are clearly tappable (minimum 44px height)
  2. The User Profile edit page is scrollable and all fields are editable on a small screen
  3. The Shop index on mobile displays items in a single-column or 2-column list; filter and sort controls are accessible without horizontal scrolling
  4. The Guild Members directory on mobile displays character cards in a single-column or 2-column layout with no overflow
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 12 → 13 → 14 → 15 → 16
Note: Phases 13, 14, 15, and 16 are all independent of each other — each depends only on Phase 12 and can be executed in any order after Phase 12 completes.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 12. Mobile Infrastructure | 3/3 | Complete | 2026-06-24 |
| 13. Core Player Views | 4/4 | Complete   | 2026-06-24 |
| 14. Calendar | 0/? | Not started | - |
| 15. DM Views | 0/? | Not started | - |
| 16. Account & Browse | 0/? | Not started | - |
