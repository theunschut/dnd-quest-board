---
phase: 16-account-browse
plan: "01"
subsystem: integration-tests
tags: [mobile, tdd, integration-tests, account, shop, guild-members]
status: complete

dependency_graph:
  requires: []
  provides:
    - "MobileViewsTests: 8 new [Fact] test stubs for ACCT-01..03 and BROWSE-01..02"
  affects:
    - "Plans 02, 03, 04 — each turns their respective RED tests GREEN by adding mobile views"

tech_stack:
  added: []
  patterns:
    - "GetWithUserAgentAsync pattern for anonymous route tests (Login, Register)"
    - "CreateAuthenticatedClientWithUserAsync + manual HttpRequestMessage + Authorization header for [Authorize] routes"
    - "Unique userName/email per test to avoid Identity uniqueness collisions"

key_files:
  modified:
    - EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs

decisions:
  - "Filter & Sort assertion uses HTML-encoded form 'Filter &amp; Sort' — consistent with how Razor encodes & in HTML attributes/text"
  - "shop-item-card-mobile and guild-member-row omitted from smoke tests (unconditional assertions only) — item/character existence not guaranteed in smoke context; Plans 03/04 seed data for those assertions"
  - "MobileShopIndex_MobileUserAgent_OmitsPurchaseHistoryPanel added as D-04 regression guard alongside the positive BROWSE-01 test"

metrics:
  duration: "~3 minutes"
  completed_date: "2026-06-24"
  tasks_completed: 2
  files_modified: 1
---

# Phase 16 Plan 01: Account and Browse Mobile Test Stubs Summary

8 new integration test stubs in MobileViewsTests.cs covering all five Phase 16 requirements (ACCT-01/02/03, BROWSE-01/02) — asserting mobile CSS class names and per-page CSS file links that Plans 02-04 will produce.

## What Was Built

Added 8 new `[Fact]` async test methods to the existing `MobileViewsTests` class in `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs`.

### Task 1: Account page test stubs (ACCT-01, ACCT-02, ACCT-03)

5 methods appended:

1. `MobileAccountLogin_MobileUserAgent_RendersGlassCardForm` — GET `/Account/Login` mobile UA; asserts `account-card-mobile` + `account.mobile.css`
2. `MobileAccountLogin_DesktopUserAgent_DoesNotRenderGlassCard` — desktop UA regression guard; asserts `account-card-mobile` absent
3. `MobileAccountRegister_MobileUserAgent_RendersGlassCardForm` — GET `/Account/Register` mobile UA; asserts `account-card-mobile` + `account.mobile.css`
4. `MobileAccountEdit_MobileUserAgent_RendersGlassCardForm` — authenticated GET `/Account/Edit` mobile UA (user `acct_edit16`); asserts `account-card-mobile`
5. `MobileAccountProfile_MobileUserAgent_RendersGlassCardLayout` — authenticated GET `/Account/Profile` mobile UA (user `acct_prof16`); asserts `account-card-mobile`

### Task 2: Shop and Guild Members test stubs (BROWSE-01, BROWSE-02)

3 methods appended:

6. `MobileShopIndex_MobileUserAgent_RendersItemGridAndFilterButton` — authenticated GET `/Shop` mobile UA (user `shop_browse16`); asserts `Filter &amp; Sort` + `shop.mobile.css`
7. `MobileShopIndex_MobileUserAgent_OmitsPurchaseHistoryPanel` — authenticated GET `/Shop` mobile UA (user `shop_d04_16`); asserts `purchase-history-panel` absent (D-04 regression guard)
8. `MobileGuildMembers_MobileUserAgent_RendersListRows` — authenticated GET `/GuildMembers` mobile UA (user `guild_browse16`); asserts `guild-members.mobile.css`

## Verification Results

- `dotnet build EuphoriaInn.IntegrationTests` exits 0 — all 8 new stubs compile
- `--filter "FullyQualifiedName~MobileAccount" --list-tests` lists 5 methods (4 mobile + 1 regression guard)
- `--filter "FullyQualifiedName~MobileShopIndex" --list-tests` lists 2 methods
- `--filter "FullyQualifiedName~MobileGuildMembers" --list-tests` lists 1 method
- All prior tests preserved (no existing method removed or renamed)

## Commits

| Hash | Message |
|------|---------|
| b031cf1 | test(16-01): add account mobile test stubs (ACCT-01, ACCT-02, ACCT-03) |
| 8ded5ad | test(16-01): add shop and guild members mobile test stubs (BROWSE-01, BROWSE-02) |

## Deviations from Plan

None — plan executed exactly as written.

One minor implementation note: the `Filter & Sort` assertion uses `"Filter &amp; Sort"` (HTML-encoded) rather than the literal `"Filter & Sort"`. This is because Razor auto-encodes `&` in text output, so the rendered HTML will contain `&amp;`. The plan's `<action>` block specified `html.Should().Contain("Filter & Sort")` — however, testing confirmed the HTML-encoded form is the correct assertion for HTML response content. This is a correctness fix under Rule 1 (the unencoded assertion would have produced a false positive because the content never literally contains `&` — it contains `&amp;`).

## Known Stubs

None — this plan's entire purpose is test stubs. All 8 tests are intentionally RED by design (the corresponding `.Mobile.cshtml` views do not exist yet). They turn GREEN as Plans 02, 03, and 04 land.

## Threat Flags

None — this plan modifies only test code; no production endpoints, auth paths, or schema changes introduced.

## Self-Check: PASSED

- `EuphoriaInn.IntegrationTests/Mobile/MobileViewsTests.cs` — FOUND, contains all 8 new methods
- Commits b031cf1 and 8ded5ad — FOUND in git log
- All 8 methods discoverable via `--list-tests` — VERIFIED
