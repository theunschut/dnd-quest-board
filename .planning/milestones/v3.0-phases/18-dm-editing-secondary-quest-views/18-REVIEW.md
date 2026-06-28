---
phase: 18-dm-editing-secondary-quest-views
reviewed: 2026-06-25T00:00:00Z
depth: standard
files_reviewed: 9
files_reviewed_list:
  - EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/quest-edit.mobile.css
  - EuphoriaInn.Service/Views/Quest/CreateFollowUp.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/quest-followup.mobile.css
  - EuphoriaInn.Service/Views/DungeonMaster/EditProfile.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/dm-editprofile.mobile.css
  - EuphoriaInn.Service/Views/QuestLog/Details.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/quest-log-detail.mobile.css
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
findings:
  critical: 0
  warning: 2
  info: 3
  total: 5
status: issues_found
---

# Phase 18: Code Review Report

**Reviewed:** 2026-06-25
**Depth:** standard
**Files Reviewed:** 9
**Status:** issues_found

## Summary

Four Razor views and four paired CSS files were reviewed, together with the integration test file that validates all Phase 18 mobile views. Authorization on the three form views (Quest Edit, CreateFollowUp, DM EditProfile) is correctly delegated to their controllers, all of which carry `[Authorize(Policy = "DungeonMasterOnly")]` — no gaps here. All `@` expressions in Razor output are HTML-encoded by default; no `@Html.Raw(...)` calls are present, so there are no XSS risks. No `@media` queries appear in any of the mobile-only CSS files. CSRF tokens are present on all POST forms via `<form asp-action="...">` tag-helper antiforgery injection.

Two warnings and three informational items were found. The most actionable is a dead `tokens` variable in `Details.Mobile.cshtml` that forces an unnecessary HTTPS round-trip on every page load. The second warning is an insufficient assertion in the `QLOG-01` integration test that passes even if the page redirects to an error view.

---

## Warnings

### WR-01: Dead `tokens` variable forces unnecessary antiforgery token computation

**File:** `EuphoriaInn.Service/Views/QuestLog/Details.Mobile.cshtml:5`

**Issue:** Line 5 calls `Antiforgery.GetAndStoreTokens(ViewContext.HttpContext)` and assigns the result to `tokens`, but `tokens` is never read anywhere in the file. The call has a real side-effect: it writes a cookie to the response and regenerates the antiforgery token store on every GET to this read-mostly page — even when the user is not authenticated and therefore cannot see the recap form. The desktop view `Quest/Details.cshtml` uses this variable to inject the token into JavaScript fetch calls (lines 842 and 864), but no equivalent JS is present in the mobile view, which relies on the standard `<form asp-action="...">` tag-helper instead.

**Fix:** Remove line 5 entirely. The `<form asp-action="UpdateRecap" ...>` tag-helper on line 85 automatically injects the `__RequestVerificationToken` hidden field without any manual antiforgery token call.

```diff
-    var tokens = Antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
```

---

### WR-02: `QLOG-01` integration test does not assert CSS link is present

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:758-761`

**Issue:** The `GetMobilePage_QuestLogDetails_ReturnsSuccessAndMobileLayout` test (lines 739-762) asserts `html.Should().Contain("quest-log-detail-main-card")` and `html.Should().Contain("quest-log-detail.mobile.css")`, which looks complete. However, the response status check (`response.StatusCode.Should().Be(HttpStatusCode.OK)`) is performed after `html` is fully read but before any HTML assertion. If the controller returns a non-200 response (e.g., a redirect or 404 because the quest is not visible), `html` will contain the error page markup and may still contain the string `"quest-log-detail-main-card"` if it happens to appear in a JS bundle or elsewhere in the layout — making the test fragile.

More concretely: the test creates a quest with `isFinalized: true` and then manually sets `FinalizedDate = DateTime.UtcNow.AddDays(-2)` via a direct DB write. But the `QuestLogController.Details` guard at line 37 requires `FinalizedDate.Value.Date > DateTime.UtcNow.AddDays(-1).Date` to be false — i.e., the finalized date must be at least one day in the past. With `AddDays(-2)` this condition is correctly satisfied today, but if the system clock skew or test execution time causes the date difference to fall exactly on a boundary, the quest could be excluded. This mirrors the same setup pattern used in DMVIEW-05 (line 691), which is consistent, but worth noting as a fragility.

The more serious gap: the test sends the request without an `Authorization` header (lines 754-757 show no auth setup). The `Details` action is not `[Authorize]`-gated, so this is intentional for unauthenticated access — but that means `ViewBag.CanEditRecap` will be `false` and the recap form will not render. The test only asserts on the card container class and CSS link, not on the recap form or any content that requires authentication, which is fine for a smoke test. Still, a test that explicitly exercises the DM recap-edit path on mobile is absent.

**Fix:** Add a second test (or extend QLOG-01) that authenticates as the quest's DM and asserts the recap `<textarea>` renders in the mobile view:

```csharp
// QLOG-01b: authenticated DM sees recap textarea
var (authClient, dmUser) = await AuthenticationHelper.CreateAuthenticatedClientWithUserAsync(
    _factory, "dm_qldet18b", "dm_qldet18b@test.com", roles: new[] { "DungeonMaster" });
// ... create quest owned by dmUser, set FinalizedDate ...
var request = new HttpRequestMessage(HttpMethod.Get, $"/QuestLog/Details/{quest.Id}");
request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
request.Headers.Authorization = authClient.DefaultRequestHeaders.Authorization;
// assert html.Should().Contain("id=\"recap\"");
```

---

## Info

### IN-01: `DungeonMasterId` hidden field in CreateFollowUp form is redundant after server-side override

**File:** `EuphoriaInn.Service/Views/Quest/CreateFollowUp.Mobile.cshtml:32`

**Issue:** Line 32 renders `<input type="hidden" asp-for="DungeonMasterId" />`. The POST handler at `QuestController.cs:751` overwrites `viewModel.OriginalQuestId` with the route value to prevent spoofing, but `DungeonMasterId` is accepted as-is from the form body. The controller then passes it to `CreateFollowUpQuestAsync` — however, looking at lines 757-783, `CreateFollowUpQuestAsync(id, token)` takes only the original quest ID, and `UpdateQuestPropertiesWithNotificationsAsync` does not accept a `DungeonMasterId` parameter. The `DungeonMasterId` field in `FollowUpQuestViewModel` is bound but never used on the POST path.

This is not a security vulnerability because the service function ignores the submitted value and derives the DM from the original quest internally — but the hidden field is confusing dead weight that may mislead future maintainers into thinking it matters. The `OriginalQuestId` hidden field (line 31) is also redundant given the server-side override on line 751, but is harmless as a UX fallback.

**Fix:** Remove `<input type="hidden" asp-for="DungeonMasterId" />` from the view, and consider removing `DungeonMasterId` from `FollowUpQuestViewModel` or marking it `[BindNever]` to make the intent explicit.

---

### IN-02: `@using` directives in view files are redundant with `_ViewImports.cshtml`

**File:** `EuphoriaInn.Service/Views/Quest/Edit.Mobile.cshtml:1-2`, `EuphoriaInn.Service/Views/Quest/CreateFollowUp.Mobile.cshtml:1-2`

**Issue:** Both mobile views open with:
```razor
@using EuphoriaInn.Domain.Interfaces
@using EuphoriaInn.Service.ViewModels.QuestViewModels
```
`_ViewImports.cshtml` already imports `EuphoriaInn.Domain.Interfaces` (line 3) and `EuphoriaInn.Service.ViewModels.QuestViewModels` (line 10) for the entire view hierarchy. These per-file `@using` directives are harmless but are inconsistent with the other mobile views in the same phase (e.g., `EditProfile.Mobile.cshtml` correctly omits them). `Details.Mobile.cshtml` in the QuestLog folder only declares `@using EuphoriaInn.Service.ViewModels.QuestLogViewModels`, which is not covered by `_ViewImports.cshtml` and is necessary.

**Fix:** Remove the two redundant `@using` directives from `Edit.Mobile.cshtml` and `CreateFollowUp.Mobile.cshtml` to match the convention of the other mobile views in this phase.

---

### IN-03: Integration test `QLOG-01` missing assertion on mobile CSS link

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:759-761`

**Issue:** On closer reading, QLOG-01 does assert `html.Should().Contain("quest-log-detail.mobile.css")` (line 761) — this is present and correct. However, none of the three Phase 18 test methods (DMVIEW-04, DMVIEW-05, DMVIEW-06, QLOG-01) assert the absence of a desktop-only marker class to prove the correct layout is selected. The pattern used in earlier phases (e.g., `HOME-01b` at line 60, `CAL-03` at line 264) provides a regression guard by checking that `html.Should().NotContain("<desktop-specific-class>")`. Without a corresponding negative assertion, a misconfigured UA routing that serves the desktop view would still pass all four Phase 18 tests as long as the CSS link appears somewhere in the shared layout.

This is low risk given the existing cross-phase regression coverage, but adding `.NotContain` guards to at least DMVIEW-04 and QLOG-01 would follow the established test pattern.

**Fix:** Add a `html.Should().NotContain("modern-card-header")` assertion to `GetMobilePage_QuestEdit_ReturnsSuccessAndMobileLayout` and `GetMobilePage_QuestLogDetails_ReturnsSuccessAndMobileLayout`, using a class name from the desktop view's layout that would not appear in the mobile view.

---

_Reviewed: 2026-06-25_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
