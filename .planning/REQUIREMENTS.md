# Requirements: D&D Quest Board — Milestone 3: Mobile Version

**Defined:** 2026-06-23
**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## v1 Requirements

### Infrastructure

- [x] **INFRA-01**: A `MobileDetectionMiddleware` detects mobile user agents per request and stores the result in `HttpContext.Items["IsMobile"]`
- [x] **INFRA-02**: A `MobileViewLocationExpander` is registered; on mobile requests the view engine checks `ViewName.Mobile.cshtml` before `ViewName.cshtml`; desktop requests are unaffected and desktop views are not modified *(backend half — expander + registration complete; end-to-end proof in Plan 02)*
- [x] **INFRA-03**: Mobile detection logic lives in `PopulateValues` (not `ExpandViewLocations`) so the view path cache correctly separates mobile and desktop entries
- [ ] **INFRA-04**: `_Layout.Mobile.cshtml` provides the mobile HTML shell with a Bootstrap offcanvas navigation drawer
- [ ] **INFRA-05**: `_ViewStart.cshtml` selects `_Layout.Mobile.cshtml` for mobile requests and `_Layout.cshtml` for desktop — no individual mobile view sets its layout explicitly
- [ ] **INFRA-06**: `mobile.css` provides baseline touch target sizing (minimum 44px height), mobile typography scale, and spacing overrides

### Quest Board (Home Page)

- [x] **HOME-01**: On mobile, the quest board displays a vertical scrollable card list instead of the decorative poster/parchment image cards
- [x] **HOME-02**: Each quest entry shows: title, challenge rating, DM name, and status (Open / Finalized / Done); finalized entries include the date
- [x] **HOME-03**: Each quest entry is tap-navigable to Quest Details, or to Quest Manage for the signed-in DM's own quests
- [x] **HOME-04**: If the current user is already signed up for a quest, the entry shows a visual indicator (badge or icon — no wax seal imagery)

### Quest Views

- [x] **QVIEW-01**: Quest Details on mobile shows the quest description and date voting buttons (Yes / No / Maybe) as large tap-friendly controls (minimum 44px)
- [x] **QVIEW-02**: The participant table on Quest Details is replaced with a stacked list on mobile (player name + character name + role, one item per row)
- [x] **QVIEW-03**: Quest Log index on mobile displays past quests as a scannable list with title, date, and DM name

### Calendar

- [ ] **CAL-01**: The calendar on mobile shows an agenda/list view instead of the 7-column day grid
- [ ] **CAL-02**: Each agenda entry shows: day label (e.g. SATURDAY, JUNE 14), quest name, and time
- [ ] **CAL-03**: Days with no quests are skipped entirely — only days with at least one quest appear
- [ ] **CAL-04**: Tapping a quest entry in the agenda navigates to Quest Details for that quest
- [ ] **CAL-05**: The `_Calendar` partial used inside Quest Details renders as a vertical per-date list with tap-friendly Yes/No/Maybe vote buttons — replacing both the broken desktop grid (Choose a Date) and the Phase 13 simplified quest-level buttons (Update Your Vote)

### DM Views

- [ ] **DMVIEW-01**: Quest Create on mobile is a single-column scrollable form with touch-friendly date/time inputs and appropriately sized controls
- [ ] **DMVIEW-02**: Quest Manage on mobile lets DMs select or deselect players and finalize the quest without horizontal overflow
- [ ] **DMVIEW-03**: DM Profile page on mobile shows bio and photo in a single-column layout, readable without zooming

### Account Pages

- [x] **ACCT-01**: Login page on mobile is a full-width single-column form with large input fields and a clearly tappable submit button
- [x] **ACCT-02**: Register page on mobile is a full-width single-column form
- [x] **ACCT-03**: User Profile edit page is usable on small screens

### Browse Pages

- [x] **BROWSE-01**: Shop index on mobile displays items in a single-column scrollable list (or 2-column grid if space permits); filter and sort controls are accessible
- [x] **BROWSE-02**: Guild Members directory on mobile displays character cards in a single-column or 2-column layout

## Future Requirements

- Tablet-specific views (`.Tablet.cshtml`) — defer; phone-first scope sufficient for v1
- "Switch to desktop" cookie override — user-controlled escape hatch; natural extension of the expander but not needed for v1
- PWA manifest and service worker — offline support; separate initiative
- Admin views on mobile — low usage frequency; defer

## Out of Scope

- **No controller changes** — all controllers and action methods remain unchanged; ViewModels are shared with desktop views
- **No desktop view changes** — mobile views are purely additive; desktop experience is unchanged
- **No native mobile app** — this milestone delivers a mobile web experience, not an iOS/Android app
- **PWA features** — offline support, push notifications, and home screen installation are a separate milestone
- **Admin views** — admin actions are low-frequency and expected on desktop

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 12 | Complete (Plan 01) |
| INFRA-02 | Phase 12 | Complete (Plan 01, backend half) |
| INFRA-03 | Phase 12 | Complete (Plan 01) |
| INFRA-04 | Phase 12 | Complete (Plan 02) |
| INFRA-05 | Phase 12 | Complete (Plan 02) |
| INFRA-06 | Phase 12 | Complete (Plan 03) |
| HOME-01 | Phase 13 | Complete (Plan 02) |
| HOME-02 | Phase 13 | Complete (Plan 02) |
| HOME-03 | Phase 13 | Complete (Plan 02) |
| HOME-04 | Phase 13 | Complete (Plan 02) |
| QVIEW-01 | Phase 13 | Complete (Plan 03) |
| QVIEW-02 | Phase 13 | Complete (Plan 03) |
| QVIEW-03 | Phase 13 | Complete |
| CAL-01 | Phase 14 | Pending |
| CAL-02 | Phase 14 | Pending |
| CAL-03 | Phase 14 | Pending |
| CAL-04 | Phase 14 | Pending |
| CAL-05 | Phase 14 | Pending |
| DMVIEW-01 | Phase 15 | Pending |
| DMVIEW-02 | Phase 15 | Pending |
| DMVIEW-03 | Phase 15 | Pending |
| ACCT-01 | Phase 16 | Complete |
| ACCT-02 | Phase 16 | Complete |
| ACCT-03 | Phase 16 | Complete |
| BROWSE-01 | Phase 16 | Complete |
| BROWSE-02 | Phase 16 | Complete |
