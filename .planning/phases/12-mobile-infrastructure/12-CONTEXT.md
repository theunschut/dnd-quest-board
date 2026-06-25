# Phase 12: Mobile Infrastructure - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire the mobile detection pipeline so every mobile request routes through `_Layout.Mobile.cshtml` and can resolve `.Mobile.cshtml` view variants — with zero impact on desktop users and no controller, ViewModel, or domain changes. Deliverables: `MobileDetectionMiddleware`, `MobileViewLocationExpander`, `_Layout.Mobile.cshtml`, updated `_ViewStart.cshtml`, and `mobile.css` baseline. No mobile views are added in this phase — that is Phases 13-16.

</domain>

<decisions>
## Implementation Decisions

### Mobile Nav Content (Offcanvas Drawer)
- **D-01:** The mobile offcanvas nav mirrors the desktop nav exactly — same auth-conditional sections (Admin panel, DM tools, player-facing items, auth/logout). Each section renders as list items (not nested dropdowns) in the offcanvas drawer. Same Razor auth-check patterns as `_Layout.cshtml`.
- **D-02:** The mobile navbar shows the d20 icon + "Quest Board" text as the brand — consistent with the desktop navbar brand.

### CSS Strategy
- **D-03:** `_Layout.Mobile.cshtml` loads **only `mobile.css`** — the five desktop CSS files (`site.css`, `calendar.css`, `quests.css`, `shop.css`, `guild-members.css`, `dm-profile.css`) are NOT loaded. This is intentional; the desktop view content inside the mobile layout will be minimally styled until Phases 13-16 add `.Mobile.cshtml` views.
- **D-04:** `mobile.css` Phase 12 baseline includes:
  - 44px minimum height on interactive elements (buttons, form controls, nav links) — per INFRA-06
  - Mobile typography scale and spacing overrides — per INFRA-06
  - **D&D theme baseline:** Cinzel font reference and dark navbar palette, so the mobile nav shell looks on-brand even before mobile views land. This is the only styling bridge from site.css to mobile.css in Phase 12.

### Rollout Sequence
- **D-05:** Phase 12 completes before any `.Mobile.cshtml` views are added. True infrastructure-first. No proof-of-concept views in Phase 12 — those belong in Phases 13-16.

### iPad/Tablet Detection
- **D-06:** `"iPad"` remains in `MobileKeywords` — tablets get the mobile layout. This is the simplest policy and the mobile views (Phases 13-16) are vertical-scroll friendly on tablets. A "Request desktop site" escape hatch is deferred to a future phase (noted in REQUIREMENTS.md Future Requirements).

### Claude's Discretion
- Exact `MobileKeywords` array entries beyond the agreed set (`["Mobi", "Android", "iPhone", "iPad", "Windows Phone", "BlackBerry"]` from the research). Use the research-documented set.
- The named sections in `_Layout.Mobile.cshtml` (`@await RenderSectionAsync("Styles", required: false)` and `@await RenderSectionAsync("Scripts", required: false)`) — must be declared so views that push scripts/styles via `@section` don't error.
- Whether `_Layout.Mobile.cshtml` renders `@RenderBody()` inside `<main>` or `<div>` — use `<main class="container-fluid px-2 mt-2">` per the research architecture.
- `mobile.css` exact selectors and values for touch targets — use `min-height: 44px` on `.btn`, `a.nav-link`, `input`, `select`, `textarea`.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Research Documents (authoritative for this phase)
- `.planning/research/ARCHITECTURE.md` — complete implementation of `MobileDetectionMiddleware`, `MobileViewLocationExpander`, `_ViewStart.cshtml` update, `_Layout.Mobile.cshtml` structure, and file naming conventions. **Primary implementation guide.**
- `.planning/research/STACK.md` — Wangkanai.Responsive evaluation and rejection rationale; hand-rolled expander choice confirmed. Includes `curl` test commands for smoke testing.
- `.planning/research/SUMMARY.md` — cross-cutting summary of all research findings.

### Requirements
- `.planning/REQUIREMENTS.md` — INFRA-01 through INFRA-06 are the six requirements for this phase; all must be satisfied.
- `.planning/ROADMAP.md` — Phase 12 success criteria (4 criteria); read before implementing to confirm scope.

### Entry Points to Modify
- `EuphoriaInn.Service/Program.cs` — add `MobileViewLocationExpander` registration (`Configure<RazorViewEngineOptions>`) and `UseMiddleware<MobileDetectionMiddleware>()` before `UseRouting()`. Session does NOT need to move — no Wangkanai.
- `EuphoriaInn.Service/Views/_ViewStart.cshtml` — replace static `Layout = "_Layout"` with conditional based on `Context.Items["IsMobile"]`.

### Desktop Layout Reference (for nav mirroring)
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — copy auth-conditional nav Razor logic into `_Layout.Mobile.cshtml` offcanvas body; translate dropdown `<ul>` into flat `<li>` list.

### New Files to Create
- `EuphoriaInn.Service/Middleware/MobileDetectionMiddleware.cs` — NEW
- `EuphoriaInn.Service/ViewExpanders/MobileViewLocationExpander.cs` — NEW
- `EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml` — NEW
- `EuphoriaInn.Service/wwwroot/css/mobile.css` — NEW

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EuphoriaInn.Service/Views/Shared/_Layout.cshtml` — the complete auth-conditional nav Razor block is the direct source for the mobile offcanvas nav items. Copy and adapt (flatten dropdowns to list items).
- `EuphoriaInn.Service/Views/_ViewStart.cshtml` — currently `Layout = "_Layout"` one-liner; becomes a two-line conditional. Simple modification.
- `EuphoriaInn.Service/Program.cs` — `AddControllersWithViews()` is already present; `Configure<RazorViewEngineOptions>` registration goes immediately after it.

### Established Patterns
- Middleware registration order: `UseStaticFiles()` → `UseMiddleware<MobileDetectionMiddleware>()` → `UseRouting()` → `UseSession()` → `UseAuthentication()` → `UseAuthorization()`. Detection must run before routing.
- Authorization in Razor views: `@inject IAuthorizationService AuthorizationService` + `@await AuthorizationService.AuthorizeAsync(User, "PolicyName")` — same pattern in `_Layout.cshtml` must be carried into `_Layout.Mobile.cshtml`.
- CSS files use `asp-append-version="true"` for cache busting — apply to the `mobile.css` link tag too.
- No new NuGet packages — hand-rolled implementation uses only `Microsoft.AspNetCore.Mvc.Razor` (already present via web SDK).

### Integration Points
- `HttpContext.Items["IsMobile"]` is the single shared flag — set by middleware, read by `_ViewStart.cshtml`, `MobileViewLocationExpander.PopulateValues()`, and potentially Razor views.
- `RazorViewEngineOptions.ViewLocationExpanders` — the registration list; `MobileViewLocationExpander` is appended here.
- `_ViewStart.cshtml` is the layout selector; it must NOT set Layout to anything other than the two paths — no individual view should override layout on mobile.

</code_context>

<specifics>
## Specific Ideas

- The user confirmed iPad stays in mobile detection, with the implicit understanding that a "Request desktop site" opt-out will come in a future phase (already in REQUIREMENTS.md Future Requirements).
- The D&D theme baseline in mobile.css should draw from the Cinzel font already loaded via Google Fonts CDN in `_Layout.Mobile.cshtml` — no additional font loading needed.

</specifics>

<deferred>
## Deferred Ideas

- **"Request desktop site" / cookie-based override** — user asked about this during iPad discussion. Already tracked in `.planning/REQUIREMENTS.md` Future Requirements as "Switch to desktop cookie override — user-controlled escape hatch." Natural extension of the expander. Defer to a post-Phase 16 phase.

</deferred>

---

*Phase: 12-mobile-infrastructure*
*Context gathered: 2026-06-23*
