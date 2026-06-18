---
status: partial
phase: 11-navigation-token-generation
source: [11-VERIFICATION.md]
started: 2026-06-18T00:00:00Z
updated: 2026-06-18T00:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. DM navbar "Open Omphalos" link appears and opens new tab
expected: When logged in as DM with integration configured (Omphalos URL + secret set in Admin Settings), the DM dropdown shows an "Open Omphalos" link below a divider. Clicking it opens Omphalos in a new browser tab.
result: [pending]

### 2. Quest Detail "Open Session Notes" button renders and redirects
expected: When viewing a quest's Details page as the DM (or admin) who owns it, with integration configured, the DM Controls card shows an amber "Open Session Notes" button below "Manage Quest". Clicking it redirects to Omphalos with a valid HMAC-signed URL.
result: [pending]

### 3. Quest Manage "Session Notes" card renders in sidebar
expected: When viewing a quest's Manage page as the DM (or admin), with integration configured, a "Session Notes" card appears in the sidebar after "View Public Page" with an amber "Open Session Notes" button.
result: [pending]

### 4. All Omphalos UI disappears when integration is disabled
expected: After disabling Omphalos integration in Admin Settings (toggle IsEnabled off, or clear the URL), none of the Omphalos elements appear: no "Open Omphalos" in the DM navbar dropdown, no "Open Session Notes" buttons on Details or Manage pages.
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
