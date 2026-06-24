---
status: partial
phase: 14-calendar
source: [14-VERIFICATION.md]
started: 2026-06-24
updated: 2026-06-24
---

## Current Test

[awaiting human testing]

## Tests

### 1. Agenda view — no horizontal overflow at 375px
expected: Calendar page on mobile renders as a vertical list with no horizontal scrollbar at 375px viewport width
result: [approved]

### 2. Quest entry tap navigation
expected: Tapping any agenda entry card navigates to the correct Quest Details page for that quest
result: [issue] - clicking the navbar calendar button doesn't do anything
fix: fix(14) committed — removed data-bs-dismiss="offcanvas" from all anchor nav links; Bootstrap 5.3.0 calls event.preventDefault() on A elements with data-bs-dismiss, blocking navigation
fix_status: resolved — pending re-test

### 3. Vote button touch targets
expected: Yes/No/Maybe vote buttons are visually at least 44px tall and comfortably tappable without accidental misses
result: [approved]

### 4. Update Vote form POST round-trip
expected: Selecting a vote option in the Update Vote section and submitting persists the selection; reload shows the chosen vote pre-checked
result: [issue] - pressing the button to signup using the selected votes, results in a 404
fix: fix(14-03) committed — added missing Quest.Id hidden field to Choose a Date form
fix_status: resolved — pending re-test

## Summary

total: 4
passed: 2
issues: 0
pending: 2
skipped: 0
blocked: 0

## Gaps

### Issue 2: Navbar calendar navigation
status: resolved
root-cause: Bootstrap 5.3.0 calls event.preventDefault() on A elements with data-bs-dismiss="offcanvas", closing the offcanvas but blocking the href navigation. Affected all 12 anchor nav links.
fix: removed data-bs-dismiss="offcanvas" from all anchor tags; kept on close button and logout form button where Bootstrap does not block default behavior
re-test: verify Calendar link + other nav links navigate correctly
