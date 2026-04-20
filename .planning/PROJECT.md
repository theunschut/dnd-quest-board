# D&D Quest Board — Milestone 2: Refactor + Feature Expansion

## What This Is

A D&D campaign management web application for a group of players and Dungeon Masters. It handles quest creation and scheduling, player signup with date voting, a character/guild system, a shop with gold economy, and email notifications. Built with ASP.NET Core 8 MVC, SQL Server, and Docker — deployed as a single container to a self-hosted environment.

## Core Value

The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## Requirements

### Validated

- ✓ Quest creation with proposed dates and difficulty selection — existing
- ✓ Player signup with Yes/No/Maybe date voting — existing
- ✓ DM quest finalization with player selection and email notification — existing
- ✓ User authentication and registration (ASP.NET Core Identity) — existing
- ✓ Role-based access control (Admin, DungeonMaster, Player) — existing
- ✓ Character creation and guild member directory — existing
- ✓ Shop with gold economy and item transactions — existing
- ✓ Monthly calendar view for quest scheduling — existing
- ✓ Admin panel for user and quest management — existing
- ✓ Docker deployment with SQL Server — existing

### Active

#### Architecture Refactor
- [x] Domain layer must not depend directly on Repository entities — fix dependency direction — Validated in Phase 01: layer-dependency-fix
- [ ] Business logic (email sending, finalize logic, shop transactions) must live in services, not controllers
- [ ] Controllers reduced to: validate input → call service → return view/redirect

#### Code Quality & Dead Code — Validated in Phase 03: code-quality-dead-code
- [x] Remove `SecurityConfiguration` class and its unused `appsettings.json` section
- [x] Remove dead `UpdateQuestPropertiesAsync` (non-notification variant) from interface and service
- [x] Replace `SignupRole == 1` magic number with named enum reference throughout
- [x] Extract 30-minute `IsSameDateTime` window as a named constant with comment
- [x] Rename `CharacterViewModels/GuildMembersIndexViewModel.cs` to match its actual class name

#### Security
- [ ] Enable account lockout on login (`lockoutOnFailure: true`, 5 attempts, 15-min lock)
- [ ] Increase minimum password length to 8 characters
- [ ] Remove `HasKey` from user-facing profile edit — make it admin-only
- [ ] Remove `Password` property from `User` domain model
- [ ] Add `.env` to `.gitignore`; keep only `.env.example` tracked

#### New Features
- [ ] DM profile page (issue #98) — photo, name, bio so players can learn each DM's style
- [ ] Shop filter and sort by price/rarity (issue #96)
- [ ] Profile picture crop/avatar selection for guild member page (issue #78)
- [ ] Follow-up quest creation (issue #49) — creates part 2 with existing players pre-filled, new date required

### Out of Scope

- Bug fixes (#94 add dates, #91 file size 413, #89 DM sessions in quest log) — separate bug-fix milestone
- D&D Beyond PDF character sheet parser (#84) — large standalone feature, future milestone
- 5etools integration (#82) — large standalone feature, future milestone
- Miniature request page (#59) — large standalone feature, future milestone
- Email verification on registration — deferred; small group, trust is assumed
- Pagination on list views — deferred; group is small enough that unbounded lists are fine now
- Image blob storage migration — deferred; performance acceptable at current scale

## Context

The codebase was built iteratively with AI assistance without upfront planning. It is functional but has accumulated architectural drift:

- **Layer boundary violation:** `EuphoriaInn.Domain` services directly reference `EuphoriaInn.Repository` entity types and depend on the repository layer for EF-specific constructs. The intended direction is Domain ← Repository (Domain defines interfaces, Repository implements them) but in practice Domain knows too much about Repository internals.
- **Controller bloat:** Quest finalization, email dispatch, shop transactions, and other multi-step operations are partially or fully implemented inside controller actions rather than delegated to services.
- **30 documented concerns** catalogued in `.planning/codebase/CONCERNS.md` — this milestone addresses the architecture, code quality, and security subsets.

The codebase map is current (analysed 2026-04-15): `.planning/codebase/`.

## Constraints

- **Compatibility:** No user-facing functionality may be removed or broken — all existing flows must work after the refactor
- **Tech stack:** Stay on ASP.NET Core 8 MVC + SQL Server + EF Core — no framework changes
- **Deployment:** Must remain deployable via `docker-compose up` with no additional setup steps
- **Database:** All schema changes require EF Core migrations; auto-applied on startup

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Refactor + new features in same milestone | Avoids two sequential code-freeze windows; features land on clean architecture | — Pending |
| Bugs deferred to separate milestone | Bugs are isolated fixes; refactor may touch same code and create conflicts | — Pending |
| No pagination this milestone | Group size makes it a non-issue; adds complexity to every list view | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-16 after Phase 01: layer-dependency-fix*
