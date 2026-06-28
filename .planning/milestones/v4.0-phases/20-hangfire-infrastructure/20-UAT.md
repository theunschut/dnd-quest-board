---
status: complete
phase: 20-hangfire-infrastructure
source: [20-VERIFICATION.md]
started: 2026-06-25T00:00:00Z
updated: 2026-06-25T18:45:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Admin can access Hangfire dashboard at /hangfire
expected: Authenticated Admin user navigates to /hangfire and receives HTTP 200 with the Hangfire dashboard UI rendered
result: pass

### 2. Unauthenticated users redirected to /Account/Login
expected: Log out (or open a fresh browser session) and navigate to /hangfire — browser is redirected to /Account/Login with HTTP 302. Dashboard content is never shown. (This was a 401 before the fix.)
result: pass

### 3. Non-Admin authenticated users redirected to /Account/Login
expected: Log in as a DM or regular player (any non-Admin role) and navigate to /hangfire — browser is redirected to /Account/Login. (This was a 403 before the fix.)
result: pass

### 4. SmokeTestJob enqueues and completes without exception
expected: On startup, the Hangfire dashboard shows one completed SmokeTestJob run; container logs contain "Smoke test: IEmailService resolved successfully. Type: SmtpEmailService" (or similar); no exceptions in the log
result: pass

### 5. Background Jobs link visible in Admin nav dropdown
expected: An Admin user sees "Background Jobs" as a third item in the Admin dropdown nav; clicking it navigates to /hangfire
result: pass

## Summary

total: 5
passed: 5
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

