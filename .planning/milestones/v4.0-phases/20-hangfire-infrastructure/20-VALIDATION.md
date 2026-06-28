---
phase: 20
slug: hangfire-infrastructure
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-25
---

# Phase 20 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xunit.v3 3.2.2 |
| **Config file** | none (xunit auto-discover) |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj --no-build` |
| **Full suite command** | `dotnet test --no-build` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj --no-build`
- **After every plan wave:** Run `dotnet test --no-build`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 20-01-01 | 01 | 1 | JOBS-01 | — | N/A | regression | `dotnet test --no-build` | ✅ | ⬜ pending |
| 20-01-02 | 01 | 1 | JOBS-01 | — | N/A | regression | `dotnet test --no-build` | ✅ | ⬜ pending |
| 20-02-01 | 02 | 1 | JOBS-02 | EoP: Admin bypass | Non-admin redirected to login | manual | Code review of Program.cs registration order | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test files required for Phase 20 per D-11.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Hangfire dashboard accessible to Admin at `/hangfire` | JOBS-02 | D-11: Phase 20 adds no integration tests for dashboard; success criterion is code review | Review Program.cs: UseHangfireDashboard appears after UseAuthentication + UseAuthorization; AdminDashboardAuthFilter is the sole entry in DashboardOptions.Authorization |
| Non-admin / unauthenticated user redirected to login | JOBS-02 | D-11 | Review AdminDashboardAuthFilter.Authorize: returns false + Response.Redirect("/Account/Login") for non-Admin |
| Smoke-test job enqueues and completes | JOBS-01 | Hangfire worker required (no test DB), startup one-shot | Start app in dev, check Hangfire dashboard → Jobs → Succeeded for SmokeTestJob |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
