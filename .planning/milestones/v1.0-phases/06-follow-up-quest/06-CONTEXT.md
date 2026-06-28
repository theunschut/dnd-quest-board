# Phase 6: Follow-Up Quest - Context

**Gathered:** 2026-05-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Add a "Create Follow-Up Quest" button to a finalized quest's Manage page. Clicking it opens a pre-filled creation form. On save, the new quest is linked to the original via a nullable self-referential `OriginalQuestId` FK. Selected players from the original quest are pre-approved on the follow-up. Both quest pages display a link to the other.

This phase does NOT add: multi-step wizard UI, campaign management screens, or any changes to the quest finalization flow.

</domain>

<decisions>
## Implementation Decisions

### Form Pre-Fill

- **D-01:** Full copy — the follow-up creation form pre-fills Title, Description, ChallengeRating, TotalPlayerCount, and DungeonMasterId from the original quest.
- **D-02:** Title is appended with `" - Part 2"` (e.g., `"The Lost Mine - Part 2"`) to help the DM remember to differentiate it. DM can edit freely before saving.
- **D-03:** `ProposedDates` is always cleared — the follow-up form starts with no dates. FOLLOW-03 requires a new date before the quest can be saved. This applies even with full copy; dates are never inherited.
- **D-04:** `DungeonMasterSession` is reset to its standard default (false), not copied.

### Player Pre-Approval

- **D-05:** Only players with `IsSelected=true` on the finalized original quest are imported as pre-approved signups on the follow-up.
- **D-06:** All imported signups are created with `SignupRole.Player` regardless of their role on the original quest. The DM can reassign roles on the Manage page after creation.
- **D-07:** The import happens at the service layer when the follow-up quest is first saved — not lazily on demand.

### Follow-Up Link Display

- **D-08:** The link between original and follow-up quests is surfaced as an inline line inside the existing Quest Summary sidebar card on both `Details.cshtml` and `Manage.cshtml`.
- **D-09:** Wording: "Continues in: [Quest Title]" (on the original) and "Continues from: [Quest Title]" (on the follow-up). Use `fa-scroll` icon. Each is a clickable link to the related quest's Details page.
- **D-10:** The "Create Follow-Up Quest" button appears only on the Manage page (DM-only), not on the public Details page.

### Multiple Follow-Ups (Chaining)

- **D-11:** Each quest may have at most one direct follow-up. The "Create Follow-Up Quest" button is hidden (or disabled) on the Manage page once a follow-up already exists for that quest. Enforced at the application layer.
- **D-12:** Chaining is allowed — a follow-up quest can itself have a follow-up (Part 1 → Part 2 → Part 3). There is no depth limit.
- **D-13:** Follow-up quests that are not yet finalized do NOT need to be finalized before a further follow-up can be created. The button is available as soon as the immediate follow-up exists.

### Schema

- **D-14:** `OriginalQuestId` is a nullable `int?` self-referential FK on `QuestEntity`. Delete behaviour: set null (not cascade delete), so deleting a follow-up does not remove the original.
- **D-15:** The EF Core migration must also add the corresponding navigation property and FK constraint. Auto-applied on startup per project convention.

### Claude's Discretion

- Whether to add a dedicated `CreateFollowUpAsync` service method or extend an existing one.
- Exact HTML/CSS for the sidebar line (follow existing Quest Summary card pattern).
- Whether to add an `IQuestRepository` method for checking follow-up existence or check via the domain model.
- URL route for the follow-up creation action (e.g., `/Quest/CreateFollowUp/{id}`).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Quest Domain
- `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` — Quest domain model; OriginalQuestId and FollowUpQuest navigation will be added here
- `EuphoriaInn.Domain/Models/QuestBoard/PlayerSignup.cs` — PlayerSignup model; IsSelected and SignupRole properties used for pre-approval logic
- `EuphoriaInn.Domain/Enums/SignupRole.cs` — Player/Spectator/AssistantDM enum; imported signups reset to Player
- `EuphoriaInn.Domain/Interfaces/IQuestService.cs` — service contract; new follow-up creation method goes here
- `EuphoriaInn.Domain/Services/QuestService.cs` — service implementation

### Repository
- `EuphoriaInn.Repository/Entities/QuestEntity.cs` — QuestEntity; OriginalQuestId FK and navigation properties added here
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — DbContext; FK relationship configuration
- `EuphoriaInn.Repository/Migrations/` — new migration for nullable OriginalQuestId column

### Controller & ViewModels
- `EuphoriaInn.Service/Controllers/QuestBoard/QuestController.cs` — Manage action and new CreateFollowUp GET/POST actions
- `EuphoriaInn.Service/ViewModels/QuestViewModels/QuestViewModel.cs` — reused for follow-up creation form
- `EuphoriaInn.Service/ViewModels/QuestViewModels/EditQuestViewModel.cs` — pattern reference for DM list injection

### Views
- `EuphoriaInn.Service/Views/Quest/Manage.cshtml` — add "Create Follow-Up Quest" button (shown only when no follow-up exists yet)
- `EuphoriaInn.Service/Views/Quest/Details.cshtml` — add "Continues in/from" line to Quest Summary sidebar
- `EuphoriaInn.Service/Views/Quest/Create.cshtml` — reused or adapted for follow-up creation form

### Requirements
- `.planning/REQUIREMENTS.md` §FOLLOW-01 – FOLLOW-05 — the five acceptance criteria for this phase

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `QuestController.Create` (GET) — existing create action pre-populates a `QuestViewModel`; the follow-up GET action follows the same pattern but reads from the original quest.
- `QuestViewModel` — already has Title, Description, ChallengeRating, DungeonMasterId, TotalPlayerCount, DungeonMasterSession, ProposedDates; reuse directly for the follow-up form. ProposedDates initialises to `[DateTime.Today.AddDays(1).AddHours(18)]` — override to empty list for follow-ups.
- `QuestController.Manage` — existing DM auth check pattern (`isQuestDm || isAdmin`) reused to guard the follow-up button.
- `PlayerSignup` create pattern in `QuestController.Details` (POST) — existing signup creation flow to reference when bulk-importing pre-approved players.

### Established Patterns
- Self-referential FKs: not currently in the codebase, but `CharacterImageEntity` (1-to-1 image relationship) provides a FK configuration reference in `QuestBoardContext`.
- Sidebar Quest Summary card: used consistently on both `Details.cshtml` and `Manage.cshtml` — adding a conditional line here requires minimal markup.
- `[Authorize(Policy = "DungeonMasterOnly")]` — applied to all DM actions; apply to `CreateFollowUp` as well.

### Integration Points
- `QuestEntity` ← new nullable `OriginalQuestId` FK + `OriginalQuest` / `FollowUpQuest` navigation properties
- `Quest` domain model ← matching nullable `OriginalQuestId`, `OriginalQuest`, `FollowUpQuest` navigation
- `EntityProfile` (Repository) ← map new navigation properties
- `QuestService` ← new `CreateFollowUpQuestAsync(int originalQuestId)` method
- `QuestController` ← new `CreateFollowUp` GET and POST actions
- `Manage.cshtml` ← conditional "Create Follow-Up Quest" button
- `Details.cshtml` and `Manage.cshtml` sidebar ← conditional "Continues in/from" line

</code_context>

<specifics>
## Specific Ideas

- Title appended with `" - Part 2"` literal string on pre-fill (not inferred from a counter).
- `ProposedDates` cleared even in full-copy mode — explicitly noted because it was flagged as potentially unclear during discussion.
- "Continues in: [Title]" / "Continues from: [Title]" as the exact sidebar label wording.
- `fa-scroll` FontAwesome icon for the quest chain relationship indicator.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 06-follow-up-quest*
*Context gathered: 2026-05-04*
