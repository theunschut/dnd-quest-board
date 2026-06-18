# Roadmap: D&D Quest Board — Milestone 2: Refactor + Feature Expansion

## Overview

Milestone 2 fixes the codebase's accumulated architectural drift before adding new features. The first four phases restore correct layer boundaries, consolidate business logic into services, remove dead code, and close security gaps. The remaining four phases deliver the four backlog features (shop filter/sort, follow-up quest, DM profile page, avatar crop) onto the now-clean architecture.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3...): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Layer Dependency Fix** - Restore correct compile-time dependency direction: Domain compiles without referencing Repository
- [x] **Phase 2: Email & Service Consolidation** - Move email dispatch and finalization logic into services; introduce typed email options
- [x] **Phase 3: Code Quality & Dead Code** - Remove dead code, fix naming, replace magic numbers with named references
- [x] **Phase 4: Security Hardening** - Enable account lockout, raise password minimum, remove HasKey from user-facing edit, clean .env from git
- [x] **Phase 5: Shop Filter & Sort** - Let players filter and sort shop items by rarity and price without a JS dependency
 (completed 2026-04-21)
- [x] **Phase 6: Follow-Up Quest** - Let DMs create a part-2 quest from a finalized quest with players pre-approved (completed 2026-06-16)
- [x] **Phase 7: DM Profile Page** - Give each DM a browsable profile with photo and bio; admin can edit any DM's profile (completed 2026-06-17)
- [ ] **Phase 8: Profile Picture Avatar Crop** - Let players crop their character portrait to a square avatar used on the guild directory

## Phase Details

### Phase 1: Layer Dependency Fix
**Goal**: The Domain project compiles and passes all tests without any reference to the Repository project; the correct dependency direction (Service → Domain ← Repository) is enforced at build time
**Depends on**: Nothing (first phase)
**Requirements**: ARCH-01, ARCH-02, ARCH-03, ARCH-04
**Success Criteria** (what must be TRUE):
  1. `EuphoriaInn.Domain.csproj` contains no `<ProjectReference>` to `EuphoriaInn.Repository`
  2. `EntityProfile.cs` (AutoMapper Entity↔DomainModel mappings) lives in `EuphoriaInn.Repository`, not `EuphoriaInn.Domain`
  3. `dotnet build` on the solution succeeds with zero errors
  4. `Program.cs` registers AutoMapper profiles by explicit type reference — no assembly scanning
**Plans**: 2 plans
Plans:
- [x] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [x] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference

### Phase 2: Email & Service Consolidation
**Goal**: Quest finalization and date-change notifications are fully handled inside services; controllers receive a `ServiceResult` and render; email configuration uses the typed options pattern
**Depends on**: Phase 1
**Requirements**: CTRL-01, CTRL-02, CTRL-03, CTRL-04, EMAIL-01, EMAIL-02, EMAIL-03, EMAIL-04
**Success Criteria** (what must be TRUE):
  1. A DM can finalize a quest and all selected players receive the correct notification email — without `QuestController` injecting `IEmailService`
  2. When a DM changes quest dates, players receive a notification email that contains the real application URL (not a placeholder)
  3. `QuestController`'s finalize action is 20 lines or fewer
  4. `ShopController.Index` contains no remaining-quantity calculation logic
  5. `EmailService` reads all SMTP settings from a single `IOptions<EmailSettings>` injection point
**Plans**: 3 plans
Plans:
- [x] 02-01-PLAN.md — Foundation types: EmailSettings record, ServiceResult<T>, AppUrl config, EmailService IOptions refactor
- [x] 02-02-PLAN.md — QuestService email consolidation: inject IEmailService, slim QuestController, post-save re-fetch
- [x] 02-03-PLAN.md — ShopService remaining-quantity extraction: slim ShopController.Index

### Phase 3: Code Quality & Dead Code
**Goal**: The codebase contains no dead methods, no magic numbers in signup logic, and no misleading file or class names
**Depends on**: Phase 2
**Requirements**: QUAL-01, QUAL-02, QUAL-03, QUAL-04, QUAL-05
**Success Criteria** (what must be TRUE):
  1. `SecurityConfiguration.cs` does not exist; the `Security` key is absent from `appsettings.json`
  2. `IQuestService` and `QuestService` contain no `UpdateQuestPropertiesAsync` method (non-notification variant)
  3. No `SignupRole == 1` literal appears anywhere in service code; the named enum reference is used throughout
  4. The 30-minute `IsSameDateTime` comparison window is a named constant with an explanatory comment
  5. `CharacterViewModels/GuildMembersIndexViewModel.cs` has been renamed to `CharactersIndexViewModel.cs`
**Plans**: 2 plans
Plans:
- [x] 03-01-PLAN.md — Remove dead SecurityConfiguration + Security appsettings block (QUAL-01); remove dead UpdateQuestPropertiesAsync from 4 layers (QUAL-02)
- [x] 03-02-PLAN.md — Replace SignupRole magic number + extract 30-min IsSameDateTime constant (QUAL-03+04); rename GuildMembersIndexViewModel.cs to CharactersIndexViewModel.cs (QUAL-05)

### Phase 4: Security Hardening
**Goal**: Failed login attempts are rate-limited with lockout, the minimum password length meets the 8-character standard, HasKey is admin-only, the Password property is removed from the domain model, and .env is not tracked by git
**Depends on**: Phase 3
**Requirements**: SEC-01, SEC-02, SEC-03, SEC-04, SEC-05, SEC-06
**Success Criteria** (what must be TRUE):
  1. After 5 consecutive failed login attempts, a user account is locked out for 15 minutes
  2. All existing users in `AspNetUsers` have `LockoutEnabled = 1` (applied via EF Core migration)
  3. Registering with a password shorter than 8 characters is rejected with a validation error
  4. The `Account/Edit` page does not show or accept a HasKey field; editing HasKey requires the Admin panel
  5. The `User` domain model has no `Password` property
  6. `.env` is listed in `.gitignore`; only `.env.example` with placeholder values is tracked
**Plans**: 4 plans
Plans:
- [x] 04-01-PLAN.md — Identity lockout config + raise password minimum to 8 (SEC-01, SEC-03)
- [x] 04-02-PLAN.md — Remove HasKey from user-facing Edit + remove Password from User domain model (SEC-04, SEC-05)
- [x] 04-03-PLAN.md — EF Core migration backfilling LockoutEnabled = 1 for existing users (SEC-02)
- [x] 04-04-PLAN.md — Add .env to .gitignore and untrack (SEC-06)

### Phase 5: Shop Filter & Sort
**Goal**: Players can narrow the shop to items of specific rarities and reorder by price without any client-side JavaScript dependency
**Depends on**: Phase 4
**Requirements**: SHOP-01, SHOP-02, SHOP-03, SHOP-04
**Success Criteria** (what must be TRUE):
  1. A player can select one or more rarity values and the shop shows only matching items
  2. A player can sort items by price ascending or price descending
  3. After applying a filter or sort, the URL reflects the chosen parameters so the page can be bookmarked or shared
  4. Filtering and sorting work correctly with JavaScript disabled in the browser
**Plans**: 2 plans
Plans:
- [x] 05-01-PLAN.md — Wave 0 test scaffolding + backend: extend TestDataHelper + Shop integration tests, extend ShopIndexViewModel and ShopController.Index with rarity filter and price sort (SHOP-01, SHOP-02, SHOP-03, SHOP-04)
- [x] 05-02-PLAN.md — View wiring: filter-row form, BuildTabUrl helper on category tabs, filter-aware empty state, shop.css additions, render-assertion integration test (SHOP-01, SHOP-02, SHOP-03, SHOP-04)
**UI hint**: yes

### Phase 6: Follow-Up Quest
**Goal**: DMs can create a part-2 quest directly from a finalized quest's Manage page, with original players pre-approved and the link visible on both quests
**Depends on**: Phase 3
**Requirements**: FOLLOW-01, FOLLOW-02, FOLLOW-03, FOLLOW-04, FOLLOW-05
**Success Criteria** (what must be TRUE):
  1. A DM sees a "Create Follow-Up Quest" button on a finalized quest's Manage page
  2. The follow-up quest creation form pre-fills all players from the original quest as pre-approved signups
  3. The follow-up quest cannot be saved without a new date selected
  4. The original quest's detail page shows a link to the follow-up quest, and the follow-up links back to the original
  5. An EF Core migration adds a nullable `OriginalQuestId` self-referential foreign key to `QuestEntity`
**Plans**: 2 plans
Plans:
**Wave 1**
- [x] 06-01-PLAN.md — Data + service layer: QuestEntity/Quest OriginalQuestId FK, EntityProfile, QuestBoardContext, EF migration, IQuestService + QuestService CreateFollowUpQuestAsync (FOLLOW-04, FOLLOW-05)

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 06-02-PLAN.md — Controller + views: CreateFollowUp GET/POST actions, FollowUpQuestViewModel, CreateFollowUp.cshtml, sidebar links on Details + Manage, Create Follow-Up button on Manage (FOLLOW-01, FOLLOW-02, FOLLOW-03, FOLLOW-04)
**UI hint**: yes

### Phase 7: DM Profile Page
**Goal**: Every DM has a browsable profile page with their photo and bio; DMs can update their own profile and admins can edit any DM's profile
**Depends on**: Phase 4
**Requirements**: DMPRO-01, DMPRO-02, DMPRO-03, DMPRO-04, DMPRO-05
**Success Criteria** (what must be TRUE):
  1. Navigating to `/DungeonMaster/Profile/{id}` shows the DM's name, photo, and bio
  2. A DM can update their own bio and upload a profile photo from the account area
  3. An admin can edit any DM's bio and photo from the admin panel
  4. The DM directory page links to each DM's individual profile
  5. An EF Core migration adds `Bio` (varchar 2000, nullable) and a `DungeonMasterProfileImage` table
**Plans**: 2 plans
Plans:
**Wave 1**
- [x] 07-01-PLAN.md — Data + service layer: DungeonMasterProfileEntity, DungeonMasterProfileImageEntity, domain model, interfaces, repository, service, AutoMapper profiles, DI registrations, GetQuestsByDungeonMasterAsync, EF migration AddDMProfileSystem (DMPRO-01, DMPRO-02, DMPRO-03, DMPRO-05)

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 07-02-PLAN.md — Web layer: Wave 0 test stubs, DungeonMasterController (Profile/EditProfile/GetDMProfilePicture), ViewModels, Views, dm-profile.css, navbar link, DM directory link (DMPRO-01, DMPRO-02, DMPRO-03, DMPRO-04)
**UI hint**: yes

### Phase 8: Profile Picture Avatar Crop
**Goal**: Players choose a square crop region when uploading a character portrait; the cropped avatar is served on the guild directory while the original is preserved on the character detail page
**Depends on**: Phase 4
**Requirements**: CROP-01, CROP-02, CROP-03, CROP-04, CROP-05
**Success Criteria** (what must be TRUE):
  1. When uploading a character portrait, the player sees a Cropper.js square crop selector before the image is submitted
  2. The original image bytes are never modified; only the four crop coordinates (`CropX`, `CropY`, `CropWidth`, `CropHeight`) are stored
  3. The `GetAvatarPicture` endpoint returns the server-side cropped version; `GetProfilePicture` continues to return the original
  4. The guild member directory uses the avatar (cropped) endpoint; the character detail page uses the original endpoint
  5. An EF Core migration adds the four crop coordinate columns to `CharacterImages`
**Plans**: 2 plans
Plans:
- TBD (phase not yet planned)
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8
Note: Phases 5, 6, 7, 8 are independent of each other (all depend on Phase 4 or Phase 3) and could run in any order.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Layer Dependency Fix | 2/2 | Complete | 2026-04-20 |
| 2. Email & Service Consolidation | 3/3 | Complete | 2026-04-20 |
| 3. Code Quality & Dead Code | 2/2 | Complete | 2026-04-20 |
| 4. Security Hardening | 4/4 | Complete | 2026-04-20 |
| 5. Shop Filter & Sort | 2/2 | Complete   | 2026-04-21 |
| 6. Follow-Up Quest | 2/2 | Complete | 2026-06-16 |
| 7. DM Profile Page | 2/2 | Complete | 2026-06-17 |
| 8. Profile Picture Avatar Crop | 0/? | Not started | - |
| 9. Shop Pagination | 2/2 | Complete | 2026-04-21 |

### Phase 9: Shop pagination — server-side paging to fix slow load from large item sets

**Goal**: The player-facing shop returns at most 12 items per database request via a unified paged repository method; URL-stacked server-side search (?search=) replaces the client-side filterShopItems JS; all filter/sort/search/page state carries cleanly across pager, category tabs, and the filter form
**Depends on**: Phase 5 (Shop Filter & Sort — URL-stacking pattern and BuildTabUrl helper)
**Requirements**: SHOP-PAG-01, SHOP-PAG-02, SHOP-PAG-03, SHOP-PAG-04, SHOP-PAG-05, SHOP-PAG-06, SHOP-PAG-07
**Success Criteria** (what must be TRUE):
  1. ShopController.Index executes exactly one database call via IShopService.GetPagedPublishedItemsAsync returning at most 12 items
  2. ?search=X filters items by Name OR Description server-side; stacks with type/rarity/sort/page
  3. The JS function filterShopItems is fully removed from site.js; no onkeyup/onchange/data-item-* hooks remain in Index.cshtml
  4. A Bootstrap 5 numbered pager renders when TotalPages > 1 with Previous/Next + current ±2 + ellipses
  5. Out-of-range ?page=9999 clamps to the last valid page; ShopIndexViewModel carries SearchQuery/CurrentPage/TotalPages/TotalItems/HasActiveSearch
**Plans**: 2 plans
Plans:
- [x] 09-01-PLAN.md — Wave 0 tests + paged repository/service method + ViewModel extension (SHOP-PAG-01, 02, 03, 07)
- [x] 09-02-PLAN.md — Controller + view wiring, Bootstrap 5 pager, server-side search input, legacy JS removal, enable skipped integration tests (SHOP-PAG-01, 03, 04, 05, 06, 07)
**UI hint**: yes

---

# Roadmap: D&D Quest Board — Milestone 3: Omphalos Integration

## Overview

Milestone 3 connects two independent apps (Quest Board on ASP.NET Core + SQL Server; Omphalos on .NET 10 Minimal API + PostgreSQL) via a browser-redirect SSO flow using a short-lived HMAC-SHA256 signed token. Quest Board generates a signed URL and issues a browser redirect; Omphalos receives and validates that redirect, auto-provisions the DM account on first use, finds or creates the quest session, and issues its JWT cookie. Zero net-new NuGet packages are required in either repo — the entire cryptographic layer uses BCL (System.Security.Cryptography.HMACSHA256).

Phase 10 (Admin Settings) is the sole blocker for Quest Board work: it supplies `IAdminSettingService` which both subsequent Quest Board phases depend on at compile time. Phase 12 (Omphalos SSO) has no code dependency on Quest Board and can begin in parallel once the token format contract is agreed. The two streams converge at end-to-end integration testing.

**NOTE: Phase 12 is implemented in the Omphalos repository at `C:\Repos\omphalos`, not this Quest Board repo.**

## Phases

- [x] **Phase 10: Admin Settings** - Admin can configure Omphalos URL and shared secret; settings persisted in DB and protected by AdminOnly policy (completed 2026-06-18)
- [ ] **Phase 11: Navigation + Token Generation** - DM navbar and quest pages show Omphalos links; clicking "Open Session Notes" generates a short-lived HMAC-signed deep link and redirects
- [ ] **Phase 12: SSO Endpoint + Quest-Session Linking (Omphalos repo)** - Omphalos validates Quest Board tokens, auto-provisions DM accounts, finds or creates quest sessions, and issues JWT cookies

## Phase Details

### Phase 10: Admin Settings
**Goal**: Admins can configure the Omphalos integration URL, shared secret, and enabled state from the Admin panel; settings are persisted in the database and take effect immediately without a restart
**Depends on**: Nothing (first phase of milestone)
**Requirements**: SETT-01, SETT-02, SETT-03, SETT-04, SETT-05, SETT-06, SETT-07, SETT-08
**Success Criteria** (what must be TRUE):
  1. An admin can navigate to a Settings page from the Admin navbar dropdown and see input fields for Omphalos URL and shared secret
  2. Saving the form with the secret field left blank preserves the existing secret — the existing value is not overwritten with an empty string
  3. Unchecking "Integration Enabled" causes all Omphalos buttons and links to disappear from the UI immediately; re-enabling makes them reappear
  4. The shared secret field renders as a password input (masked) on the settings page
  5. A non-admin user cannot access the Settings page (redirected or forbidden)
**Plans**: 2 plans
Plans:

**Wave 1**
- [x] 10-01-PLAN.md — Domain model + repository interface/impl + service interface/impl + DI registrations + EF migration + integration tests (SETT-06, SETT-08; IAdminSettingService foundation for Phase 11)

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 10-02-PLAN.md — AdminController Settings GET/POST + SettingsViewModel + Settings.cshtml view + navbar link + SETT-07 integration test (SETT-01, SETT-02, SETT-03, SETT-04, SETT-05, SETT-07)
**UI hint**: yes

### Phase 11: Navigation + Token Generation
**Goal**: DMs can open Omphalos from a navbar link (plain navigation) or from any quest page (signed deep link); clicking "Open Session Notes" generates a 5-minute HMAC-SHA256 token and redirects the browser directly to the correct Omphalos session
**Depends on**: Phase 10 (IAdminSettingService compile-time dependency)
**Requirements**: NAV-01, NAV-02, NAV-03, NAV-04, NAV-05, TOKEN-01, TOKEN-02, TOKEN-03, TOKEN-04, TOKEN-05
**Success Criteria** (what must be TRUE):
  1. A DM sees an "Open Omphalos" link in the navbar dropdown that opens the Omphalos base URL in a new tab — only when integration is enabled and OmphalosUrl is configured
  2. A DM sees an "Open Session Notes" button on Quest Detail and Quest Manage pages — only when integration is enabled, OmphalosUrl is set, and the current user is a DM or Admin
  3. Clicking "Open Session Notes" redirects the DM's browser to Omphalos with a signed URL containing questId, questTitle, username (lowercase), expiry (Unix timestamp), and HMAC-SHA256 signature
  4. The signed token expires after 300 seconds; a second click generates a new token with a fresh expiry
  5. When integration is disabled or OmphalosUrl is not configured, no Omphalos buttons or navbar links appear anywhere in the UI; the `LaunchOmphalos` endpoint returns 404
**Plans**: 2 plans
Plans:

**Wave 1**
- [x] 11-01-PLAN.md — Domain token service: IIntegrationTokenService + IntegrationTokenService (HMAC-SHA256) + QuestController constructor extension + LaunchOmphalos action + ViewBag.ShowOmphalosButton + unit + integration tests (TOKEN-01, TOKEN-02, TOKEN-03, TOKEN-04, TOKEN-05, NAV-03, NAV-04, NAV-05)

**Wave 2** *(blocked on Wave 1 completion)*
- [x] 11-02-PLAN.md — View wiring: OmphalosNavItemViewComponent + Default.cshtml + _Layout.cshtml DM dropdown + Details.cshtml DM Controls button + Manage.cshtml Session Notes card (NAV-01, NAV-02, NAV-03, NAV-04, NAV-05)
**UI hint**: yes

### Phase 12: SSO Endpoint + Quest-Session Linking
**REPO: Omphalos — `C:\Repos\omphalos` (NOT this Quest Board repo)**
**Goal**: Omphalos accepts Quest Board SSO redirects, validates the HMAC-signed token, auto-provisions a DM account on first use, finds or creates the quest session, and issues its JWT cookie — Omphalos continues to function as a standalone app when the env var is absent
**Depends on**: Token format contract agreed (can develop in parallel with Phase 11; converges at end-to-end test)
**Requirements**: SSO-01, SSO-02, SSO-03, SSO-04, SSO-05, SSO-06, SSO-07, SSO-08, SSO-09, LINK-01, LINK-02, LINK-03
**Success Criteria** (what must be TRUE):
  1. Navigating to `GET /api/sso/open-quest` with a valid signed token logs the DM into Omphalos (JWT cookie issued) and redirects to the correct session page in one browser round-trip
  2. A DM using Omphalos for the first time via SSO gets an account auto-provisioned with the correct role; subsequent SSO clicks use the existing account without modification
  3. Each Quest Board quest maps to exactly one Omphalos GameSession — a second SSO click for the same quest lands in the same session, not a new one
  4. A token with an expired `expiry`, an invalid signature, or a missing `QUEST_BOARD_SECRET` env var returns an appropriate HTTP error and does not issue a cookie
  5. Omphalos starts and operates normally when `QUEST_BOARD_SECRET` is not set in the environment — only the SSO endpoint is affected
**Plans**: TBD

**Wave 1**
- TBD — 12-01-PLAN.md: GameSession.ExternalQuestId migration + ISessionRepository.GetByExternalQuestIdAsync + ISsoService + SsoService (token validation, user auto-provision, session find-or-create) + IAuthService.GenerateToken promotion (SSO-04, SSO-05, SSO-06, SSO-07, LINK-01, LINK-02, LINK-03)

**Wave 2** *(blocked on Wave 1 completion)*
- TBD — 12-02-PLAN.md: SsoEndpoints.cs GET /api/sso/open-quest + Program.cs registration + OMPHALOS_SAMESITE cookie config + QUEST_BOARD_SECRET fail-fast + .env.example entry (SSO-01, SSO-02, SSO-03, SSO-08, SSO-09)

## Progress

**Execution Order:**
10 → 11 → end-to-end test
12 runs in parallel with 11 (converges at end-to-end test)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 10. Admin Settings | 2/2 | Complete | 2026-06-18 |
| 11. Navigation + Token Generation | 2/2 | Complete | 2026-06-18 |
| 12. SSO Endpoint + Quest-Session Linking (Omphalos) | 0/2 | Not started | - |
