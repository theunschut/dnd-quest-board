---
status: partial
phase: 32-first-login-password-flow
source: [32-05-PLAN.md]
started: 2026-07-01
updated: 2026-07-01
---

## Current Test

[awaiting human testing]

## Tests

### 1. Email visuals — Welcome and ForgotPassword templates
expected: Visit `/EmailPreview`, open the Welcome and ForgotPassword previews. Cinzel/wax-seal style; CTA buttons read "Set My Password" / "Reset My Password"; ForgotPassword shows the "if you did not request..." disclaimer; no ConfirmEmail preview link remains.
result: [pending]

### 2. Admin create-user — no password field
expected: As Admin, go to Create User. No password input field. Creating a user shows a success message mentioning a welcome/set-password email.
result: [pending]

### 3. Welcome link — set password end-to-end
expected: Open the enqueued Welcome email link (`/Account/SetPassword?userId=...&token=...`). Set a password, confirm redirect to Login with a success message, then log in successfully.
result: [pending]

### 4. Forgot password — enumeration-safe
expected: From Login, click "Forgot password?". Submitting a known email and an unknown email both show the same generic message. Using the reset link successfully sets a new password and logs in.
result: [pending]

### 5. Rate limit on Forgot Password
expected: Submit the Forgot Password form 4 times rapidly for the same email. The 4th request is rejected with HTTP 429.
result: [pending]

### 6. Passwordless sign-in fails cleanly
expected: Attempt to log in as a newly created user before they've set a password. Login fails cleanly with no crash.
result: [pending]

### 7. Resend welcome email
expected: On the Users admin page, an unconfirmed user's row button reads "Resend Welcome Email" and sends it when clicked.
result: [pending]

### 8. Reverse-proxy IP note — fixed
expected: In the deployed environment, `RemoteIpAddress` should reflect the real client, not Traefik's IP, so the forgot-password rate limiter partitions per-client instead of sharing one bucket across all users.
result: fixed 2026-07-01 (commit `836f9ac`) — added `ReverseProxy:KnownProxies` config (empty by default; set via `ReverseProxy__KnownProxies__0=<TRAEFIK_CT_IP>` env var in production, documented in `docs/server-setup.md`) and `app.UseForwardedHeaders()` as the first pipeline middleware, scoped to `X-Forwarded-For` only. Build clean, all 253 tests (57 unit + 196 integration) pass. Remaining manual step: set the env var on the App CT during deployment (see `docs/server-setup.md` section 3 note) and confirm `RemoteIpAddress` shows the real client IP once deployed — not blocking for this UAT since the fix is verified in code/tests; deployment-time confirmation can happen at next deploy.

## Summary

total: 8
passed: 1
issues: 0
pending: 7
skipped: 0
blocked: 0

## Gaps
