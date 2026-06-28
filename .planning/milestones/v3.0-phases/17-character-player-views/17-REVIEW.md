---
phase: 17-character-player-views
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
  - EuphoriaInn.Service/wwwroot/css/character-detail.mobile.css
  - EuphoriaInn.Service/Views/GuildMembers/Details.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/character-form.mobile.css
  - EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml
  - EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml
  - EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/players.mobile.css
  - EuphoriaInn.Service/Views/Players/Index.cshtml
findings:
  critical: 0
  warning: 3
  info: 3
  total: 6
status: issues_found
---

# Phase 17: Code Review Report

**Reviewed:** 2026-06-25
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

Phase 17 delivers four mobile views (character details, character create, character edit, players index) together with two companion CSS files and integration test stubs for CHAR-01 through PLAYER-01. The overall structure is consistent with the mobile patterns established in earlier phases: glass-card layout, parchment text palette, section-heading style, no media queries in the CSS files.

No critical security issues were found. The POST forms in `Details.Mobile.cshtml` correctly use `asp-action` tag helpers which auto-inject the anti-forgery token, matching the desktop counterpart. File-upload validation runs both client-side and server-side.

Three warnings were found: a logic divergence in the Create mobile view's file-validation order that silently accepts invalid file types when the size check fires first; a broken client-side `classIndex` initialisation in `Edit.Mobile.cshtml` that resets the counter to 0 when no classes exist, making the first dynamically added class collide in name with the rendered entry at index 0; and a `GetProfilePicture` endpoint that always returns `image/jpeg` regardless of the actual image format stored. Three info items cover an unused import, a cosmetic UI asymmetry, and a test-file naming comment.

---

## Warnings

### WR-01: File-validation order in Create.Mobile.cshtml clears the input but shows no type-error message when size check fires first

**File:** `EuphoriaInn.Service/Views/GuildMembers/Create.Mobile.cshtml:135-147`

**Issue:** The `change` handler checks file type after checking file size. When a user selects a file that is both oversized and of an invalid type, the size branch fires first (`return` on line 147), clears the input, and exits without ever checking the type. The user sees the size error — which is accurate — but the type validation never runs. If the user later selects a differently-sized invalid-type file the error message correctly appears, so the outcome is not dangerous, but the order is inconsistent with the desktop view (`Create.cshtml` line 155-175), which checks size first and type second with the same bug. The bigger risk here is that `ALLOWED_TYPES.includes(file.type)` relies on the browser-reported MIME type, which can be spoofed. The server-side validation in `GuildMembersController.cs` does not re-check MIME type (only file size), so a crafted request with a non-image MIME could potentially store arbitrary bytes in the `ProfilePicture` column.

**Fix:** In the `change` handler, perform the type check before the size check so the more descriptive error appears first. Also add a server-side MIME/extension check in the controller:
```csharp
// In GuildMembersController — after confirming file length > 0:
var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
if (!allowedMimeTypes.Contains(viewModel.ProfilePictureFile.ContentType,
    StringComparer.OrdinalIgnoreCase))
{
    ModelState.AddModelError(nameof(viewModel.ProfilePictureFile),
        "Only JPG, PNG, or GIF images are accepted.");
    return View(viewModel);
}
```

---

### WR-02: classIndex initialised to 0 in Edit.Mobile.cshtml when character has no classes, causing name collision

**File:** `EuphoriaInn.Service/Views/GuildMembers/Edit.Mobile.cshtml:131`

**Issue:** Line 131 sets:
```javascript
let classIndex = @Model.Classes.Count;
```
When `Model.Classes.Count` is 0 (a character with no classes saved — edge case, but possible given the model allows it), `classIndex` is initialised to 0. The Razor loop on line 63 also renders one blank row at index 0 (`i < 1` when `.Any()` is false). Clicking "Add Another Class" will then generate `Classes[0].Class` and `Classes[0].ClassLevel` input names, which exactly duplicate the rendered row's names. On form submit the model binder receives two values for each `Classes[0].*` field, and the result is unpredictable (typically the last value wins, silently discarding the first entry).

The desktop `Create.cshtml` initialises `classIndex` correctly with the same `(Model.Classes.Any() ? Model.Classes.Count : 1)` ternary used on line 124 of `Create.Mobile.cshtml`. The desktop `Edit.cshtml` also uses `Model.Classes.Count` directly (line 145), so the same bug exists there, but the mobile edit view should not repeat it.

**Fix:**
```javascript
// Edit.Mobile.cshtml line 131 — match Create.Mobile.cshtml's initialisation:
let classIndex = @(Model.Classes.Any() ? Model.Classes.Count : 1);
```

---

### WR-03: GetProfilePicture always serves Content-Type: image/jpeg regardless of stored format

**File:** `EuphoriaInn.Service/Controllers/Characters/GuildMembersController.cs:283`

**Issue:** The controller returns `File(profilePicture, "image/jpeg")` unconditionally. The store accepts JPEG, PNG, and GIF uploads (per the UI and allowed-types list), so non-JPEG images will be served with a MIME type that mismatches their actual encoding. Browsers that perform strict MIME sniffing may fail to display or decode the image, and the `<img>` in `Details.Mobile.cshtml` line 20 will silently show a broken image. This is a latent issue but visible to users who upload PNG or GIF portraits.

**Fix:** Persist the MIME type alongside the binary data (e.g., add a `ProfilePictureContentType` column via migration), or detect the format from the magic bytes at serve time:
```csharp
// Minimal detection approach (no new column needed):
private static string DetectImageMimeType(byte[] data) =>
    data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 ? "image/png" :
    data.Length >= 6 && data[0] == 0x47 && data[1] == 0x49 ? "image/gif" :
    "image/jpeg";

// In GetProfilePicture:
return File(profilePicture, DetectImageMimeType(profilePicture));
```

---

## Info

### IN-01: Unused `@using` import in Players/Index.Mobile.cshtml

**File:** `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml:1`

**Issue:** Line 1 imports `@using EuphoriaInn.Domain.Interfaces`, which is not referenced anywhere in the view. The desktop `Index.cshtml` has the same unused import on line 1. This is dead code.

**Fix:** Remove `@using EuphoriaInn.Domain.Interfaces` from both the mobile and desktop Players index views.

---

### IN-02: Registered Players rows have no navigation link, creating an inconsistent UX compared to DM rows

**File:** `EuphoriaInn.Service/Views/Players/Index.Mobile.cshtml:49`

**Issue:** DM rows (line 23) navigate to `/DungeonMaster/Profile/{id}` on tap and show a chevron icon. Player rows (line 49) use the class `no-link` and show no chevron. There is no player-profile route, so this is expected behaviour — however the `cursor: pointer` style still applies to `.players-row` by default in `players.mobile.css` line 27, and is only overridden to `cursor: default` for the `.no-link` variant on line 41. A player row with the `no-link` class still has `cursor: pointer` briefly before the override applies in the CSS cascade because `.players-row` specifies `cursor: pointer` and `.players-row.no-link` overrides it with `cursor: default`. This ordering is correct in specificity terms and does work, but reviewing the hover feedback: `.players-row:active` (line 34) applies the highlight background. The `.players-row.no-link:active` rule (line 43) suppresses it. This is fine as-is — flagged as info only.

**Fix:** No change required. If a player-profile feature is added in a future phase, the navigation and chevron can be wired up then. If desired for clarity, the `cursor: pointer` can be removed from `.players-row` and set only on `.players-row:not(.no-link)`.

---

### IN-03: Phase 17 tests added to the shared MobileViewsTests.cs file; class-level comment still references "Phase 13"

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:7-10`

**Issue:** The class-level XML doc comment on lines 7-10 reads "Phase 13 requirements HOME-01 through QVIEW-03" but Phase 17 tests (`CHAR-01`, `CHAR-02`, `CHAR-03`, `PLAYER-01`) have been appended. The comment is stale and will mislead future readers of the test file.

**Fix:** Update the class summary comment to reflect that the file now covers Phases 13-17 (or through the current phase), e.g.:
```csharp
/// <summary>
/// Integration test stubs for mobile view requirements across Phases 13–17.
/// Each group is labelled with its phase and requirement identifier (e.g. CHAR-01).
/// Tests start RED (mobile views do not exist yet) and go GREEN as implementation lands.
/// </summary>
```

---

_Reviewed: 2026-06-25_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
