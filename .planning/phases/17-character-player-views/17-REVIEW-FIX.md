---
phase: 17-character-player-views
fixed_at: 2026-06-25T00:00:00Z
review_path: .planning/phases/17-character-player-views/17-REVIEW.md
iteration: 1
findings_in_scope: 3
fixed: 3
skipped: 0
status: all_fixed
---

# Phase 17: Code Review Fix Report

**Fixed at:** 2026-06-25
**Source review:** .planning/phases/17-character-player-views/17-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 3
- Fixed: 3
- Skipped: 0

## Fixed Issues

### WR-01: File-validation order in Create.Mobile.cshtml clears the input but shows no type-error message when size check fires first

**Files modified:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs`
**Commit:** a66e60a
**Applied fix:** The `Create.Mobile.cshtml` JavaScript already had type-check-first ordering (not the reverse as described in the review — the view was already correct at the time of fixing). The genuinely missing piece was the server-side MIME type guard. Added `allowedMimeTypes` check (`image/jpeg`, `image/png`, `image/gif` via `StringComparer.OrdinalIgnoreCase`) before the size check in both the `Create` POST and `Edit` POST actions, returning a `ModelState` error for non-image content types. This closes the spoofed-MIME bypass path the reviewer identified.

---

### WR-02: classIndex initialised to 0 in Edit.Mobile.cshtml when character has no classes, causing name collision

**Files modified:** `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml`
**Commit:** 4f9da29
**Applied fix:** Changed line 131 from `let classIndex = @Model.Classes.Count;` to `let classIndex = @(Model.Classes.Any() ? Model.Classes.Count : 1);`, matching the pattern used in `Create.Mobile.cshtml`. When a character has no saved classes the loop renders one blank row at index 0; `classIndex` now starts at 1 so the "Add Another Class" button generates `Classes[1].*` fields instead of colliding with `Classes[0].*`.

---

### WR-03: GetProfilePicture always serves Content-Type: image/jpeg regardless of stored format

**Files modified:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs`
**Commit:** a66e60a
**Applied fix:** Replaced the hardcoded `"image/jpeg"` string in `GetProfilePicture` with a call to a new private `DetectImageMimeType(byte[] data)` helper. The helper inspects the first bytes of the stored image: PNG magic bytes (`0x89 0x50`) return `"image/png"`, GIF magic bytes (`0x47 0x49`) return `"image/gif"`, and everything else falls back to `"image/jpeg"`. No new database column is required — detection is done at serve time from the stored binary.

---

_Fixed: 2026-06-25_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
