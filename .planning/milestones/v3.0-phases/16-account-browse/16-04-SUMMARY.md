---
phase: 16-account-browse
plan: "04"
subsystem: mobile-guild-members
tags: [mobile, guild-members, css, BROWSE-02]
status: complete

dependency_graph:
  requires:
    - "16-01: integration test stubs (MobileGuildMembers test RED)"
  provides:
    - "BROWSE-02: mobile Guild Members directory as single-column tappable list rows"
  affects: []

tech_stack:
  added: []
  patterns:
    - "glass card CSS pattern (rgba 0.15 background, blur 15px) — consistent with dm-profile.mobile.css"
    - "tappable list row with onclick window.location.href — consistent with Phases 13-15"
    - "GetProfilePicture route for all img src — Pitfall 5 avoidance"

key_files:
  created:
    - EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml
    - EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css
  modified: []
---

# Plan 16-04 Summary — Guild Members Mobile View

## What Was Built

Created the Guild Members mobile directory view satisfying BROWSE-02.

**Files created (2):**
- `EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml` — single-column list view replacing the desktop `character-grid`/`character-card` layout
- `EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css` — section glass cards, tappable list rows with dividers, 40×40 circular thumbnails, parchment text, retired dimming, empty states

## Key Decisions

- No `@media` queries in CSS — file is exclusively loaded by `Index.Mobile.cshtml` via `_Layout.Mobile.cshtml`
- Row `onclick` uses `window.location.href` (same pattern as Plans 13–15) — no anchor wrapping
- `GetProfilePicture` route used for all img src (Pitfall 5 from RESEARCH.md)
- No `@inject`, no `Layout =` — globally available via `_ViewImports.cshtml`
- Other Characters section omits Main badge (matches desktop behavior), adds owner line

## Test Results

| Test | Result |
|------|--------|
| `MobileGuildMembers_MobileUserAgent_RendersListRows` | ✓ Passed |
| `dotnet build EuphoriaInn.Service` | ✓ Exit 0 |

## Self-Check: PASSED

- ✅ `guild-member-row` present in CSS (no `@media`)
- ✅ `.guild-member-thumbnail` has `border-radius: 50%` and `width: 40px`
- ✅ `.guild-member-row.retired` has `opacity: 0.7`
- ✅ `.guild-section-card .text-muted` override present
- ✅ `.guild-section-card .badge` no-shadow rule present
- ✅ View contains `guild-member-row`, `guild-members.mobile.css`, `guild-section-card`
- ✅ View contains `Url.Action("GetProfilePicture"` and `Url.Action("Details"`
- ✅ View contains `Create New Character` and `Url.Action("Create")`
- ✅ View contains `No Characters Yet` and `No Other Characters`
- ✅ View contains NO `character-grid`, NO `character-card`, NO `@inject`, NO `Layout =`
- ✅ Desktop `GuildMembers/Index.cshtml` untouched

## key-files.created

- EuphoriaInn.Service/Views/GuildMembers/Index.Mobile.cshtml
- EuphoriaInn.Service/wwwroot/css/guild-members.mobile.css
