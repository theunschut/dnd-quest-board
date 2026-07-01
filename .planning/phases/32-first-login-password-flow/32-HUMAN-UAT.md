---
status: partial
phase: 32-first-login-password-flow
source: [32-05-PLAN.md]
started: 2026-07-01
updated: 2026-07-01
---

## Current Test

Completed — 7/8 items verified during Wave 3 checkpoint (item 5 deferred, see Gaps).

## Tests

### 1. Email visuals — Welcome and ForgotPassword templates
expected: Visit `/EmailPreview`, open the Welcome and ForgotPassword previews. Cinzel/wax-seal style; CTA buttons read "Set My Password" / "Reset My Password"; ForgotPassword shows the "if you did not request..." disclaimer; no ConfirmEmail preview link remains.
result: pass

### 2. Admin create-user — no password field
expected: As Admin, go to Create User. No password input field. Creating a user shows a success message mentioning a welcome/set-password email.
result: pass

### 3. Welcome link — set password end-to-end
expected: Open the enqueued Welcome email link (`/Account/SetPassword?userId=...&token=...`). Set a password, confirm redirect to Login with a success message, then log in successfully.
result: pass

### 4. Forgot password — enumeration-safe
expected: From Login, click "Forgot password?". Submitting a known email and an unknown email both show the same generic message. Using the reset link successfully sets a new password and logs in.
result: pass

### 5. Rate limit on Forgot Password
expected: Submit the Forgot Password form 4 times rapidly for the same email. The 4th request is rejected with HTTP 429.
result: out of scope because will be fixed the next phase.

### 6. Passwordless sign-in fails cleanly
expected: Attempt to log in as a newly created user before they've set a password. Login fails cleanly with no crash.
result: pass

### 7. Resend welcome email
expected: On the Users admin page, an unconfirmed user's row button reads "Resend Welcome Email" and sends it when clicked.
result: pass — copy issue found and fixed 2026-07-01 (commit `3386b8c`). Confirmed: legacy pre-Phase-32 accounts can have a password already set (old admin-set-password flow) but EmailConfirmed still false, so the repurposed button (D-07/D-14) sent them the "a guild registrar has opened an account in your name" copy, which is inaccurate for them. Fixed by adding `HasPasswordAsync` through IIdentityService/IUserService and a new `Welcome.razor` `IsNewAccount` parameter — legacy re-confirms now get "your account needs one more step" copy instead. New accounts (CreateUser) unaffected, always pass `isNewAccount: true`. Build clean, 254 tests pass (58 unit incl. new coverage + 196 integration).

### 8. Reverse-proxy IP note — fixed
expected: In the deployed environment, `RemoteIpAddress` should reflect the real client, not Traefik's IP, so the forgot-password rate limiter partitions per-client instead of sharing one bucket across all users.
result: fixed 2026-07-01 (commit `836f9ac`) — added `ReverseProxy:KnownProxies` config (empty by default; set via `ReverseProxy__KnownProxies__0=<TRAEFIK_CT_IP>` env var in production, documented in `docs/server-setup.md`) and `app.UseForwardedHeaders()` as the first pipeline middleware, scoped to `X-Forwarded-For` only. Build clean, all 253 tests (57 unit + 196 integration) pass. Remaining manual step: set the env var on the App CT during deployment (see `docs/server-setup.md` section 3 note) and confirm `RemoteIpAddress` shows the real client IP once deployed — not blocking for this UAT since the fix is verified in code/tests; deployment-time confirmation can happen at next deploy.

## Summary

total: 8
passed: 7
issues: 0
pending: 0
skipped: 1
blocked: 0

## Gaps

- Item 5 (Rate limit on Forgot Password — trigger the 429 manually) not verified this session; user noted "out of scope because will be fixed the next phase." Flagged for clarification: Phase 33 (session persistence) does not touch the rate limiter — item 8's fix (commit `836f9ac`, this phase) already addresses the reverse-proxy partition-key issue. The underlying 429 behavior is covered by automated integration tests (`AccountControllerIntegrationTests`); a manual click-through was not required to close this phase, but confirm this wasn't conflated with item 8 before treating it as resolved.
