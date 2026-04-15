# Project Research Summary

**Project:** D&D Quest Board — Milestone 2: Refactor + Feature Expansion
**Domain:** ASP.NET Core 8 MVC — Clean Architecture refactor of existing brownfield app
**Researched:** 2026-04-15
**Confidence:** HIGH

## Executive Summary

This milestone refactors an existing, working ASP.NET Core 8 MVC application to fix a compile-time dependency inversion violation (Domain depends on Repository, when it should be the reverse), slim down controller actions by moving business logic into services, fix a set of security gaps, and add four user-facing features from the GitHub backlog.

The single highest-leverage change is moving `EntityProfile.cs` (AutoMapper Entity↔DomainModel mappings) from `EuphoriaInn.Domain` to `EuphoriaInn.Repository`. This one file is the reason Domain holds a `<ProjectReference>` to Repository. Removing that reference restores the correct dependency direction: `Service → Domain ← Repository`. Everything else in the architectural refactor flows from this fix.

The four new features (DM profile page, shop filter/sort, profile picture avatar crop, follow-up quest) are independent of each other and can be developed in any order after the architecture cleanup. The most complex technically is image cropping, which requires Cropper.js 1.5.x via CDN (not v2 — ESM-only) and SkiaSharp for server-side crop storage. The simplest is shop filter/sort, which needs no data model changes.

## Key Findings

### Recommended Stack

Keep the existing stack entirely. No framework changes. Specific targeted additions:

**Core technologies:**
- `MailKit 4.15.1`: Replace deprecated `System.Net.Mail.SmtpClient` in `EmailService` — Microsoft's recommended alternative; same email content, different send API
- `SkiaSharp` (via NuGet): Server-side image crop for avatar feature — `System.Drawing.Common` is deprecated on Linux/Docker in .NET 8
- `Cropper.js 1.5.13` (via cdnjs CDN): Client-side avatar crop UI — v1.5.x ships UMD bundles compatible with script-tag loading; v2.x is ESM-only and incompatible with this project's CDN-only frontend

**Stay as-is:**
- ASP.NET Core 8 MVC — no reason to change
- EF Core 9 + SQL Server — no changes needed
- AutoMapper 14 — keep explicit `AddProfile<T>()` registration (never switch to assembly scanning)
- Bootstrap 5 + vanilla JS + jQuery — consistent with all existing views

**Configuration improvement:**
- `IOptions<EmailSettings>` pattern (already in .NET 8, no new packages) to replace `IConfiguration` string-key access in `EmailService`

### Expected Features

**Must have (table stakes for this milestone):**
- Layer dependency direction fixed — Domain compiles without referencing Repository
- Controller actions ≤ 20 lines for typical operations (validate → service → respond)
- Account lockout enabled including for existing users (SQL migration required)
- `.env` removed from git tracking

**Should have (core new features):**
- DM profile page (#98) — photo + name + bio, browsable by players
- Shop filter/sort (#96) — by price and rarity, server-side LINQ, no JS library needed
- Profile picture avatar crop (#78) — Cropper.js 1.5.x client-side select, SkiaSharp server-side crop
- Follow-up quest (#49) — self-referential FK, pre-filled player list, new date required

**Defer (out of scope this milestone):**
- MailKit migration — parallel to email refactor but scope-expanding; suppress `SmtpClient` warning and defer
- Pagination — group is small enough that unbounded lists are acceptable now
- Image blob storage migration — deferred per PROJECT.md constraint

### Architecture Approach

Move `EntityProfile.cs` to `EuphoriaInn.Repository/Automapper/`, remove `<ProjectReference>` to Repository from `EuphoriaInn.Domain.csproj`, update `Program.cs` AutoMapper registration to reference `typeof(EuphoriaInn.Repository.Automapper.EntityProfile)`. Then inject `IEmailService` into `QuestService` and move email dispatch out of controllers into services. Add a lightweight `ServiceResult` record to communicate service success/failure to controllers without exposing business logic. `BaseService<TModel, TEntity>` requires no changes — its generic `TEntity` parameter is a compile-time placeholder, not a Repository type import.

**Major components after refactor:**
1. `EuphoriaInn.Domain` — pure business logic, interfaces, domain models; no EF/HTTP dependencies
2. `EuphoriaInn.Repository` — EF entities, DbContext, EntityProfile, concrete repository implementations
3. `EuphoriaInn.Service` — thin controllers, ViewModels, ViewModelProfile; talks only to Domain interfaces

### Critical Pitfalls

1. **AutoMapper assembly scanning** — never switch to `AppDomain.CurrentDomain.GetAssemblies()` scanning; it causes `DuplicateTypeMapConfigurationException` at startup. Keep explicit `AddProfile<T>()` calls for both profiles.
2. **Stale quest state in email dispatch** — `FinalizeQuestAsync` must build the email recipient list from post-save entity state, not from the `quest` object fetched before the mutating call. Either re-fetch after `SaveChangesAsync` or return recipients from the service.
3. **Lockout doesn't apply to existing users** — enabling `lockoutOnFailure: true` in code does nothing for users with `LockoutEnabled = 0` in `AspNetUsers`. An EF migration with `UPDATE AspNetUsers SET LockoutEnabled = 1` is mandatory.
4. **Password property AutoMapper trap** — removing `Password` from `User` domain model requires explicit `Ignore()` on both directions of any `ReverseMap()` that touches User mapping, or a future `UpdateAsync` will overwrite users' password hashes with empty string.
5. **`EntityProfile` move order** — remove Domain's project reference to Repository only after `EntityProfile.cs` has been physically moved and the `Program.cs` AutoMapper registration updated. Out-of-order execution causes cascading build failures.

## Implications for Roadmap

### Phase 1: Layer Dependency Fix
**Rationale:** The root architectural violation must be fixed before new features are added. Every subsequent phase lands on correct architecture.
**Delivers:** `EntityProfile` in Repository, Domain project reference to Repository removed, `Program.cs` AutoMapper registration updated, build green
**Avoids:** Pitfalls 1 and 5 (AutoMapper scanning, wrong move order)

### Phase 2: Email & Service Consolidation
**Rationale:** Independent of Phase 1's exact file moves but requires Domain to be clean. Consolidates two email dispatch flows and introduces `IOptions<EmailSettings>`.
**Delivers:** `IEmailService` injected into `QuestService`, `FinalizeQuestAsync` dispatches emails internally, `UpdateQuestPropertiesWithNotificationsAsync` returns `ServiceResult`, `EmailSettings` typed options class, `[Quest Board URL]` placeholder replaced
**Avoids:** Pitfall 2 (stale quest state), Pitfall 5 (IOptions DI registration)

### Phase 3: Code Quality & Dead Code
**Rationale:** Low-risk housekeeping; unblocks the security phase by removing confusing dead code first.
**Delivers:** `SecurityConfiguration` deleted, dead `UpdateQuestPropertiesAsync` removed, `SignupRole` cast to enum, 30-minute constant named, `CharacterViewModels` filename fixed, `Password` removed from `User` model
**Avoids:** Pitfalls 6, 7, 8, 9, 10 (see PITFALLS.md)

### Phase 4: Security Fixes
**Rationale:** Isolated config and model changes; all code quality cleanup in Phase 3 makes these targeted edits.
**Delivers:** Lockout enabled + SQL migration for existing users, password min length 8, `HasKey` admin-only, `.env` in `.gitignore`, `Password` property audit complete
**Avoids:** Pitfall 3 (lockout existing users), Pitfall 13 (.env history note)

### Phase 5: Shop Filter/Sort (#96)
**Rationale:** Zero migrations, no new libraries, immediate visible value. Good warmup feature after the refactor.
**Delivers:** Price ASC/DESC and rarity filter on shop index; server-side LINQ; Bootstrap collapse filter panel

### Phase 6: Follow-Up Quest (#49)
**Rationale:** Requires `SignupRole` enum fix from Phase 3. Self-referential FK migration, pre-filled player list on quest create.
**Delivers:** "Create follow-up" button on finalized quest Manage page; `OriginalQuestId` FK on `QuestEntity`; players pre-approved

### Phase 7: DM Profile Page (#98)
**Rationale:** Follows `CharacterImageEntity` pattern established in codebase. Bio field migration + photo storage (same SQL blob pattern).
**Delivers:** `DungeonMasterProfile` entity with photo and bio; browsable profile page; admin can edit any DM's profile

### Phase 8: Profile Picture Avatar Crop (#78)
**Rationale:** Most technically novel feature — Cropper.js + SkiaSharp. Done last so scope risk doesn't delay other features.
**Delivers:** Cropper.js 1.5.x on character edit page; `CropX/Y/Width/Height` columns on `CharacterImages`; `GetAvatarPicture` endpoint; original image unchanged on character detail page
**Research flag:** Verify SkiaSharp native dependency available in `mcr.microsoft.com/dotnet/aspnet:8.0` Docker image before starting

### Phase Ordering Rationale

- Phases 1–4 fix the house before decorating it; features land on clean architecture
- Phase 5 (shop filter) comes first among features because it is zero-risk and delivers immediate value
- Phase 6 (follow-up quest) depends on Phase 3's `SignupRole` enum fix
- Phase 8 (avatar crop) is last because Cropper.js + SkiaSharp Docker validation is the only meaningful open question

### Research Flags

Phases needing attention during plan-phase:
- **Phase 8 (avatar crop):** Confirm SkiaSharp native lib availability in the Docker base image before committing to the implementation approach; CSS-only crop-display fallback if unavailable
- **Phase 6 (follow-up quest):** Confirm EF self-referential FK migration generates correctly with nullable `OriginalQuestId`

Phases with standard patterns (plan-phase research optional):
- **Phase 1:** Well-documented AutoMapper + project reference change; no surprises expected
- **Phase 2–4:** Configuration patterns, Identity lockout, dead code removal — all standard
- **Phase 5:** LINQ filter/sort — trivial; plan directly from requirements
- **Phase 7:** Follows existing CharacterImageEntity pattern exactly

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Existing stack stays; SkiaSharp and Cropper.js CDN verified |
| Features | HIGH | All 4 features inspected against live codebase; implementation paths clear |
| Architecture | HIGH | Dependency direction fix verified against Microsoft docs and csproj inspection |
| Pitfalls | HIGH | All pitfalls grounded in actual codebase file inspection |

**Overall confidence:** HIGH

### Gaps to Address

- **SkiaSharp on Docker:** Verify `libSkiaSharp` native dependency in `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian Bookworm) before Phase 8 begins. Fallback: store crop coordinates and apply CSS `object-position` on the avatar `<img>` tag (no server-side crop needed).
- **MailKit migration:** Deliberately deferred. `SmtpClient` obsolete warning suppressed with `#pragma`. Schedule as its own task in a later milestone.

## Sources

### Primary (HIGH confidence)
- Microsoft .NET Microservices Architecture Guide — infrastructure layer patterns
- Microsoft Learn — Options pattern in ASP.NET Core 8
- Microsoft Learn — ASP.NET Core Identity lockout configuration
- AutoMapper GitHub issues — duplicate profile registration behavior

### Secondary (MEDIUM confidence)
- cdnjs.cloudflare.com — Cropper.js 1.5.13 CDN availability confirmed
- thecodewrapper.com — EF Core entity/domain separation patterns
- code-maze.com — Identity lockout per-user `LockoutEnabled` column behavior

---
*Research completed: 2026-04-15*
*Ready for roadmap: yes*
