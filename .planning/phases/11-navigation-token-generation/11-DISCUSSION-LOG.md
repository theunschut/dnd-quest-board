# Phase 11: Navigation + Token Generation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 11-navigation-token-generation
**Areas discussed:** Username field in token, Button placement on quest pages, Test coverage scope

---

## Username field in token

| Option | Description | Selected |
|--------|-------------|----------|
| currentUser.Name | Domain User.Name display name — used throughout codebase for DM identity | ✓ |
| User.Identity.Name | ASP.NET Core Identity claim — confirmed to be the email address in this app | |
| Both are the same | Initially user assumed they might be the same — Claude checked code and confirmed they differ | |

**User's choice:** currentUser.Name (after Claude confirmed UserName = email in IdentityService.cs)
**Notes:** User initially was unsure. Code check in `IdentityService.cs:37` (`UserName = email`) and `EntityProfile.cs` (`.ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))`) confirmed they are different fields. Display name (`Name`) is the correct choice for cross-app account matching with Omphalos.

---

## Button placement on quest pages

### Detail page

| Option | Description | Selected |
|--------|-------------|----------|
| In existing "DM Controls" card | Below "Manage Quest" button in existing sidebar card | ✓ |
| As separate "DM Tools" card | New card in the sidebar | |
| In quest header | Near quest title at top of page | |

**User's choice:** In the existing "DM Controls" card (recommended)
**Notes:** Minimal change, reuses existing DM-only gate (`ViewBag.CanManage`).

### Manage page

| Option | Description | Selected |
|--------|-------------|----------|
| New sidebar card below "View Public Page" | Follows same card pattern as existing sidebar | ✓ |
| In main action area near Finalize/Open buttons | More prominent, mixed with player management actions | |
| Inside "Quest Summary" card | Fewer cards, mixes metadata with action | |

**User's choice:** New sidebar card below "View Public Page" (recommended)
**Notes:** Clean isolation in the right sidebar.

---

## Test coverage scope

| Option | Description | Selected |
|--------|-------------|----------|
| Both: unit tests + integration tests | Unit tests for deterministic HMAC crypto; integration tests for endpoint behavior | ✓ |
| Integration tests only | Follow Phase 5–10 pattern exactly | |
| Unit tests only | No endpoint coverage | |

**User's choice:** Both (recommended)
**Notes:** Token generation is pure deterministic logic — unit tests are the right tool. The LaunchOmphalos endpoint has observable integration behavior (404/redirect) that warrants integration tests.

---

## Claude's Discretion

- `LaunchOmphalos` endpoint authorization: `[Authorize(Policy = "DungeonMasterOnly")]` + 404 when `!IsConfigured` (defense in depth)
- DI registration: `AddTransient<IIntegrationTokenService, IntegrationTokenService>()`
- Button icon and color for "Open Session Notes" — Claude decides based on visual fit
- ViewComponent file locations follow ASP.NET Core conventions

## Deferred Ideas

None — discussion stayed on-scope.
