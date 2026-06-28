# Phase 12: Mobile Infrastructure - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-23
**Phase:** 12-mobile-infrastructure
**Areas discussed:** Mobile nav content, CSS strategy during rollout, iPad/tablet detection

---

## Mobile nav content

### Q1: DM and Admin items in the offcanvas drawer

| Option | Description | Selected |
|--------|-------------|----------|
| Mirror desktop exactly | Same auth-conditional sections, same items — as list items (not dropdowns) in the offcanvas. | ✓ |
| Simplify DM items | Flatten DM dropdown to individual items without the 'Dungeon Master' header. | |
| You decide | Trust Claude to mirror faithfully. | |

**User's choice:** Mirror desktop exactly
**Notes:** No simplification — the existing nav structure reflects what each role needs.

---

### Q2: Should the mobile layout include site.css?

| Option | Description | Selected |
|--------|-------------|----------|
| Only mobile.css | Clean separation. Unstyled content during Phase 12 is intentional. | ✓ |
| mobile.css + site.css | Keep brand/global styles on mobile during transition. | |

**User's choice:** Only mobile.css
**Notes:** Accepted that Phase 12 is an infrastructure-only state; mobile views in Phases 13-16 will provide styling.

---

### Q3: Brand text in mobile navbar

| Option | Description | Selected |
|--------|-------------|----------|
| Icon + 'Quest Board' text | Matches desktop brand. Clear and recognizable. | ✓ |
| Icon only | More minimal on very small screens. | |
| You decide | Keep icon + text — consistent with desktop. | |

**User's choice:** Icon + 'Quest Board' text
**Notes:** Consistency with desktop brand preferred.

---

## CSS strategy during rollout

### Q1: Should Phase 12 include any mobile views?

| Option | Description | Selected |
|--------|-------------|----------|
| Phase 12 completes before any mobile views | True infrastructure-first. Phases 13-16 add views. | ✓ |
| Phase 12 includes at least one mobile view | Add Index.Mobile.cshtml as smoke test. | |

**User's choice:** Phase 12 completes before any mobile views
**Notes:** Clean separation — infrastructure lands first, then views per phase.

---

### Q2: What should mobile.css include beyond the 44px baseline?

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal — touch targets and basic spacing only | Only what INFRA-06 requires. | |
| D&D theme baseline | Include Cinzel font and dark navbar palette so nav looks on-brand. | ✓ |
| You decide | Include D&D theme baseline — nav should look branded. | |

**User's choice:** D&D theme baseline
**Notes:** The mobile nav shell should look on-brand even before mobile views land.

---

## iPad/tablet detection

### Q1: Should iPads get the mobile or desktop layout?

| Option | Description | Selected |
|--------|-------------|----------|
| Include iPad in mobile | All Apple tablets get mobile layout. Works on iPad too. | ✓ |
| Exclude iPad (desktop layout) | Remove 'iPad' from MobileKeywords. | |

**User's choice:** Include iPad in mobile
**Notes:** User asked "I believe the iPad can request the desktop version at any time if needed right?" — this implies a future "Switch to desktop" escape hatch. Deferred idea noted.

---

## Claude's Discretion

- Exact `MobileKeywords` array — use the research-documented set
- Named sections in `_Layout.Mobile.cshtml` (`@await RenderSectionAsync(...)`) — declare both required: false
- Container element for `@RenderBody()` — use `<main class="container-fluid px-2 mt-2">`
- Exact CSS selectors and values for touch targets

## Deferred Ideas

- **"Request desktop site" / cookie-based override** — user asked about this during iPad discussion. Already in REQUIREMENTS.md Future Requirements. Natural Phase 17+ extension.
