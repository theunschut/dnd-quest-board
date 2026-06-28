# Phase 21: HTML Email Templates - Context

**Gathered:** 2026-06-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement `IEmailRenderService` backed by `HtmlRenderer`, route all outbound emails through Hangfire background jobs, upgrade quest-finalization and date-changed emails to styled HTML, add the deduplication guard (`FinalizedEmailSentForDate`), and deliver `SessionReminder.razor` ready for Phase 22 to consume. The SMTP delivery path (SmtpClient → Postfix → Resend relay) is unchanged.

</domain>

<decisions>
## Implementation Decisions

### Email Visual Style
- **D-01:** All HTML emails use the **D&D quest-card aesthetic**: full-background parchment scroll poster image (`/images/Blanks/Blanks w Shadow/Poster1.png` or `Poster6.png`), Cinzel font for headings, dark brown text (`#1a0f08`), gold accents (`#FFD700` / `#ffc107`), parchment body background (`#F4E4BC`). Matches the quest board exactly — players receive something visually consistent with the app.
- **D-02:** The poster image is used as a **full-background** (`background-image: url(...)`, `background-size: cover`) on the email wrapper. Outlook ignores background images — this is acceptable for the group (17 members, all likely on Gmail/Apple Mail). A parchment-tone fallback `background-color` is applied so the email remains readable on Outlook.
- **D-03:** A **gold CR badge** (top-left, matching the quest card design) is included in finalization and reminder emails, showing the quest's challenge rating.
- **D-04:** A **wax seal image** from `/images/Wax Seals/` is used as a decorative element, consistent with the quest card design.
- **D-05:** Cinzel is loaded from Google Fonts (`https://fonts.googleapis.com/css2?family=Cinzel`). Supported in Gmail, Apple Mail, and iOS Mail. Outlook falls back to `serif` — acceptable.

### Scope — Emails Upgraded in Phase 21
- **D-06:** All three outbound email types are upgraded to HTML in Phase 21:
  1. **Quest finalization** (`SendQuestFinalizedEmailAsync` → `QuestFinalizedJob`) — HTML with dedup guard
  2. **Quest date changed** (`SendQuestDateChangedEmailAsync` → `QuestDateChangedJob`) — HTML
  3. **Session reminder** (`SessionReminder.razor`) — HTML template, ready for Phase 22's job to invoke
- **D-07:** The smoke-test job (`SmokeTestJob.cs`) introduced in Phase 20 is removed in Phase 21 (per D-09 from Phase 20 CONTEXT.md).

### Architecture — Hangfire-First Email Pipeline
- **D-08:** All outbound emails are sent through **Hangfire fire-and-forget jobs**. `QuestService` no longer calls `IEmailService` directly — it enqueues a Hangfire job instead. Jobs are resilient to app restart (Hangfire persists them in SQL Server).
- **D-09:** Every Hangfire email job follows the **Phase 20 `IServiceScopeFactory` pattern**: resolve `IEmailRenderService` and `IEmailService` via `IServiceScopeFactory.CreateAsyncScope()` inside the job method body — never via constructor injection.
- **D-10:** The pipeline inside each job is: **render → check dedup → send**
  1. Call `IEmailRenderService.RenderAsync<TComponent>(parameters)` → get HTML string
  2. Check any dedup guard (e.g. `FinalizedEmailSentForDate`) before sending
  3. Call `IEmailService.SendAsync(toEmail, subject, htmlBody)` → SMTP delivery (unchanged)
- **D-11:** `IEmailService` gains a **generic `SendAsync(string toEmail, string subject, string htmlBody)` method** alongside the existing typed methods. The typed methods (`SendQuestFinalizedEmailAsync`, `SendQuestDateChangedEmailAsync`) are either updated to call `SendAsync` internally or deprecated — Claude's discretion.
- **D-12:** `IEmailRenderService` is defined in **Domain** (interface only). `RazorEmailRenderService` implementation lives in **Service** (the project with Razor/component support). Uses `HtmlRenderer` from `Microsoft.AspNetCore.Components.Web` — never `IRazorViewEngine`.

### Deduplication Guard
- **D-13:** A `FinalizedEmailSentForDate` column (`DateTime?`) is added to the `Quest` entity via **EF Core migration**. Before sending the finalization email, the job checks: if `FinalizedEmailSentForDate == finalizedDate`, skip send. After sending, the job sets `FinalizedEmailSentForDate = finalizedDate`. This prevents duplicate sends if a quest is re-opened and re-finalized for the same date.

### Razor Component Location
- **D-14:** Email Razor components (`.razor` files) live in `EuphoriaInn.Service/Components/Emails/`:
  - `_EmailLayout.razor` — shared layout wrapper (parchment poster background, Cinzel font link, brand colors)
  - `QuestFinalized.razor` — finalization email body
  - `QuestDateChanged.razor` — date-changed email body
  - `SessionReminder.razor` — single-quest reminder (Phase 22 consumes this without modification)

### SessionReminder.razor Data Contract
- **D-15:** `SessionReminder.razor` receives the following parameters — fully defined now so Phase 22 can use it without changes:
  - `string QuestTitle`
  - `string DmName`
  - `DateTime QuestDate`
  - `string QuestDescription` (full text, ~500 words — not truncated)
  - `IList<string> ConfirmedPlayerNames` (character names; fallback to player name when no character assigned)
  - `string QuestUrl` (absolute URL — `AppUrl + /Quest/Details/{id}`)
  - `int ChallengeRating`

### Claude's Discretion
- Exact wax seal image selection (one fixed seal or randomized per questId hash — same logic as the quest card)
- Whether `SendQuestFinalizedEmailAsync` / `SendQuestDateChangedEmailAsync` are removed from `IEmailService` or kept as deprecated wrappers
- Exact CSS inline styles for email HTML (email clients require inline styles; no external stylesheet)
- Worker count and Hangfire job naming conventions for email jobs

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Roadmap & Requirements
- `.planning/ROADMAP.md` §"Phase 21: HTML Email Templates" — goal, requirements (EMAIL-01, EMAIL-02, EMAIL-03), and success criteria
- `.planning/REQUIREMENTS.md` §EMAIL-01, EMAIL-02, EMAIL-03 — full requirement text

### Prior Phase Decisions
- `.planning/phases/20-hangfire-infrastructure/20-CONTEXT.md` — IServiceScopeFactory pattern (D-05, D-06, D-09), smoke-test job removal (D-09), dashboard auth pattern

### Existing Email Code
- `EuphoriaInn.Domain/Interfaces/IEmailService.cs` — current interface; will gain generic `SendAsync`
- `EuphoriaInn.Domain/Services/EmailService.cs` — current SMTP implementation; delivery path is unchanged
- `EuphoriaInn.Domain/Services/QuestService.cs` — `FinalizeQuestAsync` method; currently calls `IEmailService` directly, will enqueue Hangfire jobs instead

### Existing Hangfire Scaffolding
- `EuphoriaInn.Service/Jobs/SmokeTestJob.cs` — IServiceScopeFactory pattern reference (remove this file in Phase 21)
- `EuphoriaInn.Service/Program.cs` — Hangfire registration; new email job registrations go here

### Design Assets
- `EuphoriaInn.Service/wwwroot/images/Blanks/Blanks w Shadow/Poster1.png` — parchment scroll poster (1000×1400)
- `EuphoriaInn.Service/wwwroot/images/Blanks/Blanks w Shadow/Poster6.png` — parchment scroll poster (600×800)
- `EuphoriaInn.Service/wwwroot/images/Wax Seals/` — wax seal images for decorative element
- `EuphoriaInn.Service/wwwroot/css/quests.css` — brand colors and Cinzel font usage to replicate in email inline styles

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SmokeTestJob.cs` — canonical IServiceScopeFactory pattern to copy into email jobs (then delete the smoke-test itself)
- `AdminDashboardAuthFilter.cs` — Admin role check pattern (not directly relevant, but shows project auth conventions)
- `EmailSettings` record (`EuphoriaInn.Domain/Models/EmailSettings.cs`) — has `AppUrl`, `FromEmail`, `FromName`, `SmtpServer/Port/Username/Password` — `AppUrl` is needed for the `QuestUrl` link in emails

### Established Patterns
- Service registration via extension methods in `ServiceExtensions.cs` — `IEmailRenderService` / `RazorEmailRenderService` registration goes here (or a new `AddEmailServices` extension)
- EF Core migration: add `FinalizedEmailSentForDate DateTime?` to `QuestEntity` and `Quest` domain model; run `dotnet ef migrations add AddFinalizedEmailSentForDate --project ../EuphoriaInn.Repository` from `EuphoriaInn.Service/`
- Hangfire job enqueue: `BackgroundJob.Enqueue<TJob>(job => job.ExecuteAsync(param1, param2))` — matching the pattern established in Phase 20

### Integration Points
- `QuestService.FinalizeQuestAsync` — replace direct `emailService.SendQuestFinalizedEmailAsync(...)` call with `BackgroundJob.Enqueue<QuestFinalizedEmailJob>(...)`
- `QuestService.UpdateQuestDatesAsync` (or wherever `SendQuestDateChangedEmailAsync` is called) — same replacement
- `EuphoriaInn.Repository/Entities/QuestEntity.cs` — add `FinalizedEmailSentForDate` property
- `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` — add `FinalizedEmailSentForDate` property
- AutoMapper `EntityProfile.cs` — map `FinalizedEmailSentForDate` if not already covered by convention

</code_context>

<specifics>
## Specific Ideas

- **Visual reference:** User shared the quest card screenshot — emails should match this exactly: portrait parchment scroll as full background, CR badge top-left (gold coin with challenge rating number), Cinzel title centered, italic description text, DM/date/players in bold labels below, wax seal bottom-left corner.
- **Poster choice:** Use `Poster1.png` (1000×1400, tall portrait) as the primary email template background — proportions suit a portrait email layout. `Poster6.png` (600×800) as an alternative for shorter emails (date-changed).
- **Full quest description in reminder:** User confirmed the quest description is ~500 words — include in full without truncation. Players use this to re-read the adventure hook before the session.
- **Character name fallback:** If a confirmed player has no character assigned, show their platform username (player name) instead of an empty slot.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 21-html-email-templates*
*Context gathered: 2026-06-26*
