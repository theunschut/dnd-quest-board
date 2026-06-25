---
phase: 17-character-player-views
plan: 01
subsystem: testing
tags: [integration-tests, requirements, mobile, csharp, xunit]

# Dependency graph
requires:
  - phase: 16-account-browse
    provides: MobileViewsTests.cs with 27 prior test stubs as the base to extend
provides:
  - CHAR-01 through PLAYER-01 requirement definitions in REQUIREMENTS.md
  - Four new [Fact] integration test stubs in MobileViewsTests.cs (RED by design)
  - Nyquist sampling harness for Plans 02-04 to go GREEN against
affects: [17-02, 17-03, 17-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Phase 17 test stub pattern: CreateAuthenticatedClientWithUserAsync + CreateTestCharacterAsync for character detail/edit tests"
    - "Test seed pattern: char_det17/char_cre17/char_edi17/player_idx17 unique usernames prevent Identity collision"

key-files:
  created: []
  modified:
    - .planning/REQUIREMENTS.md
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs

key-decisions:
  - "Test stubs are RED by design — mobile views do not exist yet; tests go GREEN when Plans 02-04 ship"
  - "Character detail/edit tests seed a CharacterEntity via TestDataHelper.CreateTestCharacterAsync to obtain a valid character.Id for the route"
  - "Players test uses authenticated GET /Players — PlayersController carries [Authorize]"

patterns-established:
  - "Authenticated GET pattern for [Authorize] routes: CreateAuthenticatedClientWithUserAsync + manual HttpRequestMessage with MobileUserAgent header"
  - "Character seed uses default role=1 (Backup) and dndClass=5 (Fighter) — no class parameters needed for test stubs"

requirements-completed: [CHAR-01, CHAR-02, CHAR-03, PLAYER-01]

# Metrics
duration: 3min
completed: 2026-06-25
---

# Phase 17 Plan 01: Character & Player Views — Requirements and Test Stubs Summary

**CHAR-01 through PLAYER-01 added to REQUIREMENTS.md and four RED integration test stubs appended to MobileViewsTests.cs (31 total tests, all compile and are discovered)**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-25T10:20:20Z
- **Completed:** 2026-06-25T10:23:30Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Added Character Views section (CHAR-01, CHAR-02, CHAR-03) and Players Page section (PLAYER-01) to REQUIREMENTS.md with wording aligned to ROADMAP.md Phase 17 success criteria
- Added four traceability rows to the Traceability table (Phase 17 / Pending)
- Appended four `[Fact]` integration test stubs to MobileViewsTests.cs; all compile and are discoverable by xUnit test runner
- All four stubs are RED by design — the `.Mobile.cshtml` views do not exist yet; they will go GREEN when Plans 02-04 ship

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CHAR-01, CHAR-02, CHAR-03, PLAYER-01 to REQUIREMENTS.md** - `91a662d` (docs)
2. **Task 2: Append Phase 17 integration test stubs to MobileViewsTests.cs** - `ae3d423` (test)

## Files Created/Modified

- `.planning/REQUIREMENTS.md` — Added "### Character Views" and "### Players Page" sections plus four traceability rows
- `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — Appended four [Fact] test stubs with separator comments

## Decisions Made

- Character detail and edit tests seed a character via `TestDataHelper.CreateTestCharacterAsync` to obtain a valid `character.Id` for route parameters — consistent with prior phase character-seeding patterns
- Players Index test uses authenticated GET with mobile UA (PlayersController carries [Authorize]) — consistent with MobileGuildMembers pattern
- No modification to existing 27 tests — all prior tests remain unchanged and intact

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

- Transient `MSB3492` MSBuild cache error on `--no-restore` builds: `Could not read existing file "obj\Debug\net10.0\EuphoriaInn.Domain.AssemblyInfoInputs.cache"`. This is a Windows file-locking artifact; the full build (without `--no-restore`) and test discovery (`dotnet test --list-tests`) both confirmed successful compilation with 0 C# errors. 31 tests discovered. Pre-existing environment issue, not caused by this plan's changes.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness

- REQUIREMENTS.md has CHAR-01 through PLAYER-01 defined — Plans 02-04 can reference these IDs in their frontmatter
- MobileViewsTests.cs has 4 RED test stubs — Plans 02-04 implement the mobile views that make these GREEN
- Plan 02: GuildMembers/Details.Mobile.cshtml + character-detail.mobile.css (makes CHAR-01 GREEN)
- Plan 03: GuildMembers/Create.Mobile.cshtml + Edit.Mobile.cshtml + character-form.mobile.css (makes CHAR-02 + CHAR-03 GREEN)
- Plan 04: Players/Index.Mobile.cshtml + players.mobile.css + desktop email removal (makes PLAYER-01 GREEN)

## Known Stubs

None — this plan only adds test stubs and requirement documentation; no UI stubs created.

## Self-Check: PASSED

- `.planning/REQUIREMENTS.md` contains `CHAR-01`: FOUND
- `.planning/REQUIREMENTS.md` contains `CHAR-02`: FOUND
- `.planning/REQUIREMENTS.md` contains `CHAR-03`: FOUND
- `.planning/REQUIREMENTS.md` contains `PLAYER-01`: FOUND
- `.planning/REQUIREMENTS.md` contains `### Character Views`: FOUND
- `.planning/REQUIREMENTS.md` contains `### Players Page`: FOUND
- `.planning/REQUIREMENTS.md` contains `| CHAR-01 | Phase 17 | Pending |`: FOUND
- `.planning/REQUIREMENTS.md` contains `| PLAYER-01 | Phase 17 | Pending |`: FOUND
- `MobileViewsTests.cs` contains `GetMobilePage_CharacterDetails_ReturnsSuccessAndMobileLayout`: FOUND
- `MobileViewsTests.cs` contains `GetMobilePage_CharacterCreate_ReturnsSuccessAndMobileLayout`: FOUND
- `MobileViewsTests.cs` contains `GetMobilePage_CharacterEdit_ReturnsSuccessAndMobileLayout`: FOUND
- `MobileViewsTests.cs` contains `GetMobilePage_PlayersIndex_ReturnsSuccessAndMobileLayout`: FOUND
- `MobileViewsTests.cs` contains `character-detail-card`: FOUND
- `MobileViewsTests.cs` contains `character-form-card`: FOUND
- `MobileViewsTests.cs` contains `players-section-card`: FOUND
- `MobileViewsTests.cs` contains `character-detail.mobile.css`: FOUND
- `MobileViewsTests.cs` contains `character-form.mobile.css`: FOUND
- `MobileViewsTests.cs` contains `players.mobile.css`: FOUND
- Commit `91a662d` (Task 1): FOUND
- Commit `ae3d423` (Task 2): FOUND
- 31 tests discovered in MobileViewsTests: CONFIRMED

---
*Phase: 17-character-player-views*
*Completed: 2026-06-25*
