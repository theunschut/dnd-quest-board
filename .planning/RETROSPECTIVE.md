# Retrospective — D&D Quest Board

## Milestone: v4.0 — Email Notifications

**Shipped:** 2026-06-28
**Phases:** 6 (20–25) | **Plans:** 22

### What Was Built

1. Hangfire background job infrastructure — SQL Server storage, admin-only dashboard, `IServiceScopeFactory` scope pattern
2. HTML email templates for all notifications — `_EmailLayout`, `QuestFinalized`, `QuestDateChanged`, `SessionReminder`, `ConfirmEmail` Razor components rendered via `HtmlRenderer`
3. Quest-finalization dedup guard — `FinalizedEmailSentForDate` EF column prevents re-send on re-finalization
4. 24h automated session reminders — Hangfire daily CRON at 09:00 + DM manual trigger + `ReminderLog` idempotency table
5. Admin email stats dashboard — Resend REST API pagination, 5-min cache, graceful degraded states
6. Email confirmation flow — admin resend button, `EmailConfirmed` guard on all four email jobs, ASP.NET Identity token callback

### What Worked

- **Wave-based planning:** Independent phases (23 vs 21–22) ran in any order after Phase 20 landed — scheduling flexibility reduced blocking time
- **NullObject dispatchers:** The `NullQuestEmailDispatcher` / `NullReminderJobDispatcher` pattern isolated Hangfire from the test host cleanly — no Moq/NSubstitute complexity, no test factory pollution
- **Auto-fix deviations:** The executor caught and fixed bugs inline (UseHangfireDashboard Testing guard, BeOneOf FluentAssertions v8 syntax, NSubstitute extension method assertion) without derailing the plan
- **Scope discipline:** Dropping EMAIL-04/REMIND-02 (digest batching) early based on real usage data ("never happened in a year") prevented building unused complexity
- **Code review after Phase 24:** The post-implementation review caught three real security issues (XSS in email body, token probing via userId ≤ 0, auto-sign-in of unconfirmed users) that the plan's threat model missed

### What Was Inefficient

- **Test factory gap (IBackgroundJobClient):** Phase 21 introduced `HangfireQuestEmailDispatcher` without updating `WebApplicationFactoryBase`. This caused 4 mobile integration test failures that were noted as "out of scope" in Phase 23's SUMMARY and finally fixed only in a separate commit on 2026-06-28. The gap was visible for 3+ days before it was addressed.
- **Phase 22 progress table:** The ROADMAP progress table showed Phase 22 as "1/5 In progress" even after all plans completed — a stale metadata artifact that persisted until milestone close
- **AdminController constructor coupling:** Taking `IBackgroundJobClient` directly in `AdminController`'s primary constructor (instead of via the `IReminderJobDispatcher` abstraction) leaked the Hangfire dependency into tests unnecessarily. The `NoOpBackgroundJobClient` stub in the test factory is a workaround for a design that could be improved by routing through the abstraction.

### Patterns Established

- `IServiceScopeFactory` + `CreateAsyncScope()` inside every Hangfire job — mandatory pattern, established in Phase 20 SmokeTestJob
- NullObject dispatchers for infrastructure not available in Testing env — `NullQuestEmailDispatcher`, `NullReminderJobDispatcher`; ready template for future jobs
- `HtmlRenderer` for email rendering — explicitly chose over `IRazorViewEngine` (throws in background context); documented for all future email templates
- Razor email component structure: `_EmailLayout` wrapping, `@Parameter` for data binding, `IEmailRenderService.RenderAsync<T>()` call site

### Key Lessons

1. **Seal Testing environment gaps at the plan level, not after:** When a new controller or service depends on infrastructure guarded by `!IsEnvironment("Testing")`, add the test stub in the same plan — don't leave it as "noted, out of scope"
2. **`IBackgroundJobClient` should stay behind an abstraction:** Controllers that only enqueue one specific job type (like `ConfirmationEmailJob`) should take an `IReminderJobDispatcher`-style interface, not `IBackgroundJobClient` directly — keeps the test factory clean
3. **Resend API `last_event` semantics are non-obvious:** `opened` and `clicked` count as delivered (not separate from delivered). ResendStatsAggregator needed a comment; future maintainers will hit this
4. **Code review as a separate phase step pays off:** The post-Phase-24 review found 3 real security issues the plan's threat model missed. Running a dedicated review after the highest-risk phase (auth token handling) was worth the extra step

### Cost Observations

- Sessions: multiple across 4 days
- Notable: HtmlRenderer vs IRazorViewEngine discovery avoided a hard-to-debug runtime failure in production; catching it in planning saved significant debugging time
