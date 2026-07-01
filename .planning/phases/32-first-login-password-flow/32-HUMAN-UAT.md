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

### 8. Reverse-proxy IP note (deploy-env only, non-blocking)
expected: In the deployed environment, confirm `ForwardedHeaders` behavior is correct for the rate-limit partition key (client IP, not the reverse proxy's IP).
result: confirmed via static analysis 2026-07-01 (no deploy needed) — `docs/server-setup.md` architecture diagram shows Traefik runs on a separate CT from the App CT, connecting over the internal Proxmox bridge to `:5000`. `Program.cs:82-97` partitions the forgot-password rate limiter by `httpContext.Connection.RemoteIpAddress`, and `UseForwardedHeaders()` is intentionally absent this phase (confirmed by grep — no matches). Since Traefik is not loopback, Kestrel sees every request's RemoteIpAddress as Traefik's internal IP. Effect: in production, ALL visitors share ONE rate-limit bucket (keyed on Traefik's IP) rather than one bucket per real client — worse than "imprecise," since one user's attempts throttle everyone. This was a deliberate scope decision documented in the code comment, not a bug introduced by this phase, but the severity (shared-fate across all users, not just reduced precision) is worth the team's explicit sign-off before relying on this rate limiter in production.

## Summary

total: 8
passed: 0
issues: 1
pending: 7
skipped: 0
blocked: 0

## Gaps
