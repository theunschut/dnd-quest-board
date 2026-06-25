---
status: complete
phase: 20-hangfire-infrastructure
source: [20-VERIFICATION.md]
started: 2026-06-25T00:00:00Z
updated: 2026-06-25T12:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Admin can access Hangfire dashboard at /hangfire
expected: Authenticated Admin user navigates to /hangfire and receives HTTP 200 with the Hangfire dashboard UI rendered
result: pass

### 2. Unauthenticated users redirected to /Account/Login
expected: A request to /hangfire without a session cookie receives HTTP 302 redirect to /Account/Login
result: issue
reported: "if i log out and try to access /hangfire i'm getting a 401 error, not a redirect?"
severity: major

### 3. Non-Admin authenticated users redirected to /Account/Login
expected: A logged-in user without the Admin role who navigates to /hangfire is redirected to /Account/Login (not shown the dashboard)
result: issue
reported: "another 403, no redirect"
severity: major

### 4. SmokeTestJob enqueues and completes without exception
expected: On startup, the Hangfire dashboard shows one completed SmokeTestJob run; container logs contain "Smoke test: IEmailService resolved successfully. Type: SmtpEmailService" (or similar); no exceptions in the log
result: pass

### 5. Background Jobs link visible in Admin nav dropdown
expected: An Admin user sees "Background Jobs" as a third item in the Admin dropdown nav; clicking it navigates to /hangfire
result: pass

## Summary

total: 5
passed: 3
issues: 2
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "Unauthenticated users navigating to /hangfire are redirected to /Account/Login with HTTP 302"
  status: failed
  reason: "User reported: getting a 401 error, not a redirect"
  severity: major
  test: 2
  artifacts: []
  missing: []
- truth: "Authenticated non-Admin users navigating to /hangfire are redirected to /Account/Login"
  status: failed
  reason: "User reported: 403, no redirect"
  severity: major
  test: 3
  artifacts: []
  missing: []
