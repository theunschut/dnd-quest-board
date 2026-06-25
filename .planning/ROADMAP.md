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
- [x] **Phase 14: Calendar** - Agenda/list view replacing the 7-column desktop grid — the highest-complexity structural adaptation in this milestone *(completed 2026-06-24: agenda view + CSS + vote partial + Details update — all 107 tests pass)*
- [x] **Phase 15: DM Views** - Quest Create, Quest Manage, and DM Profile pages adapted for touch-screen input *(completed 2026-06-24: Create + Manage + Profile mobile views — all 110 tests pass)*
- [x] **Phase 16: Account & Browse** - Login, Register, Profile, Shop, and Guild Members pages usable on small screens *(completed 2026-06-25: all 5 account views + shop + guild members — 50 integration tests pass)*
- [ ] **Phase 17: Character & Player Views** - GuildMembers/Details, Create, Edit, and Players/Index mobile views
- [ ] **Phase 18: DM Editing & Secondary Quest Views** - Quest/Edit, Quest/CreateFollowUp, QuestLog/Details, and DungeonMaster/EditProfile mobile views
- [ ] **Phase 19: Admin & Shop Management Views** - Shop/Details, all Admin/*, and all ShopManagement/* mobile views

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

**Plans**: 3 plans
Plans:

- [x] 14-01-PLAN.md — Add CAL-05 to REQUIREMENTS.md and create integration test stubs (Wave 1)
- [x] 14-02-PLAN.md — Create calendar.mobile.css and Calendar/Index.Mobile.cshtml (Wave 2)
- [x] 14-03-PLAN.md — Create _Calendar.Mobile.cshtml, update Details.Mobile.cshtml, append quests.mobile.css (Wave 2)

**UI hint**: yes

### Phase 15: DM Views

**Goal**: Dungeon Masters can create quests, manage player selection, and present their profile page entirely from a phone without form controls overflowing or requiring horizontal scroll
**Depends on**: Phase 12
**Requirements**: DMVIEW-01, DMVIEW-02, DMVIEW-03
**Success Criteria** (what must be TRUE):

  1. The Quest Create form on mobile is a single-column layout; date/time inputs use native touch pickers and all fields are reachable by vertical scroll only
  2. Quest Manage on mobile lets a DM select or deselect players and tap Finalize without any horizontal overflow
  3. The DM Profile page on mobile shows bio text and the profile photo in a single-column layout at a readable font size without requiring zoom

**Plans**: 4 plans
Plans:

- [x] 15-01-PLAN.md — Add DMVIEW integration test stubs to MobileViewsTests.cs (Wave 1)
- [x] 15-02-PLAN.md — Create Quest/Create.Mobile.cshtml and dm-create.mobile.css (Wave 2)
- [x] 15-03-PLAN.md — Create Quest/Manage.Mobile.cshtml and dm-manage.mobile.css (Wave 2)
- [x] 15-04-PLAN.md — Create DungeonMaster/Profile.Mobile.cshtml and dm-profile.mobile.css (Wave 2)

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

**Plans**: 4/4 plans executed
Plans:
**Wave 1**

- [x] 16-01-PLAN.md — Integration test stubs for ACCT-01..03 + BROWSE-01..02 in MobileViewsTests.cs (Wave 1)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 16-02-PLAN.md — Account mobile views (Login, Register, Edit, Profile, ChangePassword) + account.mobile.css (Wave 2)
- [x] 16-03-PLAN.md — Shop/Index.Mobile.cshtml + shop.mobile.css (Wave 2)
- [x] 16-04-PLAN.md — GuildMembers/Index.Mobile.cshtml + guild-members.mobile.css (Wave 2)

**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 12 → 13 → 14 → 15 → 16
Note: Phases 13, 14, 15, and 16 are all independent of each other — each depends only on Phase 12 and can be executed in any order after Phase 12 completes.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 12. Mobile Infrastructure | 3/3 | Complete | 2026-06-24 |
| 13. Core Player Views | 4/4 | Complete   | 2026-06-24 |
| 14. Calendar | 3/3 | Complete | 2026-06-24 |
| 15. DM Views | 4/4 | Complete | 2026-06-24 |
| 16. Account & Browse | 4/4 | Complete | 2026-06-25 |
| 17. Character & Player Views | 3/4 | In Progress|  |
| 18. DM Editing & Secondary Quest Views | 0/0 | Not started | - |
| 19. Admin & Shop Management Views | 0/0 | Not started | - |

### Phase 17: Character & Player Views

**Goal:** Players can view character details, create new characters, edit existing characters, and browse the player list on a phone without layout breakage
**Depends on:** Phase 12 (mobile infrastructure)
**Requirements**: CHAR-01, CHAR-02, CHAR-03, PLAYER-01
**Success Criteria** (what must be TRUE):

  1. The Guild Member detail page on mobile shows character stats, profile photo, class/level, and backstory in a single-column layout without overflow
  2. The Create Character and Edit Character forms are single-column on mobile with all fields reachable by vertical scroll and inputs at minimum 44px height
  3. The Players list on mobile is a readable single-column list with no horizontal scrolling

**Plans:** 3/4 plans executed

Plans:
- [x] 17-01-PLAN.md — Add CHAR-01..03 + PLAYER-01 to REQUIREMENTS.md and append Phase 17 integration test stubs (Wave 1)
- [x] 17-02-PLAN.md — Create GuildMembers/Details.Mobile.cshtml and character-detail.mobile.css (Wave 2)
- [ ] 17-03-PLAN.md — Create GuildMembers/Create.Mobile.cshtml, Edit.Mobile.cshtml, and character-form.mobile.css (Wave 2)
- [ ] 17-04-PLAN.md — Create Players/Index.Mobile.cshtml, players.mobile.css, and remove email column from desktop Players/Index.cshtml (Wave 2)

---

### Phase 18: DM Editing & Secondary Quest Views

**Goal:** Dungeon Masters can edit quests, create follow-up quests, and edit their DM profile on a phone; players can view individual quest log entries — all without layout breakage or horizontal scrolling
**Depends on:** Phase 12 (mobile infrastructure)
**Requirements**: DMVIEW-04, DMVIEW-05, DMVIEW-06, QLOG-01
**Success Criteria** (what must be TRUE):

  1. The Quest Edit form on mobile is a single-column layout with all fields reachable by vertical scroll
  2. The Create Follow-Up Quest form pre-fills existing player list and is usable on a small screen
  3. The DM Edit Profile page (bio, photo upload) is fully functional on mobile with no overflow
  4. The Quest Log detail page renders the quest summary and player list in a single-column layout

**Plans:** 0 plans (run /gsd-plan-phase 18 to break down)

Plans:
- [ ] TBD

---

### Phase 19: Admin & Shop Management Views

**Goal:** Admins can manage users, quests, and shop items from a phone; the Shop item detail page is readable on small screens
**Depends on:** Phase 12 (mobile infrastructure)
**Requirements**: ADMIN-01, ADMIN-02, SHOPMGMT-01
**Success Criteria** (what must be TRUE):

  1. The Admin user list and edit pages are usable on mobile without horizontal scrolling
  2. The Shop Management index, create, and edit pages are fully functional on mobile
  3. The Shop item detail page renders in a single-column layout with no overflow

**Plans:** 0 plans (run /gsd-plan-phase 19 to break down)

Plans:
- [ ] TBD
