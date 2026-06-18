# Phase 7: DM Profile Page - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Give every DM a browsable profile page at `/DungeonMaster/Profile/{id}` showing their name, photo, and bio alongside a list of all quests they have run (most recent first). DMs edit their own profile (bio + photo) from a dedicated DungeonMasterController page; admins can edit any DM's profile via the same route using an `{id}` parameter. The DM directory page links to each individual profile.

This phase does NOT add: DM-to-player messaging, quest rating/review, campaign grouping, or any changes to the quest creation/finalization flow.

</domain>

<decisions>
## Implementation Decisions

### Profile Data Storage

- **D-01:** Create a new `DungeonMasterProfileEntity` (separate from `UserEntity`) linked to UserId as a FK. This keeps DM-specific data isolated from the base user record. The entity holds `Bio` (varchar 2000, nullable) and a navigation property to `DungeonMasterProfileImage`.
- **D-02:** Create a `DungeonMasterProfileImage` table following the `CharacterImageEntity` pattern: separate table, `Id` is both PK and FK to `DungeonMasterProfileEntity`, `ImageData` is `byte[]`.
- **D-03:** `DungeonMasterProfileEntity` is created lazily — only when a DM saves their profile for the first time. The profile page handles null gracefully (placeholder image, empty bio). No migration data-seeding required.

### Edit UX & Routing

- **D-04:** A new `DungeonMasterController` handles all DM profile routes. The edit page lives at `GET/POST /DungeonMaster/EditProfile` (DM editing their own) and `GET/POST /DungeonMaster/EditProfile/{id}` (admin editing any DM). This mirrors the Quest Manage pattern.
- **D-05:** Authorization on edit actions: `[Authorize(Policy = "DungeonMasterOnly")]` (which includes Admin role) + inline check `!isOwnProfile && !User.IsInRole("Admin")` → `Forbid()`. Exact pattern from `QuestController.Manage`.
- **D-06:** The edit page is linked from the DM navbar dropdown (for the logged-in DM editing their own profile).
- **D-07:** The profile page (`/DungeonMaster/Profile/{id}`) shows an "Edit Profile" button visible to both the DM who owns the profile and to admins. This button links to `/DungeonMaster/EditProfile/{id}` (using the target DM's user id).

### Profile Page Content

- **D-08:** `/DungeonMaster/Profile/{id}` displays: DM name, profile photo (or placeholder), bio, and a list of all quests the DM has run (finalized and active), ordered most recent first.
- **D-09:** Quest list shows title, date, and difficulty for each quest. Links to the quest's `/Quest/Details/{id}` page.

### DM Directory Integration

- **D-10:** The DM directory page (which currently lists DMs) adds a link on each DM's name/row to their profile at `/DungeonMaster/Profile/{id}`.

### Claude's Discretion

- Exact HTML/CSS layout of the profile page (follow `modern-card` pattern, FontAwesome icons, Bootstrap 5 grid).
- Whether to add a new `IDungeonMasterService` or extend `IUserService` with DM profile methods.
- Image serving endpoint name and placement within `DungeonMasterController`.
- Whether `DungeonMasterProfileEntity.Id` shares the same value as `UserId` (like CharacterImageEntity) or uses an independent PK.
- Whether `DungeonMasterProfileEntity` navigation is on `UserEntity` or resolved separately by UserId query.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Image Pattern Reference
- `EuphoriaInn.Repository/Entities/CharacterImageEntity.cs` — PK=FK image table pattern to replicate for DungeonMasterProfileImage
- `EuphoriaInn.Repository/CharacterRepository.cs` — `GetCharacterProfilePictureAsync` and `UpdateProfileImageAsync` methods to mirror
- `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs` — `GetProfilePicture` image-serving endpoint pattern (returns `File(bytes, "image/jpeg")`)

### Auth Pattern Reference
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — `Manage` action (lines ~181-244): `DungeonMasterOnly` policy + inline `!currentUser.Equals(owner) && !User.IsInRole("Admin")` check; replicate this pattern for DM profile edit

### Data Layer Reference
- `EuphoriaInn.Repository/Entities/UserEntity.cs` — entity that `DungeonMasterProfileEntity` will FK to
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — DbContext FK relationship configuration (reference CharacterEntity/CharacterImageEntity relationship as model)
- `EuphoriaInn.Repository/Automapper/EntityProfile.cs` — entity↔model AutoMapper profiles; add DungeonMasterProfile mappings here
- `EuphoriaInn.Service/Automapper/ViewModelProfile.cs` — model↔viewmodel AutoMapper profiles; add DM profile mappings here

### Upload Pattern Reference
- `EuphoriaInn.Service/ViewModels/CharacterViewModels/CharacterViewModel.cs` — `[MaxFileSize]`, `[AllowedExtensions]`, `IFormFile?` pattern for the image upload field
- `EuphoriaInn.Service/Views/GuildMembers/Edit.cshtml` — `enctype="multipart/form-data"`, current image display + file input pattern

### Admin Controller Reference
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — existing admin action patterns for reference

### Requirements
- `.planning/REQUIREMENTS.md` §DMPRO-01 – DMPRO-05 — the five acceptance criteria for this phase

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CharacterImageEntity` pattern: separate table, `Id` = PK + FK to owner entity, `ImageData` = `byte[]` — copy this exact shape for `DungeonMasterProfileImage`.
- `GuildMembersController.GetProfilePicture(int id)`: returns `File(bytes, "image/jpeg")` — copy this endpoint shape for a DM photo-serving action.
- `QuestController.Manage` authorization pattern: `[Authorize(Policy = "DungeonMasterOnly")]` + `!currentUser.Equals(quest.DungeonMaster) && !User.IsInRole("Admin")` → `Forbid()`. Use this verbatim for the DM profile edit actions.
- `CharacterViewModel.ProfilePictureFile`: `[MaxFileSize(5 * 1024 * 1024)]`, `[AllowedExtensions(new[] { ".jpg", ".jpeg", ".png", ".gif" })]`, `IFormFile?` — reuse these validation attributes on the DM profile edit ViewModel.

### Established Patterns
- Image upload form: `enctype="multipart/form-data"`, display current image via `Url.Action("GetXxxPicture", new { id = ... })`, show placeholder when null.
- `modern-card` / `modern-card-header` CSS classes for all new views (project-wide convention).
- FontAwesome icons with `me-2` spacing; button layout `d-flex justify-content-between` per UI guidelines.

### Integration Points
- `UserEntity.Quests` navigation property — available to load a DM's quest list without extra joins.
- Navbar DM dropdown — add "Edit My Profile" link here for DMs.
- DM directory page (currently exists in the codebase) — add a profile link per DM row; the exact file needs to be located during planning.
- There is currently NO `DungeonMasterController` — it must be created from scratch alongside its views.

</code_context>

<specifics>
## Specific Ideas

- **Edit button on profile page** mirrors the quest pattern: the public `/DungeonMaster/Profile/{id}` page shows an "Edit Profile" button to the owning DM and to admins, linking to `/DungeonMaster/EditProfile/{id}`.
- **Navbar entry** for DMs: "Edit My Profile" in the DM dropdown navigates to `/DungeonMaster/EditProfile` (no id — current user's own profile).
- **Quest list on profile**: title + date + difficulty, linking to `/Quest/Details/{questId}`, ordered by date descending.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 07-dm-profile-page*
*Context gathered: 2026-06-17*
