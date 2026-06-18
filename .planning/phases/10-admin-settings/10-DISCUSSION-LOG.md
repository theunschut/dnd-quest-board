# Phase 10: Admin Settings - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-18
**Phase:** 10-admin-settings
**Areas discussed:** Entity schema design, IAdminSettingService interface, Test coverage approach, Default state behavior

---

## Entity Schema Design

| Option | Description | Selected |
|--------|-------------|----------|
| Typed single-row entity | IntegrationSettingsEntity with named columns: OmphalosUrl, OmphalosSharedSecret, IsEnabled, UpdatedAt. Matches SETT-06. Simpler service, explicit schema. | |
| Key-value store | AdminSettingEntity with Key (PK), Value, UpdatedAt. Generic — no migration when adding new settings. | ✓ |

**User's choice:** Key-value store
**Notes:** STATE.md architectural decision confirmed — avoids new EF migrations when adding future admin settings keys. SETT-06 typed-column spec is overridden by this decision. Keys in use: OmphalosUrl, OmphalosSharedSecret, IsEnabled.

---

## IAdminSettingService Interface

| Option | Description | Selected |
|--------|-------------|----------|
| GetSettingsAsync() → typed record | Single method returning IntegrationSettings record. One DB round-trip per call. Clean API for Phase 11 callers. | ✓ |
| Individual getters per property | IsIntegrationEnabledAsync(), GetOmphalosUrlAsync(), GetSharedSecretAsync(). More granular but multiple DB hits. | |

**User's choice:** GetSettingsAsync() → typed record
**Notes:** IntegrationSettings record contains OmphalosUrl (string?), OmphalosSharedSecret (string?), IsEnabled (bool). Used by Phase 11 ViewComponent and token service.

---

## Default State (when no settings in DB)

| Option | Description | Selected |
|--------|-------------|----------|
| Return default record with IsEnabled=false | Never returns null. Callers need no null checks. IsEnabled=false hides all Omphalos UI on fresh installs. | ✓ |
| Return null | Explicit signal that settings are unconfigured. Requires null-check at every call site. | |

**User's choice:** Return default record with IsEnabled=false
**Notes:** Phase 11 callers can safely use settings.IsEnabled without null checks. URL and secret are null in the default record.

---

## Test Coverage Approach

| Option | Description | Selected |
|--------|-------------|----------|
| Integration tests for service + repository | Follow Phase 5-7 pattern. SQLite in-memory. Cover GetSettingsAsync(), blank-secret-preserves-existing (SETT-04), upsert behavior. | ✓ |
| Unit tests only | Mock IAdminSettingService. Faster to write but doesn't verify EF entity/repository layer. | |

**User's choice:** Integration tests for service + repository
**Notes:** Matching the established milestone pattern. SETT-04 (blank secret preservation) is explicitly a test case.

---

## Claude's Discretion

- Table name for AdminSettingEntity
- View layout (follows modern-card pattern per CLAUDE.md)
- Settings link placement in Admin navbar dropdown
- IntegrationSettings record file location in Domain

## Deferred Ideas

None surfaced during discussion.
