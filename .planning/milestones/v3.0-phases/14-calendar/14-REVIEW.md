---
phase: 14-calendar
reviewed: 2026-06-24T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs
  - EuphoriaInn.Service/wwwroot/css/calendar.mobile.css
  - EuphoriaInn.Service/Views/Calendar/Index.Mobile.cshtml
  - EuphoriaInn.Service/Views/Shared/_Calendar.Mobile.cshtml
  - EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml
  - EuphoriaInn.Service/wwwroot/css/quests.mobile.css
findings:
  critical: 0
  warning: 3
  info: 3
  total: 6
status: issues_found
---

# Phase 14: Code Review Report

**Reviewed:** 2026-06-24
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Phase 14 adds the mobile calendar agenda view (`Index.Mobile.cshtml`), the vote-button partial (`_Calendar.Mobile.cshtml`), the mobile quest-details page (`Details.Mobile.cshtml`), and their CSS files, backed by integration tests for each requirement. The architecture is sound: the `MobileViewLocationExpander` cleanly resolves `.Mobile.cshtml` variants for controller views and also for `Html.PartialAsync` calls, so the hardcoded `"_Calendar"` partial name in `Details.Mobile.cshtml` does resolve to `_Calendar.Mobile.cshtml` at runtime.

Three warnings were found — two logic issues in `Details.Mobile.cshtml` and one unreliable test assertion — and three informational items.

## Warnings

### WR-01: Wrong partial name called from both voting sections in Details.Mobile.cshtml

**File:** `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml:109` and `145`
**Issue:** Both the "Update vote" and "Initial vote" forms call `Html.PartialAsync("_Calendar", calendarMonth)`. The `MobileViewLocationExpander` does rewrite this to `_Calendar.Mobile.cshtml` on mobile requests, which is correct behaviour today. However, since `Details.Mobile.cshtml` is itself a mobile-only file, the `MobileViewLocationExpander` will always resolve `"_Calendar"` to `"_Calendar.Mobile"` when it is rendered — so this is not currently broken. The risk is forward-looking: if the expander ever changes scope or is bypassed in a test setup, `Details.Mobile.cshtml` would silently fall through to the desktop `_Calendar.cshtml` partial, which does not contain `calendar-date-entry-mobile` elements and would cause CAL-05 to fail without a clear error. Calling the partial by its full name removes the ambiguity.

**Fix:**
```csharp
// Line 109 (update vote form) and line 145 (initial vote form):
// Change:
@await Html.PartialAsync("_Calendar", calendarMonth)
// To:
@await Html.PartialAsync("_Calendar.Mobile", calendarMonth)
```

---

### WR-02: "Done" status boundary condition uses redundant AddDays(-1) instead of clear past-date check

**File:** `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml:39`
**Issue:** The condition `Model.Quest.FinalizedDate.Value.Date <= DateTime.UtcNow.AddDays(-1).Date` is intended to mark a quest as "Done" once its date has passed. However, this expression is logically equivalent to `< DateTime.UtcNow.Date` (strictly yesterday or earlier), but obscures intent and contains a potential drift: `AddDays(-1)` is evaluated from `DateTime.UtcNow` at render time, which means a quest finalised for today shows "Finalized" all day — which is correct. The issue is the expression is brittle: the desktop `Details.cshtml` uses a different threshold check, creating an inconsistency between mobile and desktop status badge logic. Divergence here means a quest could show "Done" on mobile but "Finalized" on desktop (or vice versa) depending on the exact comparison used in the other view.

**Fix:** Use the same expression as the desktop view. If the desktop view uses `< DateTime.UtcNow.Date`, change to:
```csharp
if (Model.Quest?.IsFinalized == true && Model.Quest.FinalizedDate.HasValue
    && Model.Quest.FinalizedDate.Value.Date < DateTime.UtcNow.Date)
```
This is unambiguous, matches the intent ("date is in the past"), and removes the implicit magic of `-1`.

---

### WR-03: HOME-04 test copies Authorization header from authClient to shared _client — assertion may not verify authenticated state

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:126`
**Issue:** The `HOME-04` test creates an `authClient` with a `"Test ..."` Authorization header, then copies that header onto a new `HttpRequestMessage` sent via `_client`. Because `_client` was created from the factory with no handler for the auth cookie, the header copy relies entirely on the test auth scheme being stateless and accepted by the server. If the server-side test auth handler validates the request client or session context, the request may silently proceed as unauthenticated, causing the "Signed up" badge assertion to pass for the wrong reason (or fail unpredictably). All other authenticated tests use the `authClient` returned by the helper directly, which is the correct pattern.

**Fix:** Use the `authClient` directly for the request, setting the `User-Agent` header on it rather than copying the auth header to `_client`:
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "/");
request.Headers.TryAddWithoutValidation("User-Agent", MobileUserAgent);
var response = await authClient.SendAsync(request, TestContext.Current.CancellationToken);
var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
```

## Info

### IN-01: ViewBag shared state used as implicit side-channel between Details.Mobile.cshtml and _Calendar.Mobile.cshtml

**File:** `EuphoriaInn.Service/Views/Quest/Details.Mobile.cshtml:108` and `_Calendar.Mobile.cshtml:9`
**Issue:** `Details.Mobile.cshtml` sets `ViewData["UpdateVoteIndexLookup"]` inside the `foreach` loop at line 108 and `ViewBag.VoteIndexLookup` at line 137. The partial `_Calendar.Mobile.cshtml` reads both back via `ViewData` and `ViewBag`. This implicit communication through shared state makes the contract between the view and its partial invisible — nothing at the call site (`PartialAsync(...)`) signals what context the partial expects. If a future caller renders `_Calendar.Mobile.cshtml` without setting these ViewBag keys, it silently gets `voteIndex = -1` and renders no vote buttons, with no error.

**Suggestion:** Pass an explicit view-data dictionary to `PartialAsync` for the keys the partial depends on:
```csharp
@await Html.PartialAsync("_Calendar.Mobile", calendarMonth, new ViewDataDictionary(ViewData)
{
    ["UpdateVoteIndexLookup"] = updateVoteIndexLookup
})
```

---

### IN-02: agenda-quest-entry uses onclick for navigation instead of an anchor element

**File:** `EuphoriaInn.Service/Views/Calendar/Index.Mobile.cshtml:55`
**Issue:** Quest entries in the agenda use `onclick="window.location.href='...'"` on a `<div>` element. This prevents keyboard navigation (the element is not focusable), breaks middle-click to open in a new tab, and gives screen readers no semantic link. The element has a `cursor: pointer` style but is not announced as interactive by assistive technology.

**Suggestion:** Replace the `<div>` with an `<a>` element or wrap it in an `<a>`:
```html
<a href="@Url.Action("Details", "Quest", new { id = questOnDay.Quest.Id })"
   class="agenda-quest-entry @(questOnDay.IsFinalized ? "agenda-quest-finalized" : "agenda-quest-proposed") d-block text-decoration-none">
    ...
</a>
```
Remove the `cursor: pointer` and `onclick` from the current `<div>`.

---

### IN-03: CAL-02 test asserts "19:00" but the quest date is on the 15th — may fail when month boundary shifts

**File:** `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs:245-253`
**Issue:** `CAL-02` creates a `ProposedDate` on day 15 of the current month at 19:00 UTC, then asserts the rendered HTML contains `"19:00"`. This is robust for the time assertion, but the comment says "Day label format is 'SATURDAY, JUNE 14'" (line 252) — the comment date is one day off from the data (day 15 vs 14). This does not affect test correctness since the assertion only checks for `"19:00"`, but the stale comment will confuse future readers.

**Suggestion:** Update the comment to match the actual test date:
```csharp
// Day label format is e.g. "SATURDAY, JUNE 15" — assert at least the time portion is present
```

---

_Reviewed: 2026-06-24_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
