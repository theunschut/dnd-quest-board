---
phase: 34
slug: codebase-cleanup-and-security-hardening-remove-unused-code-s
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-01
---

# Phase 34 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 (`xunit.v3` 3.2.2, `xunit.runner.visualstudio` 3.1.5), `Microsoft.NET.Test.Sdk` 18.7.0 |
| **Config file** | `QuestBoard.IntegrationTests/xunit.runner.json` (only project with a custom runner config) |
| **Quick run command** | `dotnet test QuestBoard.UnitTests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60 seconds (191+ tests as of Phase 33 close; count grows as Test Coverage Gap items add new tests) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (fast compile-check for comment/dead-code/doc changes); `dotnet test QuestBoard.UnitTests` for logic changes
- **After every plan wave:** Run `dotnet test` (full suite)
- **Before `/gsd-verify-work`:** Full suite must be green, plus a final `dotnet list package --vulnerable --include-transitive` re-run as phase-closing evidence
- **Max feedback latency:** 60 seconds

---

## Per-Task Verification Map

No REQ-IDs are mapped to this phase (cleanup/hardening, not a features phase) and task IDs are not yet assigned — the planner assigns Task ID/Plan/Wave when PLAN.md files are created. Rows below are by deliverable category, derived from `34-RESEARCH.md`'s Validation Architecture and Security Domain sections; the planner should map each to concrete task IDs.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | Dead code removal (`RegisterViewModel`, etc.) | — | N/A | build | `dotnet build` | N/A — compiler is the test | ⬜ pending |
| TBD | TBD | TBD | Comment cleanup (D-06) | — | N/A | build | `dotnet build` | N/A | ⬜ pending |
| TBD | TBD | TBD | XML doc backfill on interfaces (D-07/D-08) | — | N/A | build | `dotnet build` | N/A | ⬜ pending |
| TBD | TBD | TBD | Dependency vulnerability scan (D-09/D-10) | T-34-03 (ASVS V13) | No vulnerable packages found | manual/scripted | `dotnet list package --vulnerable --include-transitive` | N/A — CLI output is the evidence | ⬜ pending |
| TBD | TBD | TBD | Tech Debt: `DateTime.Now` → `UtcNow` in `ShopSeedService` | — | Seed dates use UTC | unit | Existing `ShopSeedServiceTests.cs` if present, else manual verification (existence not confirmed in research pass — check before planning) | ❓ unconfirmed | ⬜ pending |
| TBD | TBD | TBD | Known Bugs: startup email-config validation | — | App throws in Production with missing email config; starts fine in Testing/Development | integration | New test — must not trigger the throw path in `WebApplicationFactoryBase`'s Testing environment | ❌ Wave 0 — net-new test | ⬜ pending |
| TBD | TBD | TBD | Known Bugs: Resend API 429 retry-backoff | — | Retries on 429, succeeds on eventual 2xx | unit | New test mocking `HttpClient` via `IHttpClientFactory` | ❌ Wave 0 — net-new test | ⬜ pending |
| TBD | TBD | TBD | Performance: composite index on `Quests(IsFinalized, FinalizedDate)` | — | Migration applies cleanly; query result set unchanged | integration | Existing `DailyReminderJob`/reminder tests must still pass unmodified | ✅ existing tests cover behavior | ⬜ pending |
| TBD | TBD | TBD | Scaling: `ActiveGroupId` null guard | T-34-01 (ASVS V4) | Throws `InvalidOperationException` only for unexpected-null (not SuperAdmin's intentional null) | unit/integration | New test — must NOT break existing SuperAdmin null-is-valid tests (`TenantIsolationTests.cs`) | ❌ Wave 0 — net-new, high care needed | ⬜ pending |
| TBD | TBD | TBD | Security: `Forbid()` defense-in-depth checks (No Tenant Isolation Enforcement at API Boundary) | T-34-01 (ASVS V4) | Returns `Forbid()` when entity's `GroupId` != `activeGroupId` | integration | New test per affected controller action | ❌ Wave 0 — net-new test | ⬜ pending |
| TBD | TBD | TBD | Security: secrets not logged (Email config) | T-34-02 (ASVS V7) | No secret values (SMTP password, connection strings) appear in log messages or exception traces | manual/code review | Manual review of `logger.LogError`/`LogWarning` call sites in `EmailService.cs`, `AdminController.cs` | N/A | ⬜ pending |
| TBD | TBD | TBD | Test Coverage Gaps: Hangfire retry behavior | — | Job retries on failure, moves to Failed after max retries | unit | New test — `SessionReminderJob_EmailSendFailure_RetriesCorrectly` style | ❌ Wave 0 — explicitly the deliverable | ⬜ pending |
| TBD | TBD | TBD | Test Coverage Gaps: Group Query Filter enforcement | T-34-01 (ASVS V4) | Querying with null/unassigned `ActiveGroupId` returns empty result sets | integration | New test — `QueryFilterTests.GetQuests_NoActiveGroup_ReturnsEmpty` style | ❌ Wave 0 — explicitly the deliverable | ⬜ pending |
| TBD | TBD | TBD | Test Coverage Gaps: Follow-up quest cleanup rollback | — | Orphaned quest deleted on update failure; delete failure doesn't swallow the exception | integration | New test — `QuestController_CreateFollowUp_UpdateFailure_CleansUpOrphan` style | ❌ Wave 0 — explicitly the deliverable | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] New test: startup email-config validation (Production-only throw) — needs care to not trigger in Testing environment
- [ ] New test: Resend API 429 retry-backoff behavior (mock `HttpClient` via `IHttpClientFactory`)
- [ ] New test: `ActiveGroupId` null-guard behavior (must not break existing SuperAdmin-null-is-valid tests)
- [ ] New tests: the 3 explicit Test Coverage Gaps items from `CONCERNS.md` (Hangfire retry, group filter enforcement, follow-up cleanup rollback)
- [ ] Verify: does `QuestBoard.UnitTests/Services/ShopSeedServiceTests.cs` exist? Not confirmed in research — check before planning the `DateTime.Now` fix's test coverage.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Email config secrets never appear in logs | Security: Email Configuration Secrets Potentially Logged | Requires reading log call sites and exception-handling code, not something a compiled test asserts | Grep `EmailService.cs` and `AdminController.cs` for `logger.Log*` calls; confirm no interpolated secret values (SMTP password, connection strings) in the message template |
| Dependency vulnerability scan is clean at phase close | Security: D-09/D-10 | CLI scan output is the evidence itself, not a pass/fail unit test | Run `dotnet list package --vulnerable --include-transitive` across all 5 projects; confirm zero vulnerable packages (or document any findings + fix/defer decision) |
| CSRF `[ValidateAntiForgeryToken]` coverage regression check | Security: No CSRF Token Validation on Some State-Changing Actions | Confirms an existing invariant across all controllers, better done as a targeted manual/code review sweep than a broad automated test in this phase | Grep all `[HttpPost]` actions across controllers; confirm each carries `[ValidateAntiForgeryToken]` (already true per this research pass — treat as a regression check) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
