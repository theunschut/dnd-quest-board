# D&D Quest Board

## What This Is

A D&D campaign management web application for a group of players and Dungeon Masters. It handles quest creation and scheduling, player signup with date voting, a character/guild system, a shop with gold economy, and email notifications. Built with ASP.NET Core 8 MVC, SQL Server, and Docker — deployed as a single container to a self-hosted environment.

## Core Value

The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

## Current Milestone: v2.0 Omphalos Integration

**Goal:** DMs can open Omphalos session notes for any quest with one click — navigated automatically into the correct session, authenticated via a short-lived signed token.

**Target features:**
- Admin Settings page (key-value store) for Omphalos URL and shared secret
- "Open DM Tool" link in DM navbar dropdown and "Open Session Notes" button on quest pages
- Omphalos: SSO endpoint that validates Quest Board tokens and auto-provisions users + quest sessions
- Foundation designed for bidirectional API calls (Omphalos → Quest Board) in a future milestone

## Requirements

### Validated

*From before Milestone 2:*
- ✓ Quest creation with proposed dates and difficulty selection
- ✓ Player signup with Yes/No/Maybe date voting
- ✓ DM quest finalization with player selection and email notification
- ✓ User authentication and registration (ASP.NET Core Identity)
- ✓ Role-based access control (Admin, DungeonMaster, Player)
- ✓ Character creation and guild member directory
- ✓ Shop with gold economy and item transactions
- ✓ Monthly calendar view for quest scheduling
- ✓ Admin panel for user and quest management
- ✓ Docker deployment with SQL Server

*From Milestone 2: Refactor + Feature Expansion (v1.0):*
- ✓ Domain layer compiles without referencing Repository; correct dependency direction enforced at build time — Phase 1
- ✓ Quest finalization and email dispatch fully inside services; controllers receive ServiceResult — Phase 2
- ✓ Typed EmailSettings options; AppUrl config; no SMTP duplication — Phase 2
- ✓ Dead code removed; magic numbers replaced with named enums; naming consistent — Phase 3
- ✓ Account lockout (5 attempts/15 min); password minimum 8 characters; .env gitignored — Phase 4
- ✓ Shop filter and sort by rarity/price; URL-persistent state — Phase 5
- ✓ Follow-up quest creation from a finalized quest; original players pre-approved — Phase 6
- ✓ DM profile page with photo and bio; DMs edit own profile; admin edits any — Phase 7
- ✓ Shop server-side pagination (12 items/page) with stacked search, filter, sort — Phase 9

*From Milestone 3: Omphalos Integration (v2.0 — Quest Board side complete):*
- ✓ Admin Settings page for Omphalos URL and shared secret; protected by AdminOnly policy — Phase 10
- ✓ OmphalosNavItem ViewComponent in DM navbar dropdown; "Open Omphalos" link (new tab) — Phase 11
- ✓ "Open Session Notes" button on Quest Detail and Manage pages for DMs — Phase 11
- ✓ LaunchOmphalos endpoint generates 300s HMAC-SHA256 signed redirect URL — Phase 11

### Active

*(Requirements for Milestone 3 — Phase 12 Omphalos work remaining)*

- [ ] Omphalos validates the Quest Board token, auto-provisions DM account on first use, finds/creates quest session, issues JWT cookie — Phase 12 (Omphalos repo)

### Out of Scope

- D&D Beyond PDF character sheet parser (#84) — large standalone feature, future milestone
- 5etools integration (#82) — large standalone feature, future milestone
- Miniature request page (#59) — large standalone feature, future milestone
- Email verification on registration — deferred; small group, trust is assumed
- Image blob storage migration — deferred; performance acceptable at current scale
- Profile picture avatar crop (Phase 8) — deferred from Milestone 2; no strong user demand, revisit later
- Omphalos → Quest Board API calls — bidirectional foundation laid in Milestone 3; full reverse integration in a future milestone
- True SSO / OAuth OIDC between apps — overkill for self-hosted group app; HMAC shared-secret bridge is sufficient

## Context

The codebase completed a major refactor (Milestone 2) restoring clean architecture: Domain layer independent from Repository, business logic in services, typed configuration, security hardened. Architecture concerns from the original codebase are resolved.

Omphalos (C:\Repos\omphalos) is a friend's DM campaign manager: React 18 + ASP.NET Core 10 Minimal API + PostgreSQL, JWT auth via httpOnly cookie, served from Docker. It already has an `/api` prefix, CORS `ALLOWED_ORIGIN` config, and admin settings infrastructure. The integration adds a shared-secret SSO endpoint and quest-session linking to Omphalos.

**Bidirectional design note:** The shared HMAC secret is symmetric — it can validate calls in either direction. Future phases can add Omphalos → Quest Board API calls using the same key without changing the security model.

The codebase map is current (analysed 2026-04-15): `.planning/codebase/`.

## Constraints

- **Compatibility:** No user-facing functionality may be removed or broken — all existing flows must work
- **Tech stack:** Stay on ASP.NET Core 8 MVC + SQL Server + EF Core — no framework changes
- **Deployment:** Must remain deployable via `docker-compose up` with no additional setup steps
- **Database:** All schema changes require EF Core migrations; auto-applied on startup
- **Standalone apps:** Both Quest Board and Omphalos must function independently without the other running
- **No stored credentials:** Shared secret stored in admin settings (DB) for Quest Board; env var for Omphalos

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Refactor + new features in same milestone | Avoids two sequential code-freeze windows; features land on clean architecture | ✓ Good — phases 1-4 refactor complete, features landed on clean arch |
| Bugs deferred to separate milestone | Bugs are isolated fixes; refactor may touch same code and create conflicts | — Standing |
| No pagination this milestone (M2) | Group size makes it a non-issue | — Reversed in Phase 09: shop pagination added |
| Phase 8 avatar crop deferred from M2 | No strong user demand; SkiaSharp native lib risk in Docker image | — Deferred to future milestone |
| HMAC shared-secret for cross-app auth | Simpler than OAuth; both apps remain standalone; symmetric for future bidirectional use | — Pending |
| Username-based user matching | Assumes same username in both apps; auto-provision on first SSO; no explicit linking step | — Pending |

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
*Last updated: 2026-06-18 — Milestone 3: Omphalos Integration started*
