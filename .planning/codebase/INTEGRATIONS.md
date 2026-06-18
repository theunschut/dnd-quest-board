# External Integrations

**Analysis Date:** 2026-04-15

## APIs & External Services

**CDN-delivered frontend libraries** (no API key required):
- Bootstrap 5.3.0 — `https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/`
- Font Awesome 6.4.0 — `https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/`
- jQuery 3.6.0 — `https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/`
- Google Fonts (Cinzel) — `https://fonts.googleapis.com/`

All CDN references are in `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`. There are no fallback local copies; the app requires internet access for full frontend rendering.

## Data Storage

**Primary Database:**
- Microsoft SQL Server 2022
  - Connection env var: `ConnectionStrings__DefaultConnection`
  - Development connection string: `Server=localhost;Database=QuestBoard;User Id=QuestBoardUser;...`
  - Docker connection string: `Server=sqlserver;Database=QuestBoard;User Id=sa;Password=${MSSQL_SA_PASSWORD};...`
  - Client/ORM: Entity Framework Core 9.0.6 with `QuestBoardContext` in `EuphoriaInn.Repository/Entities/QuestBoardContext.cs`
  - Docker image: `mcr.microsoft.com/mssql/server:2022-latest`
  - Persistent volume: `sqlserver_data` (Docker local driver)

**File Storage:**
- Character images stored in the database via `CharacterImageEntity` (`EuphoriaInn.Repository/Entities/CharacterImageEntity.cs`)
- No external blob/object storage detected
- Static assets served from `EuphoriaInn.Service/wwwroot/` (local filesystem)

**Caching:**
- ASP.NET Core session (cookie-based, 24-hour idle timeout) — configured in `EuphoriaInn.Service/Program.cs`
- No distributed cache (Redis, Memcached, etc.) detected

## Authentication & Identity

**Auth Provider:**
- ASP.NET Core Identity (built-in, no external IdP)
  - `UserEntity` extends `IdentityUser<int>` — `EuphoriaInn.Repository/Entities/UserEntity.cs`
  - `QuestBoardContext` extends `IdentityDbContext<UserEntity, IdentityRole<int>, int>`
  - Password policy: min 6 chars, requires digit + uppercase + lowercase, no special chars required
  - Token provider: `AddDefaultTokenProviders()`
  - Configured in `EuphoriaInn.Service/Program.cs`

**Custom Authorization:**
- `DungeonMasterRequirement` + `DungeonMasterHandler` — `EuphoriaInn.Service/Authorization/`
- `AdminRequirement` + `AdminHandler` — `EuphoriaInn.Service/Authorization/`
- Policy names: `"DungeonMasterOnly"`, `"AdminOnly"`

**Session:**
- Cookie-based session, `HttpOnly`, `IsEssential = true`, 24-hour idle timeout
- Configured in `EuphoriaInn.Service/Program.cs`

## Email / Messaging

**Provider:**
- Gmail SMTP (direct SMTP, no third-party email SDK)
  - Server: `smtp.gmail.com`
  - Port: `587` (TLS/STARTTLS)
  - SSL: `EnableSsl = true`
  - Auth: username + app-specific password
  - Implementation: `EuphoriaInn.Domain/Services/EmailService.cs`

**Triggered Emails:**
- Quest finalized — sent to selected players (`SendQuestFinalizedEmailAsync`)
- Quest dates changed — sent to signed-up players (`SendQuestDateChangedEmailAsync`)

**Required Configuration (`appsettings.json` or env vars):**
- `EmailSettings:SmtpServer` — default `smtp.gmail.com`
- `EmailSettings:SmtpPort` — default `587`
- `EmailSettings:SmtpUsername` — Gmail account address
- `EmailSettings:SmtpPassword` — Gmail app-specific password (not account password)
- `EmailSettings:FromEmail` — sender address
- `EmailSettings:FromName` — default `"D&D Quest Board"`

Docker env var equivalents (commented out in `docker-compose.yml`):
- `EmailSettings__SmtpUsername` → `${SMTP_USERNAME}`
- `EmailSettings__SmtpPassword` → `${SMTP_PASSWORD}`
- `EmailSettings__FromEmail` → `${FROM_EMAIL}`

Email is optional — the service logs a warning and skips sending if credentials are not configured.

## Monitoring & Observability

**Health Checks:**
- ASP.NET Core built-in health checks registered via `AddHealthChecks()`
- Endpoint: `GET /health`
- Used by Docker Compose healthcheck: `curl -f http://localhost:8080/health`
- No custom health check probes detected (no database or SMTP checks registered)

**Logging:**
- ASP.NET Core built-in `ILogger<T>` — used throughout domain services and Program.cs
- Log levels configured in `appsettings.json`:
  - Default: `Information`
  - `Microsoft.AspNetCore`: `Warning`
- No external logging sink (Serilog, Application Insights, ELK, etc.) detected

**Error Tracking:**
- None detected. Production error page uses `UseExceptionHandler("/Error")`.

## CI/CD & Deployment

**Container Registry:**
- GitHub Container Registry (GHCR): `ghcr.io/theunschut/dnd-quest-board:latest`

**Hosting:**
- Docker Compose on self-hosted infrastructure
- App container: `dnd-questboard`, port `7080:8080`
- DB container: `dnd-db`, port `1433:1433`
- Both containers on external network `net-dnd`
- Restart policy: `unless-stopped`

**CI Pipeline:**
- Not detected in repository (no `.github/workflows/` directory found)

## Environment Configuration

**Required variables for production Docker deployment:**
- `MSSQL_SA_PASSWORD` — SQL Server SA password (used in both `docker-compose.yml` environment blocks)

**Optional variables (email, commented out in `docker-compose.yml`):**
- `SMTP_USERNAME`
- `SMTP_PASSWORD`
- `FROM_EMAIL`

**`.env` file:**
- Expected at project root for Docker Compose variable substitution
- Not committed to repository (in `.gitignore` or simply absent)

**Development connection string** (plaintext in `EuphoriaInn.Service/appsettings.json`):
- Uses a named SQL Server login (`QuestBoardUser`) against `localhost`
- Intended for WSL-to-Windows-host SQL Server access during development

## Webhooks & Callbacks

**Incoming:** None detected.

**Outgoing:** None detected. All external communication is initiated by the app (SMTP email only).

---

*Integration audit: 2026-04-15*
