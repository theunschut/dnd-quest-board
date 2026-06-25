---
phase: quick
plan: 260624-izh
type: execute
wave: 1
depends_on: []
files_modified:
  - EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml
  - EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml
autonomous: true
requirements: [MOBILE-BUG-01, MOBILE-BUG-02, MOBILE-BUG-03, MOBILE-BUG-04]

must_haves:
  truths:
    - "Body text and headings are readable on the dark mobile background"
    - "DM name and date text are clearly visible (not near-invisible grey)"
    - "Signed up badge and status badge do not overlap on quest cards"
    - "Hamburger offcanvas opens from the right side where the button is"
    - "Page-specific CSS loads in <head>, not after script tags in <body>"
  artifacts:
    - path: "EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml"
      provides: "Fixed mobile layout with dark theme, offcanvas-end, Styles in head"
    - path: "EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml"
      provides: "Fixed quest card badge layout without overlap"
  key_links:
    - from: "_Layout.Mobile.cshtml html tag"
      to: "Bootstrap 5.3 dark-mode CSS variables"
      via: "data-bs-theme=\"dark\" attribute"
      pattern: "data-bs-theme"
    - from: "offcanvas div"
      to: "hamburger button on right side of navbar"
      via: "offcanvas-end class"
      pattern: "offcanvas-end"
---

<objective>
Fix four confirmed mobile UI bugs introduced during Phase 13 mobile view work.

Purpose: All four bugs degrade readability and UX on mobile — dark-mode text is invisible, DM/date text is barely visible, badges overlap, and the nav panel opens on the wrong side. These are regression blockers for the mobile experience.

Output: Two corrected Razor files. No new files needed.
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/STATE.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Fix _Layout.Mobile.cshtml — dark theme, offcanvas direction, Styles section placement</name>
  <files>EuphoriaInn.Service/Views/Shared/_Layout.Mobile.cshtml</files>
  <action>
Apply three fixes to _Layout.Mobile.cshtml:

**Fix 1 — Bootstrap 5.3 dark theme (Bug 1 + Bug 2):**
Change line 3 from:
  `&lt;html lang="en"&gt;`
to:
  `&lt;html lang="en" data-bs-theme="dark"&gt;`

Bootstrap 5.3 uses data-bs-theme to activate dark-mode CSS variables. Without it, --bs-body-color defaults to near-black (#212529) which is invisible on the dark #212529 background. text-muted becomes rgba(33,37,41,0.75) — nearly invisible on #343a40 card surfaces.

**Fix 2 — Offcanvas opens from correct side (Bug 4):**
Change line 28 from:
  `&lt;div class="offcanvas offcanvas-start" id="mobileNav" tabindex="-1"&gt;`
to:
  `&lt;div class="offcanvas offcanvas-end" id="mobileNav" tabindex="-1"&gt;`

The hamburger toggler button is on the right side of the navbar. offcanvas-end makes the panel slide in from the right, matching the button position.

**Fix 3 — Move Styles section to &lt;head&gt; (bonus fix):**
Remove line 164: `@await RenderSectionAsync("Styles", required: false)`
Add `@await RenderSectionAsync("Styles", required: false)` as the last line inside &lt;head&gt;, after the existing &lt;link&gt; tags (after line 11).

CSS &lt;link&gt; tags rendered by the Styles section (e.g., home.mobile.css) must be in &lt;head&gt; to avoid FOUC (flash of unstyled content). The current placement after &lt;script&gt; tags at the bottom of &lt;body&gt; is incorrect.
  </action>
  <verify>
    <automated>dotnet build EuphoriaInn.Service --no-restore -q 2>&amp;1 | tail -5</automated>
  </verify>
  <done>_Layout.Mobile.cshtml has data-bs-theme="dark" on html, offcanvas-end on the nav div, and Styles section rendered at end of head. Build succeeds.</done>
</task>

<task type="auto">
  <name>Task 2: Fix Index.Mobile.cshtml — eliminate badge overlap on quest cards</name>
  <files>EuphoriaInn.Service/Views/Home/Index.Mobile.cshtml</files>
  <action>
Fix Bug 3: the "Signed up" badge uses position-absolute top-0 end-0 m-2, and the status badge sits at the right end of the title row — both land in the top-right corner and overlap.

**Remove** the absolutely-positioned "Signed up" span (lines 61-65):
```
@if (isUserSignedUp)
{
    &lt;span class="badge bg-success position-absolute top-0 end-0 m-2"&gt;
        &lt;i class="fas fa-check me-1"&gt;&lt;/i&gt;Signed up
    &lt;/span&gt;
}
```

**Replace** the title row div (lines 68-73) with a version that stacks status + signed-up badges vertically in a single right-side column:

```razor
&lt;div class="d-flex justify-content-between align-items-start mb-1"&gt;
    &lt;h6 class="quest-card-title mb-0 pe-2"&gt;@quest.Title&lt;/h6&gt;
    &lt;div class="d-flex flex-column align-items-end gap-1"&gt;
        &lt;span class="badge @statusBadge flex-shrink-0"&gt;
            &lt;i class="@statusIcon me-1"&gt;&lt;/i&gt;@statusText
        &lt;/span&gt;
        @if (isUserSignedUp)
        {
            &lt;span class="badge bg-success flex-shrink-0"&gt;
                &lt;i class="fas fa-check me-1"&gt;&lt;/i&gt;Signed up
            &lt;/span&gt;
        }
    &lt;/div&gt;
&lt;/div&gt;
```

Also remove the `position-relative` class from the quest-card-mobile div (line 58) since absolute positioning is no longer used:
Change: `&lt;div class="quest-card-mobile position-relative"`
To:     `&lt;div class="quest-card-mobile"`
  </action>
  <verify>
    <automated>dotnet build EuphoriaInn.Service --no-restore -q 2>&amp;1 | tail -5</automated>
  </verify>
  <done>Index.Mobile.cshtml has no position-absolute badge. The title row right column is a flex-column containing status badge above signed-up badge (when present). position-relative removed from quest-card-mobile. Build succeeds.</done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| n/a | Pure view-layer CSS/HTML fix — no trust boundary changes |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-260624-01 | Information Disclosure | data-bs-theme attribute | accept | Read-only HTML attribute; reveals dark theme preference only — no sensitive data |
</threat_model>

<verification>
After both tasks complete:
1. `dotnet build` passes with no errors
2. Navigate to the mobile quest board in a browser at mobile viewport width
3. Body text is white/light-colored against the dark background
4. DM names and dates are clearly readable (not near-invisible)
5. Quest cards with "Signed up" show status badge above signed-up badge, no overlap
6. Tapping the hamburger opens the nav panel from the RIGHT side
7. Page-specific styles load without FOUC (inspect DevTools Network tab — home.mobile.css loads before body paint)
</verification>

<success_criteria>
- data-bs-theme="dark" present on &lt;html&gt; tag in _Layout.Mobile.cshtml
- offcanvas-end class on the nav div in _Layout.Mobile.cshtml
- Styles section rendered inside &lt;head&gt;, not at bottom of &lt;body&gt;
- No position-absolute badge in Index.Mobile.cshtml
- Status and signed-up badges stack vertically in a flex-column container
- dotnet build succeeds with 0 errors
</success_criteria>

<output>
After completion, create `.planning/quick/260624-izh-fix-mobile-ui-issues/260624-izh-SUMMARY.md`
</output>
