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
result: [pending]

## Summary

total: 8
passed: 0
issues: 0
pending: 8
skipped: 0
blocked: 0

## Gaps
