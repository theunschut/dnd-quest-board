---
phase: 21-html-email-templates
reviewed: 2026-06-26T00:00:00Z
depth: standard
files_reviewed: 25
files_reviewed_list:
  - EuphoriaInn.Domain/EuphoriaInn.Domain.csproj
  - EuphoriaInn.Domain/Interfaces/IEmailRenderService.cs
  - EuphoriaInn.Domain/Interfaces/IEmailService.cs
  - EuphoriaInn.Domain/Interfaces/IQuestEmailDispatcher.cs
  - EuphoriaInn.Domain/Interfaces/IQuestRepository.cs
  - EuphoriaInn.Domain/Models/QuestBoard/Quest.cs
  - EuphoriaInn.Domain/Services/EmailService.cs
  - EuphoriaInn.Domain/Services/QuestService.cs
  - EuphoriaInn.Repository/Entities/QuestEntity.cs
  - EuphoriaInn.Repository/Migrations/20260626105734_AddFinalizedEmailSentForDate.Designer.cs
  - EuphoriaInn.Repository/Migrations/20260626105734_AddFinalizedEmailSentForDate.cs
  - EuphoriaInn.Repository/Migrations/QuestBoardContextModelSnapshot.cs
  - EuphoriaInn.Repository/QuestRepository.cs
  - EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor
  - EuphoriaInn.Service/Components/Emails/QuestFinalized.razor
  - EuphoriaInn.Service/Components/Emails/SessionReminder.razor
  - EuphoriaInn.Service/Components/Emails/_EmailLayout.razor
  - EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs
  - EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs
  - EuphoriaInn.Service/Program.cs
  - EuphoriaInn.Service/Services/HangfireQuestEmailDispatcher.cs
  - EuphoriaInn.Service/Services/NullQuestEmailDispatcher.cs
  - EuphoriaInn.Service/Services/RazorEmailRenderService.cs
  - EuphoriaInn.UnitTests/Services/EmailServiceTests.cs
  - EuphoriaInn.UnitTests/Services/QuestServiceTests.cs
findings:
  critical: 0
  warning: 4
  info: 3
  total: 7
status: issues_found
---

# Phase 21: Code Review Report

**Reviewed:** 2026-06-26
**Depth:** standard
**Files Reviewed:** 25
**Status:** issues_found

## Summary

This phase introduces HTML email templates via Blazor Server-Side Rendering, a Hangfire job dispatcher pattern, and a dedup guard for finalized emails. The architecture is sound: `IQuestEmailDispatcher` in Domain keeps the service layer decoupled, `HangfireQuestEmailDispatcher` and `NullQuestEmailDispatcher` are correctly placed in Service, and the `RazorEmailRenderService` is a clean implementation of the render abstraction.

Four warnings were found. The most impactful is a partial dedup-guard bypass in `QuestFinalizedEmailJob` — the guard checks `quest` for null before checking the date, but when `quest` is null (e.g., deleted or race condition) it proceeds to send emails instead of bailing out. Two array-length mismatch issues mean `QuestDateChangedEmailJob` could silently skip sending to some recipients with no log entry. The fourth warning is an unnecessary `AutoMapper` license key committed as a plain string in `Program.cs`.

No critical (security or data-loss) issues were found.

## Warnings

### WR-01: Null-quest path in finalized-email dedup guard skips emails but logs nothing

**File:** `EuphoriaInn.Service/Jobs/QuestFinalizedEmailJob.cs:32-39`
**Issue:** The dedup check at line 33 reads:
```csharp
if (quest?.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
```
When `quest` is `null`, both sides of `==` resolve differently: `null?.FinalizedEmailSentForDate?.Date` is `null` (a `DateTime?`), while `finalizedDate.Date` is a non-nullable `DateTime`. In C# this comparison is `null == DateTime` which is `false`, so the guard is **not triggered** and execution continues into the send loop. The loop then iterates over `recipientEmails` (which are non-null, passed in from the job payload) and sends all emails even though the quest no longer exists. There is no log entry emitted for the null-quest case.

The intent recorded in the comment (`D-13`) is to deduplicate emails per date; the secondary intent of "bail if quest missing" is present in the `QuestService.FinalizeQuestAsync` pattern but not here.

**Fix:**
```csharp
var quest = await questRepository.GetQuestWithDetailsAsync(questId, cancellationToken);

// Bail if quest has been deleted since the job was enqueued
if (quest == null)
{
    logger.LogWarning(
        "Quest {QuestId} not found when processing finalized email job. Skipping.",
        questId);
    return;
}

// Dedup guard (D-13)
if (quest.FinalizedEmailSentForDate?.Date == finalizedDate.Date)
{
    logger.LogInformation(
        "Finalized email already sent for quest {QuestId} on {Date}. Skipping.",
        questId, finalizedDate);
    return;
}
```

---

### WR-02: `playerNames` array is accepted but never used in `QuestDateChangedEmailJob`

**File:** `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs:17`
**Issue:** `playerNames` is declared as a parameter and passed through the dispatcher chain all the way from `QuestService`, but the job loop only uses `recipientEmails[i]`. The `QuestDateChanged` Razor component has no `PlayerName` parameter, so each recipient receives a generic email with no personalisation. This is not a crash, but it means:
1. The array is serialised into the Hangfire job payload for no effect.
2. If the caller ever passes arrays of different lengths (e.g., `recipientEmails.Length != playerNames.Length`) the discrepancy goes undetected. In the current call sites `recipientEmails` and `playerNames` are built from the same LINQ projections so they stay in sync, but the silent drop is fragile.

**Fix (short-term):** Add a `PlayerName` parameter to `QuestDateChanged.razor` and thread it through, or remove `playerNames` from the job signature if personalisation is not desired for date-changed emails. At minimum, add a length-guard assertion:
```csharp
if (recipientEmails.Length != playerNames.Length)
{
    logger.LogError(
        "Mismatched recipientEmails ({EmailCount}) and playerNames ({NameCount}) for quest {QuestId}.",
        recipientEmails.Length, playerNames.Length, questId);
    // decide: throw or send to all recipients without names
}
```

---

### WR-03: `AutoMapper` license key committed as a plain string in `Program.cs`

**File:** `EuphoriaInn.Service/Program.cs:120`
**Issue:** The AutoMapper license key (`config.LicenseKey = "eyJhb..."`) is committed verbatim in source. While this is a commercial license key rather than a secret credential, committing it in plain text:
- Exposes the key to anyone with read access to the repository.
- Makes key rotation require a code change and redeploy.

**Fix:** Move it to configuration and read it at startup:
```csharp
// appsettings.json / environment variable:
// "AutoMapper:LicenseKey": "eyJhb..."

config.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
```
Then exclude the key value from committed configuration files (use environment variables or secrets management).

---

### WR-04: `QuestDateChangedEmailJob` renders the same HTML body for every recipient but could silently under-send if arrays are mismatched

**File:** `EuphoriaInn.Service/Jobs/QuestDateChangedEmailJob.cs:35-51`
**Issue:** The loop iterates `for (var i = 0; i < recipientEmails.Length; i++)`, which is correct, but the Razor render dictionary contains no per-recipient data (no `PlayerName`). Each render call produces identical HTML, making the N render calls wasteful. More importantly, the body rendered at index 0 is identical to the body at index N-1; if `playerNames` ever diverges in length from `recipientEmails`, no error is raised and the mismatch is invisible.

This is a lower-severity variant of WR-02 focused on the render-loop efficiency and silent-failure risk. Since `playerNames` is unused in the render, the loop could be simplified to a single render + N sends, which would also eliminate the latent misalignment risk:

```csharp
// Render once — content is identical for all recipients
var html = await renderService.RenderAsync<QuestDateChanged>(new Dictionary<string, object?>
{
    { nameof(QuestDateChanged.QuestTitle), questTitle },
    { nameof(QuestDateChanged.DmName),     dmName },
    { nameof(QuestDateChanged.AppUrl),     emailSettings.AppUrl },
    { nameof(QuestDateChanged.QuestUrl),   questUrl },
    { nameof(QuestDateChanged.OldDate),    oldDate },
    { nameof(QuestDateChanged.NewDate),    newDate }
});

foreach (var email in recipientEmails)
{
    await emailService.SendAsync(email, $"Session date changed: {questTitle}", html);
}
```

## Info

### IN-01: Obsolete legacy methods on `IEmailService` are still exercised by tests

**File:** `EuphoriaInn.UnitTests/Services/EmailServiceTests.cs:26-37`
**Issue:** `SendQuestFinalizedEmailAsync` and `SendQuestDateChangedEmailAsync` are marked `[Obsolete]` in `IEmailService.cs` (lines 10 and 12) but two test methods (`SendQuestFinalizedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing` and `SendQuestDateChangedEmailAsync_WhenUsernameEmpty_ReturnsWithoutThrowing`) continue to exercise them. This suppresses the compiler obsolete warning in the test project and means the dead code paths in `EmailService` are tested but still flagged for removal. Low urgency since the intent is "remove in a future phase."

**Fix:** When the legacy methods are removed from `IEmailService`, delete these two test methods as well. Until then, a `#pragma warning disable CS0618` with an explanatory comment would make the intent explicit.

---

### IN-02: `_EmailLayout.razor` loads Google Fonts via an external HTTP request

**File:** `EuphoriaInn.Service/Components/Emails/_EmailLayout.razor:7`
**Issue:** The layout includes a `<link>` to `https://fonts.googleapis.com/css2?family=Cinzel:wght@400;700&display=swap`. Most email clients either block external stylesheets entirely or do not follow `<link>` tags in email `<head>` sections (Outlook, Gmail web, Apple Mail). In practice the Cinzel font will silently fall back to the system serif for the majority of recipients, making the inline `font-family:'Cinzel',serif` declarations the effective styling. The `<link>` tag adds a network round-trip with no effect.

**Fix:** Remove the `<link>` tag. Cinzel is already declared inline as a string fallback (`font-family:'Cinzel',serif`). If the custom font is important, embed a base64-encoded subset in a `<style>` block, though that inflates email size.

---

### IN-03: `QuestDateChanged.razor` email copy says "The confirmed session date for [title] has changed" but the email is triggered for proposed-date changes, not finalised-date changes

**File:** `EuphoriaInn.Service/Components/Emails/QuestDateChanged.razor:20`
**Issue:** The subtitle reads: `"The confirmed session date for @QuestTitle has changed."` However, `QuestDateChangedEmailJob` is dispatched from `UpdateQuestPropertiesWithNotificationsAsync`, which fires when proposed (candidate) dates are modified — not when the single confirmed/finalized date changes. A player receiving this email may be confused: they see "confirmed session date" but the quest may not yet be finalized.

**Fix:** Update the copy to match the actual trigger:
```
The proposed dates for @QuestTitle have been updated by the Dungeon Master.
```

---

_Reviewed: 2026-06-26_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
