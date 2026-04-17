# Roadmap: D&D Quest Board — Milestone 2: Refactor + Feature Expansion

## Overview

Milestone 2 fixes the codebase's accumulated architectural drift before adding new features. The first four phases restore correct layer boundaries, consolidate business logic into services, remove dead code, and close security gaps. The remaining four phases deliver the four backlog features (shop filter/sort, follow-up quest, DM profile page, avatar crop) onto the now-clean architecture.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3...): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Layer Dependency Fix** - Restore correct compile-time dependency direction: Domain compiles without referencing Repository
- [ ] **Phase 2: Email & Service Consolidation** - Move email dispatch and finalization logic into services; introduce typed email options
- [ ] **Phase 3: Code Quality & Dead Code** - Remove dead code, fix naming, replace magic numbers with named references
- [ ] **Phase 4: Security Hardening** - Enable account lockout, raise password minimum, remove HasKey from user-facing edit, clean .env from git
- [ ] **Phase 5: Shop Filter & Sort** - Let players filter and sort shop items by rarity and price without a JS dependency
- [ ] **Phase 6: Follow-Up Quest** - Let DMs create a part-2 quest from a finalized quest with players pre-approved
- [ ] **Phase 7: DM Profile Page** - Give each DM a browsable profile with photo and bio; admin can edit any DM's profile
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
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference

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
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference

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
**Plans**: 2 plans
Plans:
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference

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
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference
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
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference
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
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference
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
- [ ] 01-01-PLAN.md — Repository infrastructure: move EntityProfile, refactor BaseRepository to dual-generic with IMapper
- [ ] 01-02-PLAN.md — Complete dependency inversion: move interfaces to Domain, refactor services, remove ProjectReference
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8
Note: Phases 5, 6, 7, 8 are independent of each other (all depend on Phase 4 or Phase 3) and could run in any order.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Layer Dependency Fix | 0/2 | Planned | - |
| 2. Email & Service Consolidation | 2/3 | In Progress|  |
| 3. Code Quality & Dead Code | 0/? | Not started | - |
| 4. Security Hardening | 0/? | Not started | - |
| 5. Shop Filter & Sort | 0/? | Not started | - |
| 6. Follow-Up Quest | 0/? | Not started | - |
| 7. DM Profile Page | 0/? | Not started | - |
| 8. Profile Picture Avatar Crop | 0/? | Not started | - |
