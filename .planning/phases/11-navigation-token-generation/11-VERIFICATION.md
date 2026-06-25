---
phase: 11-navigation-token-generation
verified: 2026-06-18T21:30:00Z
status: human_needed
score: 15/15 must-haves verified
overrides_applied: 0
human_verification:
  - test: "Log in as a DM with integration configured (Omphalos URL + secret set, IsEnabled = true). Open the Dungeon Master navbar dropdown."
    expected: "A divider and 'Open Omphalos' link appear at the bottom of the dropdown. Clicking the link opens the Omphalos base URL in a new browser tab."
    why_human: "ViewComponent rendering and new-tab behaviour require a live browser. The ViewComponent returns Content(string.Empty) when unconfigured — the absence of the link when disabled also needs visual confirmation."
  - test: "As the same DM, navigate to a Quest Detail page for a quest you own."
    expected: "Inside the DM Controls card an 'Open Session Notes' amber button (btn-warning) appears below 'Manage Quest'. Clicking it performs a full-page redirect (same tab) to a URL beginning with the Omphalos base URL followed by /api/sso/open-quest and query parameters questId, questTitle, username, expiry, sig."
    why_human: "Button visibility is gated by ViewBag.ShowOmphalosButton — a live round-trip is required to confirm the flag reaches the view and the button renders correctly in context."
  - test: "As the same DM, navigate to the Quest Manage page for the same quest."
    expected: "A 'Session Notes' card appears in the right sidebar between 'View Public Page' and any subsequent cards. The card contains an 'Open Session Notes' amber button. Clicking it redirects (same tab) to the signed Omphalos URL."
    why_human: "Manage page sidebar layout requires visual inspection to confirm card placement and styling. Same redirect behaviour as Detail page."
  - test: "Disable the integration in Admin Settings (uncheck 'Integration Enabled' and save). Then reload the DM navbar dropdown, a Quest Detail page, and a Quest Manage page."
    expected: "No Omphalos link appears in the navbar dropdown (no divider, no 'Open Omphalos'). No 'Open Session Notes' button appears on Detail or Manage. GET /Quest/LaunchOmphalos/{id} returns 404."
    why_human: "The disabled-state removal of all Omphalos UI elements spans three distinct render surfaces. The 404 is covered by integration tests but the UI absence requires visual confirmation across all three surfaces simultaneously."
---

# Phase 11: Navigation + Token Generation — Verification Report

**Phase Goal:** Deliver HMAC-SHA256 signed URL token service and all Omphalos UI navigation elements — DM navbar link, quest Details "Open Session Notes" button, quest Manage "Session Notes" card — fully gated by integration configuration.
**Verified:** 2026-06-18T21:30:00Z
**Status:** human_needed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
|----|-------|--------|----------|
| 1  | IIntegrationTokenService.GenerateSignedUrl returns a URL containing /api/sso/open-quest | ✓ VERIFIED | `IntegrationTokenService.cs` line 26: `$"{omphalosBaseUrl.TrimEnd('/')}/api/sso/open-quest"` |
| 2  | The canonical MAC message is alphabetical: expiry=...&questId=...&questTitle=...&username=... | ✓ VERIFIED | `IntegrationTokenService.cs` line 18: `$"expiry={expiry}&questId={questId}&questTitle={encodedTitle}&username={lowerUser}"` |
| 3  | Username is lowercased in both the MAC message and the URL parameter | ✓ VERIFIED | `var lowerUser = username.ToLower()` used in both the message and the URL query string |
| 4  | QuestTitle is percent-encoded using Uri.EscapeDataString (spaces become %20) | ✓ VERIFIED | `var encodedTitle = Uri.EscapeDataString(questTitle)` — unit test `GenerateSignedUrl_PercentEncodesQuestTitle` asserts `Dragon%27s%20Lair` |
| 5  | Token expiry is DateTimeOffset.UtcNow + 300 seconds | ✓ VERIFIED | `var expiry = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds()` — unit test `GenerateSignedUrl_ExpiryIsInFuture` asserts range `before+299` to `after+1` |
| 6  | LaunchOmphalos returns NotFound when settings.IsConfigured is false | ✓ VERIFIED | `QuestController.cs` line 812: `if (!settings.IsConfigured) return NotFound()` — integration test `LaunchOmphalos_WhenIntegrationDisabled_ShouldReturn404` and `LaunchOmphalos_WhenOmphalosUrlBlank_ShouldReturn404` |
| 7  | LaunchOmphalos returns Redirect to signed URL when settings.IsConfigured is true | ✓ VERIFIED | `QuestController.cs` line 830: `return Redirect(signedUrl)` — integration tests `LaunchOmphalos_WhenIntegrationEnabled_ShouldRedirectToOmphalosUrl` and `LaunchOmphalos_RedirectUrl_ContainsExpectedQueryParameters` |
| 8  | ViewBag.ShowOmphalosButton is set on Details GET and Manage GET | ✓ VERIFIED | `QuestController.cs` line 249 (Details): `ViewBag.ShowOmphalosButton = settings.IsConfigured && currentUser != null && (... || isAdmin)`; line 651 (Manage): `ViewBag.ShowOmphalosButton = omphalosSettings.IsConfigured && (isQuestDm || isAdmin)` |
| 9  | QuestController builds without compile errors after constructor extension | ✓ VERIFIED | Constructor at lines 13-21 contains both `IAdminSettingService adminSettingService` and `IIntegrationTokenService integrationTokenService`; tests confirmed passing (34 unit + 80 integration, 0 failures) |
| 10 | DM navbar dropdown shows 'Open Omphalos' link when integration is configured | ✓ VERIFIED (automated portion) | `_Layout.cshtml` line 81: `@await Component.InvokeAsync("OmphalosNavItem")` inside DM dropdown `<ul>`; ViewComponent returns `View(settings)` when `IsConfigured`. Visual rendering requires human check. |
| 11 | DM navbar dropdown shows NO Omphalos link when integration is disabled | ✓ VERIFIED (automated portion) | `OmphalosNavItemViewComponent.cs` line 11: `if (!settings.IsConfigured) return Content(string.Empty)` — no HTML emitted when unconfigured. Visual absence requires human check. |
| 12 | The 'Open Omphalos' navbar link opens in a new tab (target=_blank) | ✓ VERIFIED (automated portion) | `Default.cshtml` line 4: `target="_blank" rel="noopener noreferrer"`. Browser tab behaviour requires human check. |
| 13 | Quest Details page shows 'Open Session Notes' button inside DM Controls card when ShowOmphalosButton is true | ✓ VERIFIED (automated portion) | `Details.cshtml` lines 584-591: `@if ((bool)(ViewBag.ShowOmphalosButton ?? false))` containing `btn btn-warning w-100 mt-2` anchor to `LaunchOmphalos`. Live rendering requires human check. |
| 14 | Quest Manage page shows 'Open Session Notes' button in a new 'Session Notes' card when ShowOmphalosButton is true | ✓ VERIFIED (automated portion) | `Manage.cshtml` lines 488-501: ShowOmphalosButton-gated `modern-card` with `<h5>Session Notes</h5>` and `btn btn-warning w-100` button. Live rendering requires human check. |
| 15 | The dropdown divider is inside Default.cshtml (not in _Layout.cshtml) to prevent orphaned hr | ✓ VERIFIED | `Default.cshtml` line 2: `<li><hr class="dropdown-divider"></li>`. `_Layout.cshtml` contains `dropdown-divider` only in Admin dropdown (line 48) and user profile dropdown (line 130) — not in the DM dropdown. |

**Score:** 15/15 truths verified (4 require human confirmation of live rendering)

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `EuphoriaInn.Domain/Interfaces/IIntegrationTokenService.cs` | Token service contract | ✓ VERIFIED | Exists; `public interface IIntegrationTokenService` with `GenerateSignedUrl(string, int, string, string, string)` |
| `EuphoriaInn.Domain/Services/IntegrationTokenService.cs` | HMAC-SHA256 URL generation | ✓ VERIFIED | Exists; contains `HMACSHA256.HashData`, `Convert.ToHexString`, `TrimEnd`, `Uri.EscapeDataString` |
| `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` | DI registration | ✓ VERIFIED | Contains `services.AddTransient<IIntegrationTokenService, IntegrationTokenService>()` at line 24 |
| `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` | LaunchOmphalos action + ShowOmphalosButton ViewBag | ✓ VERIFIED | Constructor has both new parameters; `LaunchOmphalos` action at line 809; `ViewBag.ShowOmphalosButton` at lines 249 and 651 |
| `EuphoriaInn.Service/Components/OmphalosNavItemViewComponent.cs` | ViewComponent that checks IsConfigured | ✓ VERIFIED | Exists; injects `IAdminSettingService`; returns `Content(string.Empty)` when not configured, `View(settings)` when configured |
| `EuphoriaInn.Service/Views/Shared/Components/OmphalosNavItem/Default.cshtml` | Navbar dropdown item HTML with divider | ✓ VERIFIED | Exists; contains `dropdown-divider`, `target="_blank"`, `rel="noopener noreferrer"`, `fa-external-link-alt`, "Open Omphalos"; model is `IntegrationSettings` |
| `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` | ViewComponent invocation in DM dropdown | ✓ VERIFIED | Line 81: `@await Component.InvokeAsync("OmphalosNavItem")` placed after "Edit My Profile" `</li>`, before `</ul>` |
| `EuphoriaInn.Service/Views/Quest/Details.cshtml` | Open Session Notes button in DM Controls card | ✓ VERIFIED | Lines 584-591: null-safe cast `(bool)(ViewBag.ShowOmphalosButton ?? false)`, `btn btn-warning w-100 mt-2`, `fa-book-open`, `Model.Quest?.Id` |
| `EuphoriaInn.Service/Views/Quest/Manage.cshtml` | Session Notes card with Open Session Notes button | ✓ VERIFIED | Lines 488-501: null-safe cast, `modern-card`, `<h5>Session Notes</h5>`, `btn btn-warning w-100`, `fa-book-open`, `Model.Id` |
| `EuphoriaInn.UnitTests/Services/IntegrationTokenServiceTests.cs` | 7 unit tests covering TOKEN-01 through TOKEN-04 | ✓ VERIFIED | 7 `[Fact]` methods confirmed; covers endpoint path, trailing slash trim, lowercase username, percent-encoding, 64-char hex sig, future expiry, different secrets |
| `EuphoriaInn.IntegrationTests/Controllers/LaunchOmphalosIntegrationTests.cs` | 6 integration tests covering TOKEN-05, NAV-03, NAV-04, NAV-05 | ✓ VERIFIED | 6 `[Fact]` methods confirmed; covers unauthenticated, player role, disabled, blank URL, redirect to Omphalos, query parameters |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `QuestController.LaunchOmphalos` | `IIntegrationTokenService.GenerateSignedUrl` | constructor injection | ✓ WIRED | `integrationTokenService` injected via primary constructor; called at line 823 with `settings.OmphalosUrl!`, `quest.Id`, `quest.Title`, `currentUser.Name.ToLower()`, `settings.OmphalosSharedSecret!` |
| `QuestController.Details GET` | `ViewBag.ShowOmphalosButton` | `adminSettingService.GetSettingsAsync` | ✓ WIRED | Line 248: `var settings = await adminSettingService.GetSettingsAsync(token)` then line 249: `ViewBag.ShowOmphalosButton = settings.IsConfigured && ...` |
| `ServiceExtensions.AddDomainServices` | `IIntegrationTokenService` | `AddTransient` | ✓ WIRED | Line 24: `services.AddTransient<IIntegrationTokenService, IntegrationTokenService>()` |
| `_Layout.cshtml DM dropdown` | `OmphalosNavItemViewComponent` | `@await Component.InvokeAsync("OmphalosNavItem")` | ✓ WIRED | Line 81 of `_Layout.cshtml` — inside the DM-only dropdown `<ul>`, after "Edit My Profile" |
| `OmphalosNavItemViewComponent.InvokeAsync` | `IAdminSettingService.GetSettingsAsync` | constructor injection | ✓ WIRED | `adminSettingService.GetSettingsAsync()` called on line 10 of ViewComponent |
| `Details.cshtml DM Controls card-body` | `LaunchOmphalos endpoint` | `@Url.Action("LaunchOmphalos", "Quest", ...)` | ✓ WIRED | Line 586-587: `href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Quest?.Id })"` |
| `Manage.cshtml Session Notes card` | `LaunchOmphalos endpoint` | `@Url.Action("LaunchOmphalos", "Quest", ...)` | ✓ WIRED | Lines 495-496: `href="@Url.Action("LaunchOmphalos", "Quest", new { id = Model.Id })"` |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|-------------------|--------|
| `IntegrationTokenService.cs` | `settings.OmphalosUrl`, `settings.OmphalosSharedSecret` | `IAdminSettingService.GetSettingsAsync` → DB query via `AdminSettingRepository` | Yes — DB-backed via Phase 10 | ✓ FLOWING |
| `OmphalosNavItemViewComponent.cs` | `settings` (IntegrationSettings) | `adminSettingService.GetSettingsAsync()` → DB query | Yes | ✓ FLOWING |
| `Details.cshtml` — ShowOmphalosButton gate | `ViewBag.ShowOmphalosButton` | Set in `QuestController.Details GET` from DB-backed `settings.IsConfigured` | Yes | ✓ FLOWING |
| `Manage.cshtml` — ShowOmphalosButton gate | `ViewBag.ShowOmphalosButton` | Set in `QuestController.Manage GET` from DB-backed `omphalosSettings.IsConfigured` | Yes | ✓ FLOWING |

### Behavioral Spot-Checks

Step 7b: SKIPPED for the view layer (requires live browser and running server). The backend endpoint is covered by the confirmed-passing integration test suite (34 unit + 80 integration, 0 failures per prompt context).

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| TOKEN-01 | 11-01-PLAN.md | IIntegrationTokenService generates signed redirect URL | ✓ SATISFIED | `IIntegrationTokenService.cs` + `IntegrationTokenService.cs` exist and implement `GenerateSignedUrl` |
| TOKEN-02 | 11-01-PLAN.md | Canonical MAC message alphabetical with questId in MAC | ✓ SATISFIED | Message string: `expiry=...&questId=...&questTitle=...&username=...` — correct alphabetical order, questId included |
| TOKEN-03 | 11-01-PLAN.md | Tokens expire after 300 seconds | ✓ SATISFIED | `DateTimeOffset.UtcNow.AddSeconds(300)` — unit test confirms range |
| TOKEN-04 | 11-01-PLAN.md | Username normalized to lowercase in both MAC and URL | ✓ SATISFIED | `username.ToLower()` — lowercase used in message and URL; unit test asserts `username=uppercase_dm` |
| TOKEN-05 | 11-01-PLAN.md | LaunchOmphalos GET generates signed URL and returns Redirect; 404 when disabled | ✓ SATISFIED | Action exists, `[Authorize(Policy = "DungeonMasterOnly")]`, `NotFound()` when `!IsConfigured`, `Redirect(signedUrl)` when configured |
| NAV-01 | 11-02-PLAN.md | _Layout.cshtml renders OmphalosNavItem ViewComponent; shows link only when configured | ✓ SATISFIED | `@await Component.InvokeAsync("OmphalosNavItem")` in DM dropdown; ViewComponent gates on `IsConfigured` |
| NAV-02 | 11-02-PLAN.md | "Open Omphalos" opens Omphalos base URL in new tab (plain navigation, no token) | ✓ SATISFIED | `Default.cshtml`: `href="@Model.OmphalosUrl"` with `target="_blank" rel="noopener noreferrer"` — no token generation |
| NAV-03 | 11-01-PLAN.md + 11-02-PLAN.md | Quest Detail page shows "Open Session Notes" button when configured and user is DM/Admin | ✓ SATISFIED | `ViewBag.ShowOmphalosButton` set in `QuestController.Details`; `Details.cshtml` renders button when true |
| NAV-04 | 11-01-PLAN.md + 11-02-PLAN.md | Quest Manage page shows "Open Session Notes" button under same conditions | ✓ SATISFIED | `ViewBag.ShowOmphalosButton` set in `QuestController.Manage`; `Manage.cshtml` renders Session Notes card when true |
| NAV-05 | 11-01-PLAN.md + 11-02-PLAN.md | When disabled/unconfigured, no Omphalos UI elements appear; LaunchOmphalos returns 404 | ✓ SATISFIED | ViewComponent returns `Content(string.Empty)`; ViewBag flag is false; `LaunchOmphalos` returns `NotFound()` when `!IsConfigured` |

All 10 requirements from Phase 11 plans are satisfied. No orphaned requirements detected — REQUIREMENTS.md lists exactly TOKEN-01 through TOKEN-05 and NAV-01 through NAV-05 as Phase 11, all mapped and complete.

### Anti-Patterns Found

| File | Pattern | Severity | Assessment |
|------|---------|----------|------------|
| None | — | — | No TODOs, FIXMEs, placeholder comments, empty implementations, or stub returns found in any phase 11 artifact |

`return Content(string.Empty)` in `OmphalosNavItemViewComponent` is intentional — it is the correct pattern for a ViewComponent that should render nothing when unconfigured, not a stub.

### Human Verification Required

The automated evidence fully confirms all backend behavior (token generation, DI wiring, authorization gates, redirect logic) and the presence and structural correctness of all view elements. However, four behaviors require live browser confirmation:

#### 1. DM Navbar "Open Omphalos" link renders and opens new tab

**Test:** Log in as a DM with integration configured (Omphalos URL + secret + IsEnabled = true). Open the Dungeon Master navbar dropdown.
**Expected:** A horizontal divider followed by an "Open Omphalos" link with an external-link icon appears at the bottom of the dropdown. Clicking opens the Omphalos base URL in a new browser tab (not the same tab).
**Why human:** ViewComponent rendering and new-tab behaviour require a live browser; Content(string.Empty) empty-state also requires visual confirmation.

#### 2. Quest Detail "Open Session Notes" button renders for DM and triggers redirect

**Test:** As a DM, navigate to a Quest Detail page for a quest you own (integration configured).
**Expected:** Inside the DM Controls card an amber "Open Session Notes" button appears below the "Manage Quest" button. Clicking performs a same-tab redirect to a URL starting with the configured Omphalos base URL followed by `/api/sso/open-quest` and correct query parameters.
**Why human:** ViewBag flag traversal to rendered button requires visual inspection. Redirect URL structure is tested by integration tests but visual button placement/style needs confirmation.

#### 3. Quest Manage "Session Notes" card renders in sidebar

**Test:** As a DM, navigate to the Quest Manage page for a quest you own (integration configured).
**Expected:** A "Session Notes" card appears in the right sidebar after the "View Public Page" card. The card contains a full-width amber "Open Session Notes" button. Clicking performs a same-tab redirect to the signed Omphalos URL.
**Why human:** Sidebar card placement and visual ordering require live rendering confirmation.

#### 4. All Omphalos UI disappears when integration is disabled

**Test:** Disable integration in Admin Settings (uncheck "Integration Enabled", save). Reload DM navbar, a Quest Detail page, and a Quest Manage page.
**Expected:** No "Open Omphalos" in navbar dropdown (no divider, no link). No "Open Session Notes" button on Detail or Manage. GET `/Quest/LaunchOmphalos/{id}` returns 404.
**Why human:** The simultaneous absence of elements across three distinct render surfaces (layout + two views) requires visual confirmation. The 404 is covered by integration tests.

---

_Verified: 2026-06-18T21:30:00Z_
_Verifier: Claude (gsd-verifier)_
