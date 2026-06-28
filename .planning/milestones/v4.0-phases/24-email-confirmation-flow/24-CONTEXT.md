# Phase 24: Email Confirmation Flow - Context

**Gathered:** 2026-06-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin users can manually trigger a confirmation email for any unconfirmed user. All background email jobs skip users whose `EmailConfirmed` is false. The confirmation callback endpoint calls `UserManager.ConfirmEmailAsync` and redirects to login with a TempData message.

</domain>

<decisions>
## Implementation Decisions

### EmailConfirmed Domain Exposure
- **D-01:** Add `bool EmailConfirmed` to the `User` domain model. Wire through AutoMapper `EntityProfile` (same pattern as `HasKey`). This makes the field available to jobs and ViewModels without layer violations.

### Confirmation Callback Endpoint
- **D-02:** `GET /Account/ConfirmEmail?userId=X&token=Y` lives in `AccountController` — it already owns all identity flows (Login, Register, Profile, ChangePassword).
- **D-03:** On success: redirect to `AccountController.Login` with a TempData success banner ("Email confirmed — you can now log in"). On failure: same redirect with a TempData error message. No dedicated views needed.

### Admin Users Page UI
- **D-04:** "Send Confirmation Email" button appears inline in the existing Actions column for each row where `EmailConfirmed == false`. The button is absent for already-confirmed users. Consistent with existing Promote/Demote/Reset row actions.
- **D-05:** After POSTing to `AdminController.SendConfirmationEmail`, page reloads and a TempData success or error banner appears at the top — same feedback pattern as promotions and password resets.

### Email Job Guard
- **D-06:** Introduce a shared extension method `WhereEmailConfirmed()` on `IEnumerable<User>` in the Domain layer. All four jobs (`QuestFinalizedEmailJob`, `QuestDateChangedEmailJob`, `SessionReminderJob`, `DailyReminderJob`) call this to filter their recipient list before sending.
- **D-07:** The extension method lives in a new class in `EuphoriaInn.Domain` (e.g., `UserExtensions`), alongside the `User` model it operates on. Keeps logic testable in isolation.

### Claude's Discretion
- Email subject/body for the confirmation email in this phase: plain inline HTML string (Phase 25 upgrades it to a Razor component). Claude may choose a reasonable subject line and body.
- Token generation: use `UserManager.GenerateEmailConfirmationTokenAsync` + `WebEncoders.Base64UrlEncode` for URL-safe encoding.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Architecture
- `.planning/codebase/ARCHITECTURE.md` — layer structure, dependency direction; confirms Domain cannot reference Service/Repository

### Conventions
- `.planning/codebase/CONVENTIONS.md` — naming patterns, AutoMapper patterns

### Existing Email Infrastructure
- `EuphoriaInn.Domain/Interfaces/IEmailService.cs` — `SendAsync` signature to use for sending the confirmation email
- `EuphoriaInn.Domain/Interfaces/IIdentityService.cs` — existing identity abstraction; `AdminResetPasswordAsync` pattern shows how admin identity operations are done without UserManager leaking into controllers
- `EuphoriaInn.Repository/IdentityService.cs` — implementation; new `SendEmailConfirmationAsync` and `ConfirmEmailAsync` methods will be added here

### Existing Job Patterns
- `EuphoriaInn.Service/Jobs/SessionReminderJob.cs` — canonical example of IServiceScopeFactory pattern, recipient iteration, and how jobs access User data
- `EuphoriaInn.Service/Jobs/DailyReminderJob.cs` — second example of the same job pattern

### Admin UI Pattern
- `EuphoriaInn.Service/Controllers/Admin/AdminController.cs` — shows TempData feedback pattern and existing row-action POST methods (PromoteToAdmin, ResetPassword)
- `EuphoriaInn.Service/Views/Admin/Users.cshtml` — existing Users page to add the button to

### Account Flow Pattern
- `EuphoriaInn.Service/Controllers/Admin/AccountController.cs` — existing Login/Register/Profile methods; ConfirmEmail callback goes here

### Domain Model
- `EuphoriaInn.Domain/Models/User.cs` — add `bool EmailConfirmed` here
- `EuphoriaInn.Domain/Automapper/EntityProfile.cs` — wire `EmailConfirmed` mapping

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IEmailService.SendAsync`: ready to use for sending the confirmation email — no new send infrastructure needed
- `IIdentityService` + `IdentityService`: the pattern for wrapping `UserManager` operations is established; add `GenerateEmailConfirmationTokenAsync` and `ConfirmEmailAsync` as new methods
- `AdminController.ResetPassword` POST: closest analog to `SendConfirmationEmail` — find user, perform identity operation, set TempData, redirect to Users

### Established Patterns
- Jobs get user data via `IUserService` / `IUserRepository`, returning `User` domain objects — `EmailConfirmed` must be on `User` for jobs to guard on it
- AutoMapper `EntityProfile` maps `UserEntity` → `User`; `HasKey` shows the simple bool property pattern
- All Admin actions return `RedirectToAction(nameof(Users))` with `TempData["Success"]` or `TempData["Error"]`

### Integration Points
- `UserManagementViewModel`: may need `bool EmailConfirmed` to drive button visibility in the view
- Four jobs need the `WhereEmailConfirmed()` filter call added to their recipient-iteration logic

</code_context>

<specifics>
## Specific Ideas

- The confirmation token must be URL-safe: use `WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))` when building the callback URL, and reverse on receipt in `ConfirmEmail`.
- The `SendConfirmationEmail` action in AdminController needs `UserManager` access. Route through `IIdentityService` (same as `ResetPassword` uses it) — do not inject `UserManager` directly into the controller.

</specifics>

<deferred>
## Deferred Ideas

- Styled Razor component for the confirmation email → Phase 25 (already planned)
- Auto-send confirmation email on user registration → out of scope for this phase; not currently wired into Register flow

</deferred>

---

*Phase: 24-email-confirmation-flow*
*Context gathered: 2026-06-26*
