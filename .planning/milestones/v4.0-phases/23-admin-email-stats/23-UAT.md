---
status: complete
phase: 23-admin-email-stats
source: [23-VERIFICATION.md]
started: 2026-06-27T12:00:00Z
updated: 2026-06-28T12:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Four stat cards render with live Resend API data
Configure `EmailSettings__ResendApiKey` with a valid Resend API key, sign in as Admin,
navigate to `/Admin/EmailStats`.
expected: Four colored stat cards appear with numeric counts; "Stats as of {date}" line present; no alert banner shown
result: pass

### 2. Missing-key warning banner renders correctly
Ensure `ResendApiKey` is blank (default), navigate to `/Admin/EmailStats` as Admin.
expected: Yellow alert banner with "ResendApiKey not configured" heading; Refresh button present; no 500 error
result: pass

### 3. Cache serves cached response; ?force=true bypasses it
Load `/Admin/EmailStats` (with live key), note AsOf timestamp; reload immediately; reload with `?force=true`.
expected: First two loads show same AsOf (cache hit); force reload shows a later AsOf timestamp
result: pass

### 4. Admin-only enforcement in live app
Log in as a Player-role user; attempt to navigate to `/Admin/EmailStats`.
expected: Redirect to login or 403 Forbidden
result: pass

## Summary

total: 4
passed: 4
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps
