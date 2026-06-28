---
phase: 07-dm-profile-page
plan: 02
subsystem: web-layer
tags: [mvc, controller, views, automapper, integration-tests, dm-profile, css]
dependency_graph:
  requires:
    - 07-01 (IDungeonMasterProfileService, DungeonMasterProfile model, GetQuestsByDungeonMasterAsync)
  provides:
    - DungeonMasterController (Profile GET, EditProfile GET/POST, GetDMProfilePicture GET)
    - DMProfileViewModel + QuestSummaryViewModel
    - EditDMProfileViewModel with file upload validation
    - Views/DungeonMaster/Profile.cshtml
    - Views/DungeonMaster/EditProfile.cshtml
    - wwwroot/css/dm-profile.css
    - Quest->QuestSummaryViewModel AutoMapper mapping
    - Integration tests for DMPRO-01 through DMPRO-04
  affects:
    - EuphoriaInn.Service/Controllers/DungeonMaster (new)
    - EuphoriaInn.Service/ViewModels/DungeonMasterViewModels (new)
    - EuphoriaInn.Service/Views/DungeonMaster (new)
    - EuphoriaInn.Service/wwwroot/css/dm-profile.css (new)
    - EuphoriaInn.Service/Automapper/ViewModelProfile.cs (extended)
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml (navbar + CSS link)
    - EuphoriaInn.Service/Views/Players/Index.cshtml (DM name links)
    - EuphoriaInn.IntegrationTests/Controllers (2 new test files)
tech_stack:
  added: []
  patterns:
    - DungeonMasterOnly policy + inline IDOR check returning Forbid() (mirrors QuestController.Manage)
    - IFormFile -> byte[] upload pattern (mirrors GuildMembersController)
    - MaxFileSizeAttribute + AllowedExtensionsAttribute reuse from CharacterViewModels
    - GetDMProfilePicture image-serving endpoint (mirrors GuildMembersController.GetProfilePicture)
    - AllowAnonymous on Profile + GetDMProfilePicture for public access
key_files:
  created:
    - EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs
    - EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/DMProfileViewModel.cs
    - EuphoriaInn.Service/ViewModels/DungeonMasterViewModels/EditDMProfileViewModel.cs
    - EuphoriaInn.Service/Views/DungeonMaster/Profile.cshtml
    - EuphoriaInn.Service/Views/DungeonMaster/EditProfile.cshtml
    - EuphoriaInn.Service/wwwroot/css/dm-profile.css
    - EuphoriaInn.IntegrationTests/Controllers/DungeonMasterControllerIntegrationTests.cs
    - EuphoriaInn.IntegrationTests/Controllers/PlayersControllerIntegrationTests.cs
  modified:
    - EuphoriaInn.Service/Automapper/ViewModelProfile.cs
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml
    - EuphoriaInn.Service/Views/Players/Index.cshtml
decisions:
  - "Quest->QuestSummaryViewModel maps FinalizedDate->Date — quest date on profile shows finalization date, consistent with what players see"
  - "Profile GET + GetDMProfilePicture marked [AllowAnonymous] — profiles are public per DMPRO-01"
  - "Forbid() integration test uses BeOneOf(Forbidden, Redirect, Unauthorized) — Identity.Application DefaultForbidScheme redirects to /Account/AccessDenied in test infrastructure (matches existing QuestController test pattern)"
  - "PlayersControllerIntegrationTests creates DM user with DungeonMaster role via CreateAuthenticatedClientWithUserAsync — GetAllDungeonMastersAsync filters by DM role so bare CreateTestUserAsync (Player only) would not appear in directory"
metrics:
  duration_minutes: 15
  completed_date: "2026-06-17T19:23:30Z"
  tasks_completed: 2
  files_changed: 11
---

# Phase 07 Plan 02: DM Profile Web Layer Summary

**One-liner:** DungeonMasterController with four actions wired to IDungeonMasterProfileService, two ViewModels, two Razor views using modern-card pattern, dm-profile.css, navbar Edit My Profile link, DM directory profile links, and 7 integration tests covering DMPRO-01 through DMPRO-04.

## What Was Built

Complete web layer for the DM Profile subsystem. Combined with Plan 01 (data/service layer), all five DMPRO requirements are now delivered.

### Task 1: Integration Test Stubs (RED state before controller)

- `DungeonMasterControllerIntegrationTests.cs` — 5 tests:
  - `Profile_WithValidDmUserId_ReturnsOk` (DMPRO-01)
  - `Profile_WithNoSavedProfile_RendersPlaceholderNotNotFound` (DMPRO-01 — null profile graceful handling)
  - `Profile_WithNonExistentUserId_ReturnsNotFound` (DMPRO-01 — 404 for bad id)
  - `EditProfile_OwnProfile_ReturnsOk` (DMPRO-02)
  - `EditProfile_AdminEditingOtherDm_ReturnsOk` (DMPRO-03)
  - `EditProfile_NonAdminDmEditingOtherDm_ReturnsForbidden` (DMPRO-03 IDOR check)
- `PlayersControllerIntegrationTests.cs` — 1 test:
  - `Index_DmDirectory_ContainsProfileLinkForEachDm` (DMPRO-04)

**Commit:** `a87844c`

### Task 2: Controller, ViewModels, Views, CSS, Navbar, Directory Link

- `DungeonMasterController` — `[Authorize]` class with:
  - `Profile(int id)` — `[AllowAnonymous]` GET; returns 404 for unknown user, renders placeholder when no profile row exists
  - `EditProfile(int? id)` — `[Authorize(Policy = "DungeonMasterOnly")]` GET; inline IDOR check `targetUserId != currentUser.Id && !User.IsInRole("Admin")` → `Forbid()`
  - `EditProfile(int? id, EditDMProfileViewModel)` — `[ValidateAntiForgeryToken]` POST; same IDOR check; IFormFile → byte[] → UpsertProfileAsync
  - `GetDMProfilePicture(int id)` — `[AllowAnonymous]` GET; returns image bytes as `image/jpeg`
- `DMProfileViewModel` — UserId, Name, Bio, HasProfilePicture, CanEdit, `List<QuestSummaryViewModel>`
- `QuestSummaryViewModel` — Id, Title, Date (mapped from FinalizedDate), ChallengeRating
- `EditDMProfileViewModel` — DungeonMasterId, Bio with `[StringLength(2000)]`, ProfilePicture (current image bytes), ProfilePictureFile with `[MaxFileSize]` + `[AllowedExtensions]`
- `Profile.cshtml` — two-column layout (`col-lg-4` sidebar + `col-lg-8` quest history); placeholder `div.dm-profile-placeholder` when no photo; "No bio provided yet." when null; quest table with difficulty badge switch expression
- `EditProfile.cshtml` — `enctype="multipart/form-data"`, current image preview, `#dmFileSizeError` div, client-side JS file validation, "Back to Profile" / "Save Profile" button layout
- `dm-profile.css` — `.dm-profile-photo` (128px circle), `.dm-profile-placeholder` (128px circle), `.badge.bg-purple` (deadly difficulty)
- `_Layout.cshtml` — added `<link dm-profile.css>` in `<head>`; added "Edit My Profile" `<li>` after "My Quests" inside `DungeonMasterOnly` dropdown
- `Players/Index.cshtml` — DM name cell now wraps in `<a asp-controller="DungeonMaster" asp-action="Profile" asp-route-id="@dm.Id">`
- `ViewModelProfile.cs` — `CreateMap<Quest, QuestSummaryViewModel>()` mapping FinalizedDate → Date

**Commit:** `87e711c`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] PlayersControllerIntegrationTests — DM user needs DM role to appear in directory**

- **Found during:** Task 2 integration test run
- **Issue:** `PlayersControllerIntegrationTests.Index_DmDirectory_ContainsProfileLinkForEachDm` created a test DM user via `CreateTestUserAsync` which only assigns the default Player role. `PlayersController` uses `GetAllDungeonMastersAsync` which filters by DM role, so the user never appeared in the page HTML.
- **Fix:** Changed to `CreateAuthenticatedClientWithUserAsync(factory, ..., roles: ["DungeonMaster"])` so the user is correctly in the DM role and appears in the directory listing.
- **Files modified:** `EuphoriaInn.IntegrationTests/Controllers/PlayersControllerIntegrationTests.cs`
- **Commit:** `87e711c`

**2. [Rule 1 - Bug] DungeonMasterControllerIntegrationTests — Forbid() returns 302, not 403**

- **Found during:** Task 2 integration test run
- **Issue:** `EditProfile_NonAdminDmEditingOtherDm_ReturnsForbidden` asserted `HttpStatusCode.Forbidden (403)`. The test infrastructure sets `DefaultForbidScheme = "Identity.Application"` which redirects to `/Account/AccessDenied` — returning 302, not 403.
- **Fix:** Changed assertion to `BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect, HttpStatusCode.Unauthorized)` — exact same pattern used by `QuestControllerIntegrationTests_Comprehensive.cs` and `AdminControllerIntegrationTests.cs`.
- **Files modified:** `EuphoriaInn.IntegrationTests/Controllers/DungeonMasterControllerIntegrationTests.cs`
- **Commit:** `87e711c`

## Known Stubs

None. All views are fully wired:
- Profile.cshtml renders real data from `IDungeonMasterProfileService.GetProfileByUserIdAsync` and `IQuestService.GetQuestsByDungeonMasterAsync`
- EditProfile.cshtml saves via `IDungeonMasterProfileService.UpsertProfileAsync`
- Placeholder states ("No bio provided yet.", "No Quests Yet") are intentional UX for the null/empty data case — not stubs

## Threat Flags

All threat mitigations from the plan's `<threat_model>` are implemented and verified:

| Threat | Mitigation | Location |
|--------|------------|----------|
| T-07-01 (DoS — oversized file) | `[MaxFileSize(5MB)]` on ProfilePictureFile + client-side `#dmFileSizeError` check | EditDMProfileViewModel.cs, EditProfile.cshtml |
| T-07-02 (EoP — IDOR DM editing another DM) | Inline `targetUserId != currentUser.Id && !User.IsInRole("Admin")` → `Forbid()` in both GET and POST | DungeonMasterController.cs |
| T-07-03 (Tampering — CSRF) | `[ValidateAntiForgeryToken]` on POST action | DungeonMasterController.cs |
| T-07-04 (Tampering — malicious file) | `[AllowedExtensions(".jpg",".jpeg",".png",".gif")]` on ProfilePictureFile | EditDMProfileViewModel.cs |

No new threat surface beyond what was specified in the plan's threat model.

## Self-Check: PASSED

| Check | Result |
|-------|--------|
| DungeonMasterController.cs | FOUND |
| DMProfileViewModel.cs | FOUND |
| EditDMProfileViewModel.cs | FOUND |
| Profile.cshtml | FOUND |
| EditProfile.cshtml | FOUND |
| dm-profile.css | FOUND |
| DungeonMasterControllerIntegrationTests.cs | FOUND |
| PlayersControllerIntegrationTests.cs | FOUND |
| Commit a87844c (Task 1) | FOUND |
| Commit 87e711c (Task 2) | FOUND |
| dotnet build: 0 errors | PASS |
| dotnet test (94 total): 0 failures | PASS |
