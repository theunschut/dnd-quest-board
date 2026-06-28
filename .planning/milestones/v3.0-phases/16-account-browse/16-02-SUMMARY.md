---
phase: 16-account-browse
plan: "02"
subsystem: account-mobile-views
tags: [mobile, account, glass-card, razor-views, css]
status: complete

dependency_graph:
  requires:
    - "16-01: MobileViewsTests integration test stubs (ACCT-01, ACCT-02, ACCT-03)"
  provides:
    - "Login.Mobile.cshtml: full-width single-column glass card login form (ACCT-01)"
    - "Register.Mobile.cshtml: full-width single-column glass card register form (ACCT-02)"
    - "Edit.Mobile.cshtml: glass card profile edit form with HasKey checkbox (ACCT-03)"
    - "Profile.Mobile.cshtml: glass card profile display with stacked label-value layout (ACCT-03)"
    - "ChangePassword.Mobile.cshtml: glass card change password form (ACCT-03)"
    - "account.mobile.css: glass card + parchment + text-muted override shared stylesheet"
  affects:
    - "MobileViewsTests: MobileAccount* tests now GREEN (5/5 passing)"

tech_stack:
  added: []
  patterns:
    - "Glass card pattern (.account-card-mobile) matching Phase 13-15 aesthetic (D-02)"
    - "account.mobile.css shared by all 5 Account mobile views — single CSS file, no @media queries"
    - "@section Styles injection for per-view CSS loading"
    - "Stacked label-above-value layout replacing col-sm-3/col-sm-9 grid in Profile.Mobile.cshtml"
    - "btn-warning for Change Password link (filled, per CLAUDE.md button guidelines)"

key_files:
  created:
    - EuphoriaInn.Service/wwwroot/css/account.mobile.css
    - EuphoriaInn.Service/Views/Account/Login.Mobile.cshtml
    - EuphoriaInn.Service/Views/Account/Register.Mobile.cshtml
    - EuphoriaInn.Service/Views/Account/Edit.Mobile.cshtml
    - EuphoriaInn.Service/Views/Account/Profile.Mobile.cshtml
    - EuphoriaInn.Service/Views/Account/ChangePassword.Mobile.cshtml
  modified: []

decisions:
  - "btn-warning for Change Password link (not btn-outline-primary from desktop) — CLAUDE.md requires filled colored buttons"
  - "No @inject in any mobile view — auth/antiforgery already globally injected via _ViewImports.cshtml"
  - "No Layout assignment — _ViewStart.Mobile.cshtml selects _Layout.Mobile.cshtml automatically"
  - "form-text span (not div) for password hint in Register.Mobile.cshtml — consistent with rendering"

metrics:
  duration: "~7 minutes"
  completed_date: "2026-06-24"
  tasks_completed: 4
  files_modified: 6
---

# Phase 16 Plan 02: Account Mobile Views Summary

Five Account mobile views (Login, Register, Edit, Profile, ChangePassword) with a shared glass card CSS file. All 5 MobileAccount integration tests pass GREEN.

## What Was Built

### Task 1: account.mobile.css

Created `EuphoriaInn.Service/wwwroot/css/account.mobile.css` with:

- `.account-card-mobile`: glass card container (`backdrop-filter: blur(15px)`, `rgba(255,255,255,0.15)` background, 12px border-radius)
- Parchment text rules for headings and form labels (`#F4E4BC !important` with text-shadow)
- `.account-section-heading`: uppercase parchment heading for section dividers
- Faded parchment rules for `.form-text`, `small`, `.account-field-label`
- `.account-field-value`: profile field value display
- `.account-card-mobile .text-muted` scoped override — overrides global dark-brown `.text-muted` from `mobile.css`
- `.account-card-mobile .badge`: `text-shadow: none !important` suppression
- `.account-subtitle`: faded parchment subtitle helper
- No `@media` queries — device targeting handled at layout-selection layer

### Task 2: Login.Mobile.cshtml and Register.Mobile.cshtml

`Login.Mobile.cshtml`:
- `@model LoginViewModel`, exact asp-for bindings from desktop
- Glass card with h5 header (fa-sign-in-alt icon), validation summary, Email/Password/RememberMe fields
- `asp-action="Login"` with `asp-route-returnurl` chaining
- `d-grid gap-2` submit with `btn-warning btn-lg`
- "Don't have an account?" link to Register (returnUrl chained)

`Register.Mobile.cshtml`:
- `@model RegisterViewModel`, exact asp-for bindings from desktop
- Glass card with h5 + account-subtitle, Name/Email/Password/ConfirmPassword fields
- Password hint: "Password must be at least 6 characters long." (verbatim desktop text)
- No IsDungeonMaster checkbox (matches desktop which omits it)
- `d-grid gap-2` submit with `btn-warning btn-lg`
- "Already have an account?" link to Login (returnUrl chained)

### Task 3: Edit.Mobile.cshtml, Profile.Mobile.cshtml, ChangePassword.Mobile.cshtml

`Edit.Mobile.cshtml`:
- `@model EditProfileViewModel`, `asp-validation-summary="ModelOnly"` (matching desktop)
- `<input asp-for="Id" type="hidden" />` preserved verbatim
- Name, Email (type="email"), HasKey checkbox fields
- Password Management section with `.account-section-heading` and btn-warning Change Password link
- Cancel/Save button row

`Profile.Mobile.cshtml`:
- `@model ProfileViewModel`, display-only (no form)
- TempData SuccessMessage alert at top
- Stacked `account-field-label` / `account-field-value` pairs for Name and Email (no col-sm-3/col-sm-9 grid)
- `(bool?)ViewData["IsAdmin"]` and `(bool?)ViewData["IsDungeonMaster"]` cast patterns from desktop
- Building access section with fa-key/fa-times icons and `.text-muted` (overridden by scoped CSS rule)
- Home / Edit Profile button row

`ChangePassword.Mobile.cshtml`:
- `@model ChangePasswordViewModel`, `asp-validation-summary="All"` (matching desktop)
- CurrentPassword, NewPassword, ConfirmPassword fields with asp-validation-for
- Cancel→Profile / Change Password (btn-primary) button row

### Task 4: Verification Gate

Rebuilt integration tests project and ran:

```
dotnet test EuphoriaInn.IntegrationTests --filter "FullyQualifiedName~MobileAccount"
```

Result: **Passed! — Failed: 0, Passed: 5, Skipped: 0** (4 mobile + 1 desktop regression guard)

## Deviations from Plan

### Auto-applied adjustments

**1. btn-warning for Change Password link (CLAUDE.md compliance)**
- **Found during:** Task 3
- **Issue:** Desktop Edit.cshtml uses `btn-outline-primary` for the Change Password link; CLAUDE.md section "Button Guidelines" says "Always use filled in colored buttons instead of outline only"
- **Fix:** Used `btn-warning` (filled) instead of `btn-outline-primary`
- **Files modified:** `Edit.Mobile.cshtml`
- **Decision:** Logged in frontmatter decisions

**2. form-text as `<span>` not `<div>`**
- **Found during:** Task 2
- **Issue:** Plan action says `<span class="form-text">` — consistent with inline sibling of the validation span; desktop uses `<div class="form-text">`. Either is valid HTML; span chosen for inline consistency.
- **Note:** Both forms are acceptable; test only checks content string, not element type.

Otherwise: plan executed exactly as written.

## Known Stubs

None — all five views wire real ViewModel bindings, form actions, and ViewData expressions. No hardcoded empty values or placeholder text introduced.

## Threat Flags

None — this plan adds only Razor views and a CSS file. No new network endpoints, auth paths, or schema changes introduced.

## Self-Check: PASSED

- `EuphoriaInn.Service/wwwroot/css/account.mobile.css` — FOUND, contains `backdrop-filter: blur(15px)`, `#F4E4BC !important`, `.account-card-mobile .text-muted`, `.account-card-mobile .badge`, no `@media`
- `EuphoriaInn.Service/Views/Account/Login.Mobile.cshtml` — FOUND, contains `account-card-mobile`, `asp-action="Login"`, `account.mobile.css`, `btn-warning btn-lg`
- `EuphoriaInn.Service/Views/Account/Register.Mobile.cshtml` — FOUND, contains `account-card-mobile`, `asp-action="Register"`, `Password must be at least 6 characters long.`, no `IsDungeonMaster`
- `EuphoriaInn.Service/Views/Account/Edit.Mobile.cshtml` — FOUND, contains `account-card-mobile`, `asp-for="Id"`, `type="hidden"`, `asp-validation-summary="ModelOnly"`
- `EuphoriaInn.Service/Views/Account/Profile.Mobile.cshtml` — FOUND, contains `account-card-mobile`, `account-field-value`, `ViewData["IsAdmin"]`, no `col-sm-3`
- `EuphoriaInn.Service/Views/Account/ChangePassword.Mobile.cshtml` — FOUND, contains `account-card-mobile`, `asp-action="ChangePassword"`, `asp-validation-summary="All"`
- Commits a2c1084, 912ad6e, ae0570a — FOUND in git log
- `dotnet test --filter "FullyQualifiedName~MobileAccount"` — PASSED (5/5)
- Desktop views unchanged — `git diff HEAD~3 HEAD --name-only` shows only `.Mobile.cshtml` + `account.mobile.css`
