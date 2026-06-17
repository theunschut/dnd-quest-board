---
plan: 260617-w1w
status: complete
date: 2026-06-17
commits:
  - b79c9fe
  - 8cb1831
---

# Quick Task 260617-w1w: Fix #89 — Quest Log shows DM session quests

## What was done

Filtered DM session quests out of the Quest Log by extending the existing `.Where()` predicate in `QuestService.GetCompletedQuestsAsync`. The shared `GetQuestsWithDetailsAsync` repository method was intentionally left untouched so other callers (quest board, calendar, manage) are unaffected.

Additionally hardened `QuestLogController.Details` to return 404 for direct URL access to a DM session quest.

## Changes

| File | Change |
|------|--------|
| `EuphoriaInn.Domain/Services/QuestService.cs` | Added `&& !q.DungeonMasterSession` to `GetCompletedQuestsAsync` Where filter |
| `EuphoriaInn.Service/Controllers/QuestBoard/QuestLogController.cs` | Added 404 guard for DM session on Details action |
| `EuphoriaInn.IntegrationTests/Controllers/QuestLogControllerIntegrationTests.cs` | Two new integration tests: mixed list filters correctly, all-session list returns empty |
| `EuphoriaInn.IntegrationTests/Helpers/TestDataHelper.cs` | Extended `CreateTestQuestAsync` with optional `dungeonMasterSession` parameter |

## Verification

- All new and existing tests pass
- Solution builds with 0 errors
- `GetQuestsWithDetailsAsync` repository method unchanged (only call-site filtered)
