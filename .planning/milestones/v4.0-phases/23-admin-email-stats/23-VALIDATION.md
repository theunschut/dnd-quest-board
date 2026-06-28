---
phase: 23
slug: admin-email-stats
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-26
---

# Phase 23 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (EuphoriaInn.UnitTests) + Microsoft.AspNetCore.Mvc.Testing (EuphoriaInn.IntegrationTests) |
| **Config file** | none — uses `<Using Include="Xunit" />` in .csproj + GlobalUsings.cs |
| **Quick run command** | `dotnet test EuphoriaInn.UnitTests --filter "Category=EmailStats" -x` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (confirms no compile errors)
- **After every plan wave:** Run `dotnet test` (full suite green)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 23-01-01 | 01 | 1 | STATS-01 | T-23-01 / — | Missing ResendApiKey renders warning view, not exception | unit | `dotnet test EuphoriaInn.UnitTests --filter "EmailStats"` | ❌ W0 | ⬜ pending |
| 23-01-02 | 01 | 1 | STATS-01 | T-23-02 | API error renders error banner, not exception | unit | `dotnet test EuphoriaInn.UnitTests --filter "EmailStats"` | ❌ W0 | ⬜ pending |
| 23-01-03 | 01 | 1 | STATS-01 | — | last_event aggregation counts correctly per status | unit | `dotnet test EuphoriaInn.UnitTests --filter "EmailStats"` | ❌ W0 | ⬜ pending |
| 23-01-04 | 01 | 1 | STATS-01 | — | force=true clears cache before fetch | unit | `dotnet test EuphoriaInn.UnitTests --filter "EmailStats"` | ❌ W0 | ⬜ pending |
| 23-02-01 | 02 | 1 | STATS-01 | T-23-01 | Non-admin redirected/forbidden from /Admin/EmailStats | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "AdminController"` | ⬜ partial | ⬜ pending |
| 23-02-02 | 02 | 1 | STATS-01 | T-23-01 | Unauthenticated user redirected from /Admin/EmailStats | integration | `dotnet test EuphoriaInn.IntegrationTests --filter "AdminController"` | ⬜ partial | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.UnitTests/Controllers/AdminEmailStatsTests.cs` — stubs for STATS-01 (missing key, API error, aggregation, cache clear)
- [ ] `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — add `GET /Admin/EmailStats` auth test cases

*Note: AdminController is testable via mocked IHttpClientFactory or by extracting aggregation logic to an internal helper.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Live Resend API returns real stats | STATS-01 | Requires real `ResendApiKey` with data; can't mock live API in CI | 1. Set ResendApiKey in user secrets. 2. Navigate to /Admin/EmailStats. 3. Verify 4 stat cards show non-zero values matching Resend dashboard. |
| Refresh button clears stale cache | STATS-01 | Cache invalidation timing is hard to automate | 1. Load page. 2. Note AsOf time. 3. Click Refresh. 4. Verify new API call is made (AsOf updates). |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
