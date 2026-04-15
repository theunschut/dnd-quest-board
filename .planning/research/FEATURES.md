# Feature Landscape

**Domain:** D&D Campaign Management Web App — Milestone 2 Feature Additions
**Researched:** 2026-04-15

---

## Feature 1: DM Profile Page (GitHub #98)

### What It Is

A dedicated profile page per Dungeon Master showing photo, display name, and a free-text bio/style description. Players browse DM profiles before signing up for quests.

### Current State

The `UserEntity` has no bio or profile photo fields. `Quest` has a `DungeonMasterId` FK to `User` and exposes `DungeonMaster.Name` already. There is no `DungeonMasterController` in the current codebase — only an `Authorization/DungeonMasterHandler.cs`. The user identity (DM role) is managed purely through ASP.NET Identity roles.

### Expected Behavior

- Any user in the `DungeonMaster` role appears in a DM directory listing
- Each DM has a profile page at `/DungeonMaster/{id}` showing: photo, name, bio/style blurb, and a list of their past/upcoming quests
- The DM themselves can edit their own bio and upload their photo
- Admin can edit any DM's profile
- Players can navigate from a quest card to the posting DM's profile

### Implementation Pattern

**Data model additions required:**
- `UserEntity`: add `Bio` (`nvarchar(2000)`), `ProfilePhoto` (`varbinary(max)` or separate table like `CharacterImageEntity` pattern)
- The `CharacterImageEntity` pattern (separate table, one-to-one FK = same PK) is already established in this codebase — follow it for DM photos too: create `UserProfileImageEntity`
- EF migration required

**New controller:**
- `DungeonMasterController` at `EuphoriaInn.Service/Controllers/QuestBoard/DungeonMasterController.cs`
- `GET /DungeonMaster` — list all DMs (uses `IUserService.GetUsersInRoleAsync("DungeonMaster")`)
- `GET /DungeonMaster/{id}` — individual profile
- `GET /DungeonMaster/Edit` — edit own profile (requires `[Authorize(Policy = "DungeonMasterOnly")]`)
- `POST /DungeonMaster/Edit` — save bio + photo upload
- `GET /DungeonMaster/GetPhoto/{id}` — returns `File(bytes, "image/jpeg")` (same pattern as `GuildMembersController.GetProfilePicture`)

**Domain layer additions:**
- Add `Bio` and photo fields to `User` domain model and `UserEntity`
- Extend `IUserService` with `GetDungeonMastersAsync()` and `GetDungeonMasterWithQuestsAsync(int id)`

**AutoMapper:**
- New `DungeonMasterViewModel` and `DungeonMasterIndexViewModel`; add mapping in `ViewModelProfile`

### Table Stakes

| Feature | Why Expected | Complexity |
|---------|--------------|------------|
| Photo upload + display | Players want to recognise their DM | Low — mirrors CharacterImageEntity pattern exactly |
| Bio/style description text field | Core purpose of the issue | Low — single varchar column |
| DM directory listing | No point showing profiles you can't navigate to | Low |
| "Quests run by this DM" list | Players assess DM activity | Low — Quest already has DungeonMasterId FK |
| Link from quest to DM profile | Main discovery path | Low — add anchor in quest card |

### Differentiators

| Feature | Value | Complexity |
|---------|-------|------------|
| Quest completion stats (# sessions run) | Credibility signal | Low — COUNT query |
| Player review/rating on DM | Rich discovery | High — new entities, moderation concerns |
| DM availability calendar | Pre-signup planning | High — ties into calendar feature |

### Anti-Features

| Anti-Feature | Why Avoid | Instead |
|--------------|-----------|---------|
| Public (unauthenticated) DM profiles | App is already auth-gated everywhere | Keep consistent: require login |
| Rich text / markdown bio | Overkill for a small group | Plain textarea with `<pre>` or `white-space: pre-wrap` rendering |
| DM self-registration to the role | Role assignment is an admin operation | Admin panel already handles role assignment |

### Dependencies

- No dependencies on any of the other 3 features
- Dependent on: the architecture refactor (moving `User` domain model cleanly off `Password`/`HasKey` pollution) — but can be done in parallel if the DM profile only touches new fields

---

## Feature 2: Shop Filter and Sort (GitHub #96)

### What It Is

Add price sort (ascending/descending) and rarity filter to the existing shop index page. The shop already has `ItemRarity` (Common → Legendary) and `Price` on `ShopItem`, and already filters by `ItemType`. No new data model changes are needed.

### Current State

`ShopController.Index` accepts an `ItemType? type` query parameter. `ShopIndexViewModel` has pre-computed `EquipmentItems` and `MagicItems` computed properties. Sorting and rarity filtering are entirely absent. All filtering/sorting is in-memory (items fetched then filtered via LINQ on the ViewModel).

`ItemRarity` enum: `Common (0), Uncommon (1), Rare (2), VeryRare (3), Legendary (4)` — ordinal values are already in D&D canonical rarity order.

### Expected Behavior

- User can sort current item list by price ascending or descending
- User can filter by one or more rarities (Common, Uncommon, Rare, Very Rare, Legendary)
- Filters compose: type filter + rarity filter + sort all work together
- Filter/sort state is reflected in the URL (query string) so browser back-button works
- Active filters are visually indicated

### Implementation Pattern

**No new entity/migration needed** — all data is already present.

**Controller change:**
- Extend `ShopController.Index` signature: `(ItemType? type, ItemRarity? rarity, string? sortPrice, CancellationToken token)`
- Sort and filter in-memory after fetching published items — the dataset is small (small group app) so EF-level filtering is not required but could be added for correctness

**ViewModel change:**
- Add `SortPrice`, `SelectedRarity` properties to `ShopIndexViewModel`
- Replace the hard-coded `EquipmentItems`/`MagicItems` computed properties with a single `FilteredItems` property that respects all active filters, or do the filtering in the controller before assigning to `Items`

**View change:**
- Add a filter/sort bar above the item grid using Bootstrap 5 button groups (no new JS library needed)
- Rarity filter: checkboxes or button toggles that submit as query parameters
- Price sort: a dropdown or two buttons (ASC/DESC) that set `sortPrice=asc` or `sortPrice=desc`
- Use `asp-route-*` tag helpers to compose query string values in anchor tags

### Table Stakes

| Feature | Why Expected | Complexity |
|---------|--------------|------------|
| Price sort ASC/DESC | Core of the issue title | Low — one LINQ OrderBy |
| Rarity filter | Core of the issue title | Low — one LINQ Where |
| URL-based state (query params) | Expected from any filter UI | Low — existing pattern in the controller |
| Active filter indication | User must know what's applied | Low — CSS class on active button |

### Differentiators

| Feature | Value | Complexity |
|---------|-------|------------|
| Multi-rarity selection | More flexible browsing | Medium — requires comma-separated param or multiple params |
| "Reset filters" link | UX convenience | Low |
| Persist filter preference in session | Convenience for repeat visits | Low |

### Anti-Features

| Anti-Feature | Why Avoid | Instead |
|--------------|-----------|---------|
| Client-side JS filtering/sorting | Adds complexity, duplicates logic | Server-side via query params is simpler and bookmarkable |
| Price range slider | Overkill for a small fixed item catalog | Exact sort is sufficient |
| Search box | Different feature, out of scope here | Separate issue if needed |

### Dependencies

- No dependencies on the other 3 features
- Entirely self-contained within `ShopController`, `ShopIndexViewModel`, and the shop Index view

---

## Feature 3: Profile Picture Crop / Avatar Selection (GitHub #78)

### What It Is

When a player uploads a non-square image as their character's profile picture, let them interactively select a square crop region to use as the avatar on the guild member directory card. The full original image is still displayed on the character detail page. Only the crop offset/dimensions are stored alongside the image — the original bytes are not modified.

### Current State

`CharacterEntity` has a one-to-one `CharacterImageEntity` (separate `CharacterImages` table; PK = FK to `Characters`). The `GuildMembersController.GetProfilePicture(id)` endpoint returns `CharacterImageEntity.ImageData` as `image/jpeg`. The guild member card displays this image in what is presumably a square container. No crop data currently exists anywhere in the data model.

### Expected Behavior

- After selecting an image file on the Create/Edit character form, a crop UI appears in-page showing the uploaded image
- User drags/resizes a square crop box over the desired region
- On save, two things are sent to the server: the original file bytes AND the crop rectangle (x, y, width, height as percentages or pixels)
- `CharacterImageEntity` gains four nullable crop columns: `CropX`, `CropY`, `CropWidth`, `CropHeight` (stored as floats, 0.0–1.0 relative to original image dimensions — avoids coupling to resolution)
- Guild member directory: a new endpoint `GET /GuildMembers/GetAvatarPicture/{id}` (or CSS approach, see below) serves the cropped square
- Character detail page: existing `GetProfilePicture` endpoint continues to serve the full original image unchanged

### Image Crop Library Recommendation: Cropper.js v1.5.x

**Recommendation:** Use Cropper.js v1.5.x (NOT v2.x) via CDN.

**Rationale:**
- v1.5.x has a stable UMD bundle with a separate CSS file — drop-in compatible with this project's CDN-loading pattern (`<script>` + `<link rel="stylesheet">`)
- v2.x (released April 2026, latest 2.1.1) is a TypeScript rewrite — cdnjs lists it at 2.0.1 but its distribution format has changed; it may lack the traditional `cropper.css` + `cropper.js` CDN pair. v2 is primarily ESM-first
- v1.5.13 is the latest stable v1 release (confirmed on cdnjs, npmjs)
- 13.8k GitHub stars; MIT licence; well-documented vanilla JS API

**CDN links for v1.5.13:**
```html
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.5.13/cropper.min.css">
<script src="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.5.13/cropper.min.js"></script>
```

**Key API for this use case:**
```javascript
const cropper = new Cropper(imageElement, {
    aspectRatio: 1,          // enforce square crop for avatar
    viewMode: 1,             // prevent crop box from exceeding image
    autoCropArea: 0.8,       // default crop covers 80% of image
    responsive: true,
    background: false
});

// On form submit:
const data = cropper.getData(true); // true = rounded to integers
// data: { x, y, width, height, rotate, scaleX, scaleY }
// Store x, y, width, height as hidden fields
```

**Alternatives considered:**

| Library | CDN available | Last active | Assessment |
|---------|--------------|-------------|------------|
| Cropper.js v1.5.x | YES (cdnjs, unpkg) | Maintained | RECOMMENDED |
| Cropper.js v2.x | Partial (cdnjs 2.0.1) | Active (2026) | ESM-first, riskier for CDN drop-in |
| Croppie | YES | Unmaintained since 2021 | Avoid |
| Guillotine (jQuery) | jQuery CDN | Unmaintained | Avoid; jQuery already loaded but library stale |
| SmartCrop.js | YES | Active | Automatic only — no manual UI, wrong tool |

### Server-side crop application

Two approaches for serving the avatar:

**Option A (recommended) — Server-side crop on retrieval:**
- Store original bytes + crop rectangle in DB
- New endpoint `GetAvatarPicture(id)` reads `ImageData`, applies `System.Drawing.Common` (or `SkiaSharp`) to crop and return a square JPEG
- `SkiaSharp` is preferable: `System.Drawing.Common` is deprecated on non-Windows in .NET 8 and unreliable in Docker Linux containers
- Add `SkiaSharp` NuGet to `EuphoriaInn.Service` (not Repository — it's a presentation concern)
- **Dependency note:** `SkiaSharp` requires the native `libSkiaSharp` — the Docker base image `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian) supports this

**Option B — CSS-only avatar framing:**
- Store crop rectangle, render full image in a fixed-size `<div>` container using CSS `object-fit`, `object-position`, and `transform`/`clip-path` calculated from crop data
- Zero new NuGet dependencies; no server-side image processing
- More complex CSS; detail page and avatar page share the same image endpoint
- Works well if avatar container size is fixed and known

**Option A is recommended** because it cleanly separates the two image endpoints, works correctly regardless of CSS context (emails, future mobile views), and `SkiaSharp` is the .NET cross-platform standard for image processing.

### EF Migration Required

`CharacterImages` table gains four nullable float columns: `CropX`, `CropY`, `CropWidth`, `CropHeight`. Existing rows get NULLs (interpreted as "no crop — show full image").

### Table Stakes

| Feature | Why Expected | Complexity |
|---------|--------------|------------|
| Square crop UI on upload | The entire point of the issue | Medium — Cropper.js integration |
| Square avatar on guild member card | The visible outcome on the directory | Medium — new endpoint + SkiaSharp |
| Full image on character detail page | Explicitly in the issue spec | Low — existing endpoint unchanged |
| Crop persists across edits | User expects their crop to survive | Low — nullable crop columns in DB |

### Differentiators

| Feature | Value | Complexity |
|---------|-------|------------|
| Live preview of avatar during crop | Better UX on the form | Low — Cropper.js has built-in preview API |
| Re-crop existing image without re-upload | Edit page shows crop over existing image | Medium — need to load image into Cropper from the serve endpoint |

### Anti-Features

| Anti-Feature | Why Avoid | Instead |
|--------------|-----------|---------|
| Server-side crop on upload (overwrite original) | Destroys the original; detail page needs full image | Store crop coords separately |
| Free-form (non-square) crop selection | Avatar containers are square; free crop produces inconsistent results | Lock `aspectRatio: 1` in Cropper.js options |
| Rotation/flip controls | Unnecessary complexity for this use case | Disable in Cropper.js options |

### Dependencies

- Depends on nothing from the other 3 features
- Introduces a new NuGet dependency (`SkiaSharp`) if Option A is chosen — verify Docker image compatibility before starting

---

## Feature 4: Follow-up Quest (GitHub #49)

### What It Is

Create a "Part 2" (or nth follow-up) of an existing finalized quest. The new quest is pre-populated with all selected players from the original quest, skipping the signup phase. Only a new proposed date is required. The follow-up quest links back to its origin.

### Current State

`Quest` has no `OriginalQuestId` or `ParentQuestId` field. `PlayerSignup` stores signups with a `UserId` FK and a `SignupRole` (which uses magic number `== 1` for DM). Quest creation (`QuestController.Create`) starts fresh with no pre-population. The "finalized" player list is implicit: `PlayerSignup` rows where the quest is finalized and the player was selected.

### Expected Behavior

- A "Create Follow-up Quest" action is available from the `Quest/Manage/{id}` page, visible only to the quest's DM or an Admin, and only if the quest is finalized
- The follow-up creation form pre-fills: title (e.g., "{Original Title} — Part 2"), description, challenge rating, DM, player count, and the confirmed player list from the original quest
- Only the date picker is required to be filled in by the DM; everything else is editable but pre-filled
- On save, the new quest is created with:
  - `OriginalQuestId` set to the parent quest's ID
  - The pre-filled players added as `PlayerSignup` records in a "pre-approved" state (they are already in; no voting needed)
  - A single proposed date (or multiple, DM's choice — same UI as regular quest creation)
- The follow-up quest's detail page shows a "Sequel to: {Original Title}" link
- The original quest's detail page optionally shows "Continued in: {Follow-up Title}" (forward link)

### Key Design Decision: Pre-approved Signups

There are two approaches for pre-filled players:

**Option A:** Create signups as already-confirmed (set `IsFinalized = true` and skip the voting phase entirely). DM can still remove players before running the quest. This matches the issue spec ("automatically pre-fills all existing players — only a new date needs to be set").

**Option B:** Create signups in a "pre-filled" state that still requires date voting. Players are notified and vote on the new date.

Option A is correct per the issue spec. The date is the only unknown; players are already committed.

### Implementation Pattern

**Data model additions:**
- `QuestEntity`: add `OriginalQuestId int? FK(Quests)` (nullable self-referential FK)
- EF migration required; existing quests get NULL

**Controller additions:**
- `GET /Quest/CreateFollowUp/{id}` — loads original quest, builds `CreateFollowUpViewModel` with pre-filled data
- `POST /Quest/CreateFollowUp` — validates, creates new `Quest` + pre-approved `PlayerSignup` records, sets `OriginalQuestId`, redirects to new quest's Manage page

**Domain layer:**
- `Quest` domain model gains `int? OriginalQuestId` and a navigation `Quest? OriginalQuest`
- `IQuestService` gains `CreateFollowUpQuestAsync(int originalQuestId, DateTime proposedDate, int dungeonMasterId)`
- Method fetches the original quest's finalized player list, creates a new quest, copies signups with pre-approved state

**ViewModel:**
- `CreateFollowUpViewModel` — inherits or wraps `QuestViewModel`; adds `OriginalQuestId`, pre-populated `PlayerIds` list, single required date

**View changes:**
- `Quest/Manage/{id}`: add "Create Follow-up Quest" button (DungeonMasterOnly, only if `IsFinalized`)
- `Quest/Details/{id}`: add "Sequel to..." / "Continued in..." links if FK is set

### Table Stakes

| Feature | Why Expected | Complexity |
|---------|--------------|------------|
| Pre-fill title, description, CR, DM, player count | Core pre-population spec | Low |
| Pre-approve confirmed players from original | Core spec — no re-voting | Medium — need to identify "confirmed" signups |
| `OriginalQuestId` link (back-link on new quest) | Traceability between sessions | Low |
| Date picker (only mandatory field) | Core spec | Low |
| "Create Follow-up" button on Manage page | Entry point | Low |

### Differentiators

| Feature | Value | Complexity |
|---------|-------|------------|
| Forward link from original quest to follow-up | Narrative continuity | Low — add FK and nav property |
| Allow removing specific players before confirming | DM flexibility | Low — checkboxes in pre-fill form |
| Multiple follow-ups per quest | Campaigns with 3+ parts | Low — self-referential FK already supports it if queried as tree |
| "Create follow-up" from Quest Details (not just Manage) | Discoverability | Low |

### Anti-Features

| Anti-Feature | Why Avoid | Instead |
|--------------|-----------|---------|
| Force players to re-vote on the new date | Breaks the core premise of the feature | Pre-approve signups; just send a notification |
| Copy proposed dates from original | They're in the past | Only the new date |
| Deep quest chain navigation UI | Overengineering for a small group | Simple "Sequel to" / "Continued in" links are enough |
| Allow follow-up of non-finalized quest | Players aren't confirmed yet | Gate behind `IsFinalized` check |

### Dependencies

- No dependencies on features 1, 2, or 3
- Depends on the codebase having the `SignupRole` magic number replaced with a named enum (active in this milestone's refactor work) — because the follow-up logic needs to identify DM-role signups vs player-role signups cleanly. Safe to implement after the enum refactor task.

---

## Cross-Feature Dependency Map

```
DM Profile (#98)           — independent
Shop Filter/Sort (#96)     — independent
Image Crop (#78)           — independent; introduces SkiaSharp NuGet
Follow-up Quest (#49)      — independent; benefits from SignupRole enum refactor
```

No feature blocks another. All four can be developed in parallel. The only sequencing recommendation:
- Follow-up quest (#49): start after `SignupRole` enum magic number is replaced (already planned in this milestone's code quality work)
- Image crop (#78): verify SkiaSharp Docker compatibility before committing to Option A

---

## Feature Complexity Summary

| Feature | Complexity | New Migrations | New NuGet | New Endpoints |
|---------|------------|----------------|-----------|---------------|
| DM Profile | Medium | Yes (bio + photo columns on User) | No | ~5 (list, detail, edit GET/POST, photo) |
| Shop Filter/Sort | Low | No | No | 0 (extends existing Index) |
| Image Crop | Medium-High | Yes (crop columns on CharacterImages) | Yes (SkiaSharp) | 1 (GetAvatarPicture) |
| Follow-up Quest | Medium | Yes (OriginalQuestId on Quests) | No | 2 (GET/POST CreateFollowUp) |

---

## Sources

- Cropper.js GitHub: https://github.com/fengyuanchen/cropperjs (v2.1.1 released April 2026; v1.5.x is latest stable v1 branch)
- cdnjs cropperjs: https://cdnjs.com/libraries/cropperjs (v2.0.1 listed; v1.5.13 confirmed via unpkg)
- unpkg cropperjs v1.5.13 dist: https://app.unpkg.com/cropperjs@1.5.13/files/dist (confirmed `cropper.min.css` + `cropper.min.js` present)
- Confidence: Cropper.js CDN details — MEDIUM (confirmed file existence via unpkg; v1.5.x CDN script tags are well-documented across community sources)
- All other feature analysis derived from direct codebase inspection — HIGH confidence
