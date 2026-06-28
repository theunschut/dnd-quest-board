---
phase: 07-dm-profile-page
fixed_at: 2026-06-17T19:45:00Z
review_path: .planning/phases/07-dm-profile-page/07-REVIEW.md
iteration: 1
findings_in_scope: 4
fixed: 4
skipped: 0
status: all_fixed
---

# Phase 7: Code Review Fix Report

**Fixed at:** 2026-06-17T19:45:00Z
**Source review:** .planning/phases/07-dm-profile-page/07-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 4
- Fixed: 4
- Skipped: 0

## Fixed Issues

### WR-01: Bio-only save silently clears the profile picture

**Files modified:** `EuphoriaInn.Domain/Interfaces/IDungeonMasterProfileService.cs`, `EuphoriaInn.Domain/Services/DungeonMasterProfileService.cs`
**Commit:** 5c022a9
**Applied fix:** Added `bool removeImage = false` parameter to `UpsertProfileAsync` in both the interface and the service implementation. The update branch now passes `null` (clear) vs `imageBytes` (replace) to `UpsertProfileImageAsync` only when `imageBytes != null || removeImage`. Added inline comments documenting all three call semantics (replace, clear, keep unchanged) so the invariant is explicit. The controller call site is unchanged because the new parameter has a default value.

### WR-02: `GetDMProfilePicture` always returns `Content-Type: image/jpeg` regardless of actual format

**Files modified:** `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs`
**Commit:** e96a5f5
**Applied fix:** Replaced the hardcoded `"image/jpeg"` string with magic-byte detection. PNG header `89 50` maps to `image/png`; GIF header `47 49 46` maps to `image/gif`; everything else falls through to `image/jpeg`. No schema change required.

### WR-03: No server-side enforcement of the 5 MB image size limit

**Files modified:** `EuphoriaInn.Service/Controllers/DungeonMaster/DungeonMasterController.cs`
**Commit:** a3e1d42
**Applied fix:** Added an explicit `const long maxFileSizeBytes = 5 * 1024 * 1024` guard immediately after the null/length check and before `CopyToAsync`. If the length exceeds the limit, a `ModelState` error is added and the view is returned without reading the stream into memory. This prevents oversized blobs from reaching the database even if the Kestrel `MaxRequestBodySize` limit has been raised.

### WR-04: TOCTOU race in `UpsertProfileAsync` — double AddAsync possible under concurrent requests

**Files modified:** `EuphoriaInn.Repository/DungeonMasterProfileRepository.cs`
**Commit:** 89c4afa
**Applied fix:** Overrode `AddAsync` in `DungeonMasterProfileRepository` to catch `DbUpdateException` and retry with `UpdateAsync`, consistent with the `QuestRepository.AddAsync` pattern already in the codebase. The fix is placed in the Repository layer (where EF Core is a dependency) rather than the Domain service layer, keeping `Microsoft.EntityFrameworkCore` out of `EuphoriaInn.Domain`. A concise comment explains the `DatabaseGeneratedOption.None` PK design that makes the race possible.

---

_Fixed: 2026-06-17T19:45:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
