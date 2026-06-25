---
status: testing
phase: 20-hangfire-infrastructure
source: [20-VERIFICATION.md]
started: 2026-06-25T00:00:00Z
updated: 2026-06-25T00:00:00Z
---

## Current Test

number: 1
name: Admin can access Hangfire dashboard at /hangfire
expected: |
  Authenticated Admin user navigates to /hangfire and sees the Hangfire dashboard UI (job list, queues, servers tab)
awaiting: user response

## Tests

### 1. Admin can access Hangfire dashboard at /hangfire
expected: Authenticated Admin user navigates to /hangfire and receives HTTP 200 with the Hangfire dashboard UI rendered
result: [pending]

### 2. Unauthenticated users redirected to /Account/Login
expected: A request to /hangfire without a session cookie receives HTTP 302 redirect to /Account/Login
result: [pending]

### 3. Non-Admin authenticated users redirected to /Account/Login
expected: A logged-in user without the Admin role who navigates to /hangfire is redirected to /Account/Login (not shown the dashboard)
result: [pending]

### 4. SmokeTestJob enqueues and completes without exception
expected: On startup, the Hangfire dashboard shows one completed SmokeTestJob run; container logs contain "Smoke test: IEmailService resolved successfully. Type: SmtpEmailService" (or similar); no exceptions in the log
result: [pending]

### 5. Background Jobs link visible in Admin nav dropdown
expected: An Admin user sees "Background Jobs" as a third item in the Admin dropdown nav; clicking it navigates to /hangfire
result: [pending]

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0
blocked: 0

## Gaps
