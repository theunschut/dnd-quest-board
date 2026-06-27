---
status: testing
phase: 23-admin-email-stats
source: [23-VERIFICATION.md]
started: 2026-06-27T12:00:00Z
updated: 2026-06-27T12:00:00Z
---

## Current Test

number: 1
name: Four stat cards render with live Resend API data
expected: |
  Page renders four colored stat cards with numeric counts (Sent, Delivered, Bounced, Failed)
  and a "Stats as of ..." freshness line; no alert banner is shown
awaiting: user response

## Tests

### 1. Four stat cards render with live Resend API data
Configure `EmailSettings__ResendApiKey` with a valid Resend API key, sign in as Admin,
navigate to `/Admin/EmailStats`.
expected: Four colored stat cards appear with numeric counts; "Stats as of {date}" line present; no alert banner shown
result: [pending]

### 2. Missing-key warning banner renders correctly
Ensure `ResendApiKey` is blank (default), navigate to `/Admin/EmailStats` as Admin.
expected: Yellow alert banner with "ResendApiKey not configured" heading; Refresh button present; no 500 error
result: [pending]

### 3. Cache serves cached response; ?force=true bypasses it
Load `/Admin/EmailStats` (with live key), note AsOf timestamp; reload immediately; reload with `?force=true`.
expected: First two loads show same AsOf (cache hit); force reload shows a later AsOf timestamp
result: [pending]

### 4. Admin-only enforcement in live app
Log in as a Player-role user; attempt to navigate to `/Admin/EmailStats`.
expected: Redirect to login or 403 Forbidden
result: [pending]

## Summary

total: 4
passed: 0
issues: 0
pending: 4
skipped: 0
blocked: 0

## Gaps
