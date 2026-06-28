# Phase 7: DM Profile Page - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 07-dm-profile-page
**Areas discussed:** Profile data storage, Edit UX location, Profile page content depth, Admin edit placement

---

## Profile data storage

| Option | Description | Selected |
|--------|-------------|----------|
| Extend UserEntity directly | Add Bio to UserEntity + DungeonMasterProfileImage linked to UserId. Simplest — no new intermediate entity. Bio would exist on all users but only DMs use it. | |
| New DungeonMasterProfileEntity | Separate entity linked to UserId; holds Bio + navigation to DungeonMasterProfileImage. DM-specific data isolated. | ✓ |
| You decide | Claude picks the approach that fits existing patterns. | |

**User's choice:** New DungeonMasterProfileEntity — keep DM-specific data isolated from UserEntity.

**Follow-up — When is the entity created?**

| Option | Description | Selected |
|--------|-------------|----------|
| Lazily on first save | Entity is null until DM saves profile for the first time. Profile page handles null gracefully. | ✓ |
| Eagerly on DM role assignment | Row created whenever a user is promoted to DM. Requires logic in role-promotion flow. | |

**User's choice:** Lazily on first save.

---

## Edit UX location

| Option | Description | Selected |
|--------|-------------|----------|
| Dedicated /Account/EditDMProfile page | Separate page, only DMs can reach. Keeps Account/Edit clean. | |
| Add DM fields to existing Account/Edit | Conditionally show DM fields. One page, longer, DM-conditional. | |
| You decide | Claude picks the cleanest approach. | |

**User's choice:** Dedicated separate page — but in the **DM navbar dropdown**, not the Account area menu.

**Follow-up — What URL?**

| Option | Description | Selected |
|--------|-------------|----------|
| /DungeonMaster/EditProfile | Lives in new DungeonMasterController. DM-specific pages stay grouped. | ✓ |
| /Account/EditDMProfile | Lives in AccountController alongside other account pages. | |

**User's choice:** `/DungeonMaster/EditProfile` in a new DungeonMasterController.

---

## Profile page content depth

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal — name, photo, bio only | Exactly DMPRO-01. Clean profile card. | |
| Bio + quest history list | Past and active quests the DM has run. Title, date, difficulty. | ✓ |
| Bio + stats summary | Just quest counts — lightweight. | |

**User's choice:** Bio + quest history list.

**Follow-up — Quest list details:**

| Option | Description | Selected |
|--------|-------------|----------|
| All quests, most recent first | Finalized and active. Simple list. | ✓ |
| Only finalized quests | Track record only — excludes active/upcoming. | |
| You decide | Claude picks most useful display. | |

**User's choice:** All quests, most recent first.

---

## Admin edit placement

| Option | Description | Selected |
|--------|-------------|----------|
| Link from DM profile page to admin edit action | Edit button on profile page visible to admins. Links to /Admin/EditDMProfile/{id}. | |
| Extend existing /Admin/EditUser with DM fields | Add bio + photo conditionally to admin user-edit page. | |
| You decide | Claude picks the cleanest admin flow. | |

**User's choice:** Same pattern as Quest Manage — "Edit Profile" button on the public DM profile page, visible to both the DM owner and admins. Same `/DungeonMaster/EditProfile/{id}` route; `[Authorize(Policy = "DungeonMasterOnly")]` + owner-or-admin inline check (mirrors `QuestController.Manage`).

**Notes:** User explicitly referenced the quest manage pattern: players see `/Quest/Details/{id}`, admins get a button that routes to `/Quest/Manage/{id}` as if they're the DM. Same flow for DM profiles — admins see an "Edit Profile" button on `/DungeonMaster/Profile/{id}` that goes to `/DungeonMaster/EditProfile/{id}`.

---

## Claude's Discretion

- Exact HTML/CSS layout of the profile page
- Whether to add a new IDungeonMasterService or extend IUserService
- Image serving endpoint name
- Whether DungeonMasterProfileEntity.Id shares the UserId value or uses independent PK
- Whether DungeonMasterProfileEntity navigation lives on UserEntity

## Deferred Ideas

None — discussion stayed within phase scope.
