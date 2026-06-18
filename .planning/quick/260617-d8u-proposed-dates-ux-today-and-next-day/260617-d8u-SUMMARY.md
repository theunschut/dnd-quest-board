---
quick_id: 260617-d8u
slug: proposed-dates-ux-today-and-next-day
status: complete
date: 2026-06-17
duration_minutes: 5
tasks_completed: 2
files_modified: 2
commits:
  - d7e651a
  - dd290ab
tags:
  - ux
  - javascript
  - forms
key_files:
  modified:
    - EuphoriaInn.Service/wwwroot/js/site.js
    - EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml
---

# Quick Task 260617-d8u: Proposed Dates UX — Today at 18:00 + Next-Day Auto-Advance

**One-liner:** Proposed date inputs now default to today at 18:00 and each "Add Date" advances by exactly 1 day from the last filled date, with the follow-up form auto-populating one date on page load.

## Tasks Completed

| # | Description | Commit |
|---|-------------|--------|
| 1 | Fix site.js — setDefaultDateTime uses today; addProposedDate advances by last-date+1-day | d7e651a |
| 2 | Fix CreateFollowUp.cshtml — addDate() advances by last-date+1-day; DOMContentLoaded auto-init | dd290ab |

## Changes Made

### Task 1 — site.js

**`setDefaultDateTime(input)`**
- Removed `tomorrow` variable and `setDate(getDate() + 1)` offset
- Now sets `now.setHours(18, 0, 0, 0)` and formats `now` directly
- Result: empty inputs on Create pages default to today at 18:00 (was tomorrow)

**`addProposedDate()`**
- Removed the `setDefaultDateTime(newInput)` call
- Added last-date+1-day logic: iterates all datetime-local inputs in `#proposed-dates` (excluding the newly appended one) from the end, parses the last filled value, adds 1 day at 18:00
- Falls back to today at 18:00 when no prior input has a value

### Task 2 — CreateFollowUp.cshtml

**`addDate()`**
- After `container.appendChild(entry)`, added the same last-date+1-day logic as site.js
- Picks `allInputs[allInputs.length - 2]` (second-to-last) as the reference date since the new input is already in the DOM
- Falls back to today at 18:00 when container had no prior entries

**DOMContentLoaded handler**
- Added at the bottom of the `@section Scripts` block
- Checks if `#proposed-dates` has zero `.proposed-date-entry` elements
- Calls `addDate()` once if empty — ensures the follow-up form always loads with one date pre-filled (today at 18:00)

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- `d7e651a` present in git log
- `dd290ab` present in git log
- `EuphoriaInn.Service/wwwroot/js/site.js` modified
- `EuphoriaInn.Service/Views/Quest/CreateFollowUp.cshtml` modified
- Build: 6 projects, 0 errors, 0 warnings
