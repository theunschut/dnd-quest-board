# Phase 25: Confirmation Email Razor Template - Context

**Gathered:** 2026-06-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a unit test file `ConfirmationEmailJobTests.cs` for the `ConfirmationEmailJob` that was implemented as part of Phase 24. The core Phase 25 deliverables (`ConfirmEmail.razor` + `ConfirmationEmailJob`) are already complete — this phase's remaining work is proving the job wires `IEmailRenderService` and `IEmailService` correctly.

**Already done (Phase 24 executor implemented ahead):**
- `EuphoriaInn.Service/Components/Emails/ConfirmEmail.razor` — D&D-themed template using `_EmailLayout`, gold button CTA, `UserName`/`CallbackUrl`/`AppUrl` parameters
- `EuphoriaInn.Service/Jobs/ConfirmationEmailJob.cs` — Hangfire job resolving services via `IServiceScopeFactory`, calling `renderService.RenderAsync<ConfirmEmail>(...)` then `emailService.SendAsync`
- `AdminController.SendConfirmationEmail` — already enqueues `ConfirmationEmailJob` via `IBackgroundJobClient`

All three Phase 25 ROADMAP success criteria are satisfied. Only the test file is missing.

</domain>

<decisions>
## Implementation Decisions

### Test Coverage
- **D-01:** Add `EuphoriaInn.UnitTests/Services/ConfirmationEmailJobTests.cs` with 2 happy-path tests.
- **D-02:** Test 1 — `ExecuteAsync_CallsRenderAsync_WithCorrectParameters`: verifies `renderService.RenderAsync<ConfirmEmail>` is called with a dictionary containing `UserName`, `CallbackUrl`, and `AppUrl` matching what was passed in.
- **D-03:** Test 2 — `ExecuteAsync_CallsSendAsync_WithRenderedHtml`: verifies `emailService.SendAsync` is called with `toEmail` and subject `"Confirm your D&D Quest Board account"`. The rendered HTML returned by the mock renderService should be what gets passed to SendAsync.
- **D-04:** No null/empty `callbackUrl` test needed — the AdminController already guards against null users before enqueuing, and `callbackUrl` is built by `Url.Action(...)` which returns a non-null string when called on a valid route.

### Test Scaffolding
- **D-05:** Follow the `SessionReminderJobTests` `IServiceScopeFactory` mocking pattern exactly: `Substitute.For<IServiceProvider>()` → `Substitute.For<IServiceScope>()` → `Substitute.For<IServiceScopeFactory>()` returning `new AsyncServiceScope(scope)`.
- **D-06:** Mock `IOptions<EmailSettings>` with `AppUrl = "https://example.com"` (same as `SessionReminderJobTests`).

### Obsolete Method Removal
- **D-07:** Remove `SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync` from `IEmailService` and `EmailService`. These were marked `[Obsolete]` in Phase 21 with the note "Will be removed in a future phase" — all call sites were migrated to `SendAsync` in Phase 21. No callers remain outside the interface declaration, implementation, and their own unit tests.
- **D-08:** Delete the two corresponding tests in `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` (`SendQuestFinalizedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing` and `SendQuestDateChangedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing`) — they test code that no longer exists.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Job Test Pattern
- `EuphoriaInn.UnitTests/Services/SessionReminderJobTests.cs` — canonical IServiceScopeFactory mocking setup; `new AsyncServiceScope(scope)` pattern; NSubstitute service-provider chain

### The Job Being Tested
- `EuphoriaInn.Service/Jobs/ConfirmationEmailJob.cs` — full implementation; 3 services resolved from scope (`IEmailRenderService`, `IEmailService`, `IOptions<EmailSettings>`)

### Obsolete Method Removal Targets
- `EuphoriaInn.Domain/Interfaces/IEmailService.cs` — remove `SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync` declarations (lines 9–12, both with `[Obsolete]`)
- `EuphoriaInn.Domain/Services/EmailService.cs` — remove corresponding method bodies
- `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs` — remove the 2 tests that cover the deleted methods

### The Razor Component
- `EuphoriaInn.Service/Components/Emails/ConfirmEmail.razor` — parameter names: `UserName`, `CallbackUrl`, `AppUrl` (used as dictionary keys in `RenderAsync`)

### Architecture
- `.planning/codebase/ARCHITECTURE.md` — layer structure; unit tests live in `EuphoriaInn.UnitTests`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SessionReminderJobTests` constructor: copy the `IServiceScopeFactory` → `IServiceScope` → `IServiceProvider` mock chain verbatim; only change the services being registered
- `IEmailRenderService.RenderAsync<T>`: generic, parameterized by component type — mock returns `Task.FromResult("<html></html>")` for simplicity
- `IOptions<EmailSettings>`: mock with `emailOptions.Value.Returns(new EmailSettings { AppUrl = "https://example.com" })`

### Established Patterns
- NSubstitute for all mocks; xUnit + FluentAssertions for assertions
- `new AsyncServiceScope(scope)` wraps `IServiceScope` substitute (struct, not mockable directly)
- `await _renderService.Received(1).RenderAsync<ConfirmEmail>(...)` to assert generic method calls

### Integration Points
- `ConfirmationEmailJob` constructor takes `IServiceScopeFactory` + `ILogger<ConfirmationEmailJob>` (primary constructor syntax)
- `Arg.Is<Dictionary<string, object?>>()` for asserting render parameters

</code_context>

<specifics>
## Specific Ideas

- Test 1 asserting the render params: use `Arg.Is<Dictionary<string, object?>>(d => d[nameof(ConfirmEmail.UserName)].ToString() == "TestUser" && ...)` or assert the mock was called then inspect captured args.
- Test subject line to match exactly: `"Confirm your D&D Quest Board account"` (hardcoded in `ConfirmationEmailJob.ExecuteAsync`).

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 25-confirmation-email-razor-template*
*Context gathered: 2026-06-27*
