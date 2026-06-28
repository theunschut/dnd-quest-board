---
status: partial
phase: 18-dm-editing-secondary-quest-views
source: [18-VERIFICATION.md]
started: 2026-06-25T13:01:26.482Z
updated: 2026-06-25T13:01:26.482Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. Quest Edit mobile layout
expected: No horizontal overflow at 375px width; "Add Another Date Option" button calls addProposedDate() and a new date row appears in the form
result: [pending]

### 2. CreateFollowUp datetime inputs + JS
expected: datetime-local picker opens natively on mobile; Add Date / Remove buttons work; date entries are renumbered correctly
result: [pending]

### 3. DM EditProfile photo upload validation
expected: Selecting a file > DM_MAX_FILE_SIZE shows a client-side error; selecting a disallowed type (not jpg/png/gif/webp) also shows a client-side error before form submit
result: [pending]

### 4. QuestLog Details CanEditRecap + Building Access badge
expected: DM sees recap textarea form + Manage Quest button; non-DM player sees read-only recap display; Building Access badge is green (bg-success) when key available, red (bg-danger) when not
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
