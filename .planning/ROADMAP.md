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
- [x] **Phase 17: Character & Player Views** - GuildMembers/Details, Create, Edit, and Players/Index mobile views *(completed 2026-06-25: all 4 plans done — 122 integration tests pass)*
- [x] **Phase 18: DM Editing & Secondary Quest Views** - Quest/Edit, Quest/CreateFollowUp, QuestLog/Details, and DungeonMaster/EditProfile mobile views *(completed 2026-06-25: all 4 views + integration tests — 126 integration tests pass)*
- [x] **Phase 19: Admin & Shop Management Views** - Shop/Details, all Admin/*, and all ShopManagement/* mobile views (completed 2026-06-25)

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
| 17. Character & Player Views | 4/4 | Complete | 2026-06-25 |
| 18. DM Editing & Secondary Quest Views | 5/5 | Complete | 2026-06-25 |
| 19. Admin & Shop Management Views | 7/7 | Complete   | 2026-06-25 |

### Phase 17: Character & Player Views

**Goal:** Players can view character details, create new characters, edit existing characters, and browse the player list on a phone without layout breakage
**Depends on:** Phase 12 (mobile infrastructure)
**Requirements**: CHAR-01, CHAR-02, CHAR-03, PLAYER-01
**Success Criteria** (what must be TRUE):

  1. The Guild Member detail page on mobile shows character stats, profile photo, class/level, and backstory in a single-column layout without overflow
  2. The Create Character and Edit Character forms are single-column on mobile with all fields reachable by vertical scroll and inputs at minimum 44px height
  3. The Players list on mobile is a readable single-column list with no horizontal scrolling

**Plans:** 4/4 plans executed

Plans:

- [x] 17-01-PLAN.md — Add CHAR-01..03 + PLAYER-01 to REQUIREMENTS.md and append Phase 17 integration test stubs (Wave 1)
- [x] 17-02-PLAN.md — Create GuildMembers/Details.Mobile.cshtml and character-detail.mobile.css (Wave 2)
- [x] 17-03-PLAN.md — Create GuildMembers/Create.Mobile.cshtml, Edit.Mobile.cshtml, and character-form.mobile.css (Wave 2)
- [x] 17-04-PLAN.md — Create Players/Index.Mobile.cshtml, players.mobile.css, and remove email column from desktop Players/Index.cshtml (Wave 2)

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

**Plans:** 5/5 plans executed

Plans:
**Wave 1**

- [x] 18-01-PLAN.md — Quest/Edit.Mobile.cshtml + quest-edit.mobile.css (Wave 1)
- [x] 18-02-PLAN.md — Quest/CreateFollowUp.Mobile.cshtml + quest-followup.mobile.css (Wave 1)
- [x] 18-03-PLAN.md — DungeonMaster/EditProfile.Mobile.cshtml + dm-editprofile.mobile.css (Wave 1)
- [x] 18-04-PLAN.md — QuestLog/Details.Mobile.cshtml + quest-log-detail.mobile.css (Wave 1)

**Wave 2**

- [x] 18-05-PLAN.md — Integration tests for all 4 Phase 18 mobile views (Wave 2)

---

### Phase 19: Admin & Shop Management Views

**Goal:** Admins can manage users, quests, and shop items from a phone; the Shop item detail page is readable on small screens
**Depends on:** Phase 12 (mobile infrastructure)
**Requirements**: ADMIN-01, ADMIN-02, SHOPMGMT-01
**Success Criteria** (what must be TRUE):

  1. The Admin user list and edit pages are usable on mobile without horizontal scrolling
  2. The Shop Management index, create, and edit pages are fully functional on mobile
  3. The Shop item detail page renders in a single-column layout with no overflow

**Plans:** 7/7 plans complete

Plans:
**Wave 1**

- [x] 19-01-PLAN.md — Add ADMIN-01/ADMIN-02/SHOPMGMT-01 to REQUIREMENTS.md + RED integration test stubs

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 19-02-PLAN.md — Admin Users + Admin Quests mobile list views (delete-fetch JS)
- [x] 19-03-PLAN.md — Admin EditUser + ResetPassword mobile forms (shared admin-form CSS)
- [x] 19-04-PLAN.md — ShopManagement Create + Edit mobile forms (pricing JS, button-free price)
- [x] 19-05-PLAN.md — ShopManagement Index mobile flat list (modals, auth-guarded actions)
- [x] 19-06-PLAN.md — Shop/Details mobile view (inlined non-modal content, toasts)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 19-07-PLAN.md — Phase integration test gate (all 8 mobile-view tests GREEN)

---

# Roadmap: D&D Quest Board — Milestone 4: Email Notifications

## Overview

Milestone 4 adds three capabilities to the existing application: styled HTML email templates for all outbound notifications, automated and DM-triggered session reminders via Hangfire, and an admin email stats dashboard backed by the Resend REST API. The existing email delivery path (SmtpClient → Postfix → Resend SMTP relay) is unchanged — only the template rendering and job orchestration layers are new.

The critical infrastructure decision is `HtmlRenderer` (built into .NET 10) for template rendering inside background jobs — `IRazorViewEngine` throws `NullReferenceException` in that context and must not be used. Every Hangfire job resolves scoped services via `IServiceScopeFactory` to avoid DbContext lifetime violations.

## Phases

**Phase Numbering:**
Continues from Milestone 3 (Phases 12–19). Milestone 4 Email Notifications starts at Phase 20.

- [x] **Phase 20: Hangfire Infrastructure** - Install Hangfire with SQL Server storage, expose admin-only dashboard at `/hangfire`, and establish the `IServiceScopeFactory` pattern all subsequent jobs must follow (completed 2026-06-25)
- [x] **Phase 21: HTML Email Templates** - Implement `IEmailRenderService` backed by `HtmlRenderer`, upgrade quest-finalization email to styled HTML with deduplication, and add the single-quest reminder template (completed 2026-06-26)
- [ ] **Phase 22: Session Reminders** - Add `ReminderSentAt` idempotency column, implement the daily recurring reminder job and DM manual trigger, with digest batching for players on multi-quest days
- [ ] **Phase 23: Admin Email Stats** - Add admin-only stats dashboard pulling live sent/bounced/failed counts from the Resend REST API

## Phase Details

### Phase 20: Hangfire Infrastructure

**Goal**: Hangfire is running, its dashboard is accessible only to Admin users, and the `IServiceScopeFactory` pattern is established as the mandatory contract for all subsequent job implementations
**Depends on**: Nothing (first Milestone 4 phase)
**Requirements**: JOBS-01, JOBS-02
**Success Criteria** (what must be TRUE):

  1. Visiting `/hangfire` as an Admin user loads the Hangfire dashboard; visiting it as a non-admin or unauthenticated user returns a 401 or redirect — never the dashboard content
  2. The Hangfire dashboard registration appears in `Program.cs` after `UseAuthentication` and `UseAuthorization` — verifiable by code review (Docker-proxied requests would bypass `LocalRequestsOnlyAuthorizationFilter`, so a custom `IDashboardAuthorizationFilter` is used)
  3. A smoke-test Hangfire job that resolves a scoped service via `IServiceScopeFactory` enqueues and completes without exception, confirming the scope pattern works before any real job is wired
  4. The application starts and all existing integration tests pass — Hangfire adds the `[HangFire]` schema to the database without any EF Core migration

**Plans**: 4/4 plans complete
Plans:

- [x] 20.1-01-PLAN.md

- [x] 20-01-PLAN.md — Install Hangfire packages, create AdminDashboardAuthFilter, create SmokeTestJob (Wave 1)
- [x] 20-02-PLAN.md — Add Background Jobs nav link to Admin dropdown in _Layout.cshtml (Wave 1)
- [x] 20-03-PLAN.md — Wire Hangfire into Program.cs, middleware ordering, regression gate (Wave 2)

### Phase 21: HTML Email Templates

**Goal**: All outbound emails render as styled HTML; the quest-finalization email is upgraded and protected against duplicate sends on re-finalization; the single-quest reminder template is ready for Phase 22 to consume
**Depends on**: Phase 20
**Requirements**: EMAIL-01, EMAIL-02, EMAIL-03
**Success Criteria** (what must be TRUE):

  1. Finalizing a quest sends a styled HTML email (not plain text) to all confirmed players; the email renders correctly in a standard email client
  2. Re-opening and re-finalizing a quest for the same confirmed date does not send a second finalization email to players — the `FinalizedEmailSentForDate` column on the Quest entity (added via EF Core migration) prevents the duplicate
  3. The `IEmailRenderService` interface is defined in Domain and backed by `RazorEmailRenderService` in Service; the implementation uses `HtmlRenderer` — not `IRazorViewEngine` — and can be invoked from a background job context without throwing `NullReferenceException`
  4. A shared email layout Razor component (`_EmailLayout.razor`) and a quest-finalization Razor component (`QuestFinalized.razor`) exist and are used by the finalization send path
  5. A single-quest session reminder Razor component (`SessionReminder.razor`) renders a complete HTML email that Phase 22 can use without modification

**Plans**: 4 plans in 4 waves

Plans:
**Wave 1**

- [x] 21-01-PLAN.md — Foundation: EF migration, entity/model/repo changes, IEmailRenderService interface, IEmailService.SendAsync (Wave 1)

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 21-02-PLAN.md — Razor email components: _EmailLayout, QuestFinalized, QuestDateChanged, SessionReminder (Wave 2)

**Wave 3** *(blocked on Wave 1+2 completion)*

- [x] 21-03-PLAN.md — Jobs + QuestService rewire: RazorEmailRenderService, QuestFinalizedEmailJob, QuestDateChangedEmailJob, SmokeTestJob removal, DI registration (Wave 3)

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 21-04-PLAN.md — Test fixes + regression gate: update QuestServiceTests, run full suite GREEN (Wave 4)

**UI hint**: yes

### Phase 22: Session Reminders

**Goal**: Players are automatically reminded of their confirmed quests 24 hours before the session, with digest batching for multi-quest days, idempotent retry behavior, and a DM manual trigger option
**Depends on**: Phase 21
**Requirements**: EMAIL-04, REMIND-01, REMIND-02, REMIND-03, REMIND-04
**Success Criteria** (what must be TRUE):

  1. A Hangfire recurring job runs daily at 09:00 and sends reminder emails to all players confirmed for quests whose `FinalizedDate` falls the following day — verified by checking Hangfire's recurring job list in the dashboard
  2. A player confirmed for two quests on the same day receives exactly one combined digest email listing both quests, not two separate emails
  3. A DM tapping the "Send Reminder" button on the quest manage page enqueues a Hangfire background job; the job completes and the player receives a reminder email
  4. If the Hangfire job retries after a partial failure, players who already received a reminder (tracked via `ReminderSentAt` on the quest or a `ReminderLog` table, added via EF Core migration) are not emailed again
  5. The date comparison in the reminder job accounts for the `FinalizedDate` timezone storage convention (UTC vs. local verified before implementation) so no quests are missed or triggered a day early

**Plans**: TBD

### Phase 23: Admin Email Stats

**Goal**: Admin users can see live email delivery health (sent, delivered, bounced, failed) pulled from the Resend API, without adding the Resend SDK
**Depends on**: Nothing (fully independent of Phases 21–22; requires only Phase 20 for the admin auth pattern)
**Requirements**: STATS-01
**Success Criteria** (what must be TRUE):

  1. An Admin user can navigate to the email stats page and see counts for sent, delivered, bounced, and failed emails — pulled live from the Resend API
  2. The stats are fetched via a typed `HttpClient` calling `GET https://api.resend.com/emails` with a Bearer token; no Resend SDK package is added to the project
  3. The `ResendApiKey` is read from `EmailSettings` in `appsettings.json`; the page renders an actionable error message if the key is missing or the API returns an error (not an unhandled exception)
  4. A non-admin user cannot access the stats page (protected by the existing `"AdminOnly"` authorization policy)

**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 20 → 21 → 22 → 23
Note: Phase 23 is fully independent of Phases 21–22 and can be executed in any order after Phase 20 completes. The critical path is 20 → 21 → 22.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 20. Hangfire Infrastructure | 4/4 | Complete    | 2026-06-25 |
| 21. HTML Email Templates | 4/4 | Complete   | 2026-06-26 |
| 22. Session Reminders | 0/TBD | Not started | - |
| 23. Admin Email Stats | 0/TBD | Not started | - |
