---
phase: 24
slug: email-confirmation-flow
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-26
---

# Phase 24 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit.v3 3.2.2 + NSubstitute 5.3.0 + FluentAssertions 8.10.0 |
| **Config file** | `EuphoriaInn.UnitTests/EuphoriaInn.UnitTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~UserExtensions|FullyQualifiedName~EmailConfirmation" -x` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.UnitTests -x`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 24-01-01 | 01 | 1 | REQ-24-01 | — | EmailConfirmed maps by AutoMapper convention | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~UserExtensions" -x` | ❌ W0 | ⬜ pending |
| 24-01-02 | 01 | 1 | REQ-24-04 | — | WhereEmailConfirmed returns only confirmed users | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~UserExtensionsTests" -x` | ❌ W0 | ⬜ pending |
| 24-02-01 | 02 | 2 | REQ-24-02 | T-24-01 | SendConfirmationEmail calls identity + email service | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~EmailConfirmation" -x` | ❌ W0 | ⬜ pending |
| 24-03-01 | 03 | 2 | REQ-24-03 | T-24-02 | ConfirmEmail decodes token and calls ConfirmEmailAsync | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~EmailConfirmation" -x` | ❌ W0 | ⬜ pending |
| 24-04-01 | 04 | 3 | REQ-24-04 | — | SessionReminderJob skips unconfirmed recipients | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~SessionReminderJobTests" -x` | ✅ | ⬜ pending |
| 24-04-02 | 04 | 3 | REQ-24-04 | — | QuestFinalizedEmailJob call site guards on EmailConfirmed | unit | `dotnet test EuphoriaInn.UnitTests --filter "FullyQualifiedName~EmailConfirmationJobGuard" -x` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.UnitTests/Services/UserExtensionsTests.cs` — stubs covering REQ-24-04 (`WhereEmailConfirmed()` logic)
- [ ] `EuphoriaInn.UnitTests/Services/EmailConfirmationJobGuardTests.cs` — stubs covering job guard for `QuestFinalizedEmailJob` / `QuestDateChangedEmailJob`

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Admin/Users button absent for confirmed users | REQ-24-01 | View-level conditional rendering; not covered by unit tests | Log in as Admin, navigate to /Admin/Users, verify button absent on rows with confirmed email |
| Clicking button sends actual email | REQ-24-02 | Requires running email stack | Click "Send Confirmation Email" for an unconfirmed user; verify TempData success banner appears |
| Clicking confirmation link in email | REQ-24-03 | End-to-end email → browser flow | Open received email, click link, verify redirect to Login with "Email confirmed" banner |
| TempData banners on Login.cshtml | REQ-24-03 | View-level rendering | Verify Success/Error banners appear on Login page after ConfirmEmail redirect |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
