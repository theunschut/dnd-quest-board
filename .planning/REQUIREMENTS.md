# Requirements: D&D Quest Board — Milestone 2

**Defined:** 2026-04-15
**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## v1 Requirements

### Architecture Refactor

- [x] **ARCH-01**: `EntityProfile.cs` (AutoMapper Entity↔DomainModel) lives in `EuphoriaInn.Repository`, not `EuphoriaInn.Domain`
- [x] **ARCH-02**: `EuphoriaInn.Domain.csproj` has no `<ProjectReference>` to `EuphoriaInn.Repository`
- [x] **ARCH-03**: Dependency direction is `Service → Domain ← Repository`; Domain compiles without Repository
- [x] **ARCH-04**: AutoMapper registration in `Program.cs` explicitly references both profile types by assembly anchor (no `AppDomain` scanning)

### Controller Slimming

- [x] **CTRL-01**: Quest finalization (email dispatch included) is fully handled inside `QuestService.FinalizeQuestAsync`; controller action is ≤ 20 lines
- [x] **CTRL-02**: `QuestController` does not inject `IEmailService` directly (all email goes through `QuestService`)
- [x] **CTRL-03**: Date-change email dispatch is handled inside `QuestService.UpdateQuestPropertiesWithNotificationsAsync`; controller receives `ServiceResult` not a user list
- [x] **CTRL-04**: Shop remaining-quantity calculation is handled inside `ShopService`; `ShopController.Index` only maps and renders

### Email & Configuration

- [x] **EMAIL-01**: `EmailSettings` typed options record exists and is registered with `AddOptions<EmailSettings>().BindConfiguration()` in `ServiceExtensions`
- [x] **EMAIL-02**: `EmailService` injects `IOptions<EmailSettings>` instead of `IConfiguration`; SMTP setup is not duplicated across methods
- [x] **EMAIL-03**: The `[Quest Board URL]` placeholder in the date-changed email body is replaced with the real application URL
- [x] **EMAIL-04**: Email finalize dispatch builds its recipient list from post-save entity state (not pre-finalize fetched `quest` object)

### Security

- [x] **SEC-01**: `lockoutOnFailure: true` is passed to `PasswordSignInAsync` and `LockoutOptions` configured (5 attempts, 15-min lock)
- [x] **SEC-02**: EF Core migration sets `LockoutEnabled = 1` for all existing users in `AspNetUsers`
- [x] **SEC-03**: Minimum password length is 8 characters (up from 6)
- [x] **SEC-04**: `HasKey` checkbox is removed from `Account/Edit.cshtml` and `EditProfileViewModel`; it can only be set via `Admin/EditUser`
- [x] **SEC-05**: `Password` property removed from `User` domain model, `Equals`, and `GetHashCode`; AutoMapper ignore is explicit on both mapping directions
- [x] **SEC-06**: `.env` added to `.gitignore`; `.env.example` with placeholder values is the only tracked env file

### Code Quality & Dead Code

- [x] **QUAL-01**: `SecurityConfiguration.cs` deleted; `Security` section removed from `appsettings.json`
- [x] **QUAL-02**: Dead `UpdateQuestPropertiesAsync` (non-notification variant) removed from `IQuestService` and `QuestService`
- [x] **QUAL-03**: `SignupRole == 1` magic number replaced with `(SignupRole)playerSignup.SignupRole == SignupRole.Spectator` cast throughout service code
- [x] **QUAL-04**: `IsSameDateTime` 30-minute window extracted as a named constant with explanatory comment
- [x] **QUAL-05**: `CharacterViewModels/GuildMembersIndexViewModel.cs` renamed to `CharactersIndexViewModel.cs` to match its class name

### Feature: Shop Filter/Sort (GitHub #96)

- [ ] **SHOP-01**: User can filter shop items by item rarity (one or more rarity values)
- [ ] **SHOP-02**: User can sort shop items by price ascending or descending
- [ ] **SHOP-03**: Filter and sort state persists in the URL as query parameters (bookmarkable)
- [ ] **SHOP-04**: Applying filter/sort does not require a page reload beyond the initial request (server-side, no JS dependency)

### Feature: Follow-Up Quest (GitHub #49)

- [ ] **FOLLOW-01**: A DM can create a follow-up quest from a finalized quest's Manage page
- [ ] **FOLLOW-02**: The follow-up quest pre-fills all players from the original quest as pre-approved signups
- [ ] **FOLLOW-03**: The follow-up quest requires a new date to be set before saving
- [ ] **FOLLOW-04**: The follow-up quest is linked to the original via `OriginalQuestId`; the original quest's detail page shows a link to its follow-up
- [ ] **FOLLOW-05**: An EF Core migration adds nullable `OriginalQuestId` self-referential FK to `QuestEntity`

### Feature: DM Profile Page (GitHub #98)

- [ ] **DMPRO-01**: A dedicated DM profile page exists at `/DungeonMaster/Profile/{id}` showing the DM's photo, name, and bio
- [ ] **DMPRO-02**: DMs can edit their own profile bio and upload a profile photo via the existing account area
- [ ] **DMPRO-03**: Admin can edit any DM's profile
- [ ] **DMPRO-04**: The DM directory page links to each DM's profile
- [ ] **DMPRO-05**: An EF Core migration adds `Bio` (varchar 2000, nullable) and a linked `DungeonMasterProfileImage` table (following `CharacterImageEntity` pattern)

### Feature: Profile Picture Avatar Crop (GitHub #78)

- [ ] **CROP-01**: When uploading a character profile picture, the user sees a Cropper.js 1.5.x square crop selector before submitting
- [ ] **CROP-02**: The selected crop region is stored as four nullable float columns (`CropX`, `CropY`, `CropWidth`, `CropHeight`) on `CharacterImages`; the original full image bytes are never modified
- [ ] **CROP-03**: A new `GetAvatarPicture` endpoint serves the cropped version using SkiaSharp; the existing `GetProfilePicture` endpoint continues serving the original
- [ ] **CROP-04**: The guild member directory page uses the avatar (cropped) endpoint; the character detail page uses the original endpoint
- [ ] **CROP-05**: An EF Core migration adds the four crop coordinate columns to `CharacterImages`

## v2 Requirements

### Bug Fixes (separate milestone)

- **BUG-01**: DM can add new dates to an existing quest (issue #94)
- **BUG-02**: Profile images ≤ 5MB do not return HTTP 413 (issue #91)
- **BUG-03**: DM sessions are excluded from the Quest Log page (issue #89)

### Large Features (future milestones)

- **FEAT-01**: D&D Beyond PDF character sheet parser (issue #84)
- **FEAT-02**: 5etools integration (issue #82)
- **FEAT-03**: Miniature request page (issue #59)
- **FEAT-04**: Email notifications and password reset (issue #25)
- **FEAT-05**: Build artifact for non-Docker deployment (issue #64)

### Performance / Polish

- **PERF-01**: Pagination on quest list, shop, and admin user list
- **PERF-02**: MailKit replacing deprecated `System.Net.Mail.SmtpClient`
- **PERF-03**: Image blob storage migrated to filesystem/Azure Blob

## Out of Scope

| Feature | Reason |
|---------|--------|
| Bug fixes (#94, #91, #89) | Separate bug-fix milestone to avoid merge conflicts with refactor |
| D&D Beyond PDF parser (#84) | Large standalone feature; own milestone |
| 5etools integration (#82) | Large standalone feature; own milestone |
| Miniature request page (#59) | Large standalone feature; own milestone |
| Email verification on registration | Small group, trust assumed; not requested |
| Pagination | Group size makes unbounded lists acceptable now |
| MailKit migration | Parallel scope expansion to email refactor; deferred, SmtpClient warning suppressed |
| Image blob storage migration | Performance acceptable at current scale |
| Rate limiting middleware | Low risk for private group app; deferred |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ARCH-01 | Phase 1 | Complete |
| ARCH-02 | Phase 1 | Complete |
| ARCH-03 | Phase 1 | Complete |
| ARCH-04 | Phase 1 | Complete |
| CTRL-01 | Phase 2 | Complete |
| CTRL-02 | Phase 2 | Complete |
| CTRL-03 | Phase 2 | Complete |
| CTRL-04 | Phase 2 | Complete |
| EMAIL-01 | Phase 2 | Complete |
| EMAIL-02 | Phase 2 | Complete |
| EMAIL-03 | Phase 2 | Complete |
| EMAIL-04 | Phase 2 | Complete |
| SEC-01 | Phase 4 | Complete |
| SEC-02 | Phase 4 | Complete |
| SEC-03 | Phase 4 | Complete |
| SEC-04 | Phase 4 | Complete |
| SEC-05 | Phase 4 | Complete |
| SEC-06 | Phase 4 | Complete |
| QUAL-01 | Phase 3 | Complete |
| QUAL-02 | Phase 3 | Complete |
| QUAL-03 | Phase 3 | Complete |
| QUAL-04 | Phase 3 | Complete |
| QUAL-05 | Phase 3 | Complete |
| SHOP-01 | Phase 5 | Pending |
| SHOP-02 | Phase 5 | Pending |
| SHOP-03 | Phase 5 | Pending |
| SHOP-04 | Phase 5 | Pending |
| FOLLOW-01 | Phase 6 | Pending |
| FOLLOW-02 | Phase 6 | Pending |
| FOLLOW-03 | Phase 6 | Pending |
| FOLLOW-04 | Phase 6 | Pending |
| FOLLOW-05 | Phase 6 | Pending |
| DMPRO-01 | Phase 7 | Pending |
| DMPRO-02 | Phase 7 | Pending |
| DMPRO-03 | Phase 7 | Pending |
| DMPRO-04 | Phase 7 | Pending |
| DMPRO-05 | Phase 7 | Pending |
| CROP-01 | Phase 8 | Pending |
| CROP-02 | Phase 8 | Pending |
| CROP-03 | Phase 8 | Pending |
| CROP-04 | Phase 8 | Pending |
| CROP-05 | Phase 8 | Pending |

**Coverage:**
- v1 requirements: 42 total (note: REQUIREMENTS.md initially listed 40; actual count per requirement IDs is 42)
- Mapped to phases: 42/42
- Unmapped: 0

---
*Requirements defined: 2026-04-15*
*Last updated: 2026-04-17 — marked ARCH-02, ARCH-03, ARCH-04 complete after Phase 1 execution*
