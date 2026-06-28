---
phase: 20-hangfire-infrastructure
plan: "02"
subsystem: navigation
tags: [hangfire, admin, navigation, ui]
status: complete

dependency_graph:
  requires: []
  provides:
    - Admin dropdown Background Jobs nav link (href="/hangfire")
  affects:
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml

tech_stack:
  added: []
  patterns:
    - Plain href for middleware-served routes (not MVC tag helpers)

key_files:
  modified:
    - EuphoriaInn.Service/Views/Shared/_Layout.cshtml

decisions:
  - Use plain href="/hangfire" instead of asp-controller/asp-action (Hangfire is middleware, not MVC)
  - No additional authorization guard on the <li> — visibility controlled by existing AdminOnly block

metrics:
  duration: "~2 minutes"
  completed: "2026-06-25"
  tasks_completed: 1
  tasks_total: 1
  files_changed: 1
---

# Phase 20 Plan 02: Admin Nav Link Summary

Admin dropdown in `_Layout.cshtml` now includes a third entry — "Background Jobs" — linking to `/hangfire` via plain `href`, protected by the existing `AdminOnly` authorization guard.

## Tasks Completed

| Task | Description | Commit | Files |
|------|-------------|--------|-------|
| 1 | Insert Background Jobs nav link into Admin dropdown | 4eeaff3 | `_Layout.cshtml` |

## What Was Built

Added a `<li>` element containing `<a class="dropdown-item" href="/hangfire">` with `<i class="fas fa-tasks me-2"></i>Background Jobs` to the Admin dropdown in `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`.

The insertion is placed after the Quest Management item and before the closing `</ul>` of the Admin dropdown menu. The link is inside the existing `@if ((await AuthorizationService.AuthorizeAsync(User, "AdminOnly")).Succeeded)` block — no additional auth check was added on the `<li>` itself.

Key implementation decisions:
- `href="/hangfire"` (plain attribute) — Hangfire dashboard is served by middleware, not an MVC controller action, so `asp-controller`/`asp-action` tag helpers would generate incorrect URLs
- `class="dropdown-item"` — consistent with User Management and Quest Management items
- `fa-tasks` FontAwesome icon with `me-2` spacing — per UI-SPEC.md conventions

## Deviations from Plan

None — plan executed exactly as written.

## Verification

- `href="/hangfire"` appears exactly once in `_Layout.cshtml` (line 49)
- `fa-tasks` icon appears exactly once (line 50)
- No `asp-controller="Hangfire"` or MVC tag helpers used
- `dotnet build EuphoriaInn.Service/EuphoriaInn.Service.csproj --no-restore` exits with code 0, 0 warnings, 0 errors

## Self-Check: PASSED

- [x] `_Layout.cshtml` modified and contains `href="/hangfire"` — FOUND
- [x] Commit `4eeaff3` exists — FOUND
- [x] Build passes (0 errors, 0 warnings)
