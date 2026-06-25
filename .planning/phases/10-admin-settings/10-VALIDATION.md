---
phase: 10
slug: admin-settings
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.5.3 + FluentAssertions 8.8.0 |
| **Config file** | `EuphoriaInn.IntegrationTests/EuphoriaInn.IntegrationTests.csproj` |
| **Quick run command** | `dotnet test EuphoriaInn.IntegrationTests --filter "AdminSetting" --no-build` |
| **Full suite command** | `dotnet test EuphoriaInn.IntegrationTests --no-build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test EuphoriaInn.IntegrationTests --filter "AdminSetting" --no-build`
- **After every plan wave:** Run `dotnet test EuphoriaInn.IntegrationTests --no-build`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 10-01-01 | 01 | 1 | D-10, D-05 | — | GetSettingsAsync returns default when DB empty | Integration (service) | `dotnet test --filter "GetSettingsAsync_WhenDbEmpty"` | ❌ W0 | ⬜ pending |
| 10-01-02 | 01 | 1 | D-10, SETT-04, D-08 | — | Blank secret on save preserves existing secret | Integration (service) | `dotnet test --filter "BlankSecret_Preserves"` | ❌ W0 | ⬜ pending |
| 10-01-03 | 01 | 1 | D-10, SETT-05 | — | GetSettingsAsync returns stored values after save | Integration (service) | `dotnet test --filter "GetSettingsAsync_ReturnsStoredValues"` | ❌ W0 | ⬜ pending |
| 10-01-04 | 01 | 1 | D-10 | — | Upsert twice: second save overwrites first | Integration (service) | `dotnet test --filter "SaveSettings_Twice_Overwrites"` | ❌ W0 | ⬜ pending |
| 10-02-01 | 02 | 1 | SETT-07 | T-AuthZ | Non-admin returns 302/403 on GET /Admin/Settings | Integration (HTTP) | `dotnet test --filter "AdminController"` | ⚠️ needs new method | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `EuphoriaInn.IntegrationTests/Services/AdminSettingServiceTests.cs` — stubs for D-05, SETT-04/D-08, SETT-05, and double-upsert (tasks 10-01-01 through 10-01-04)
- [ ] New test method in `EuphoriaInn.IntegrationTests/Controllers/AdminControllerIntegrationTests.cs` — covers SETT-07 (task 10-02-01)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Secret field renders masked in browser | SETT-03 | Rendering concern — not testable via HTTP string matching | Load `/Admin/Settings` in browser; confirm secret input renders as dots |
| `type="password"` field loads empty on GET | D-09 | Browser behavior — automated test can assert `value=""` in HTML but masked rendering requires visual check | GET `/Admin/Settings`; inspect source; confirm `OmphalosSharedSecret` input has no `value` attribute |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
