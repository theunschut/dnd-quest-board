# Technology Stack

**Analysis Date:** 2026-04-15

## Languages

**Primary:**
- C# 12 (implicit via .NET 8 SDK) — all backend logic, domain models, repositories, services, controllers
- HTML/Razor — server-side views in `EuphoriaInn.Service/Views/`
- CSS — custom stylesheets in `EuphoriaInn.Service/wwwroot/css/`
- JavaScript — vanilla JS in `EuphoriaInn.Service/wwwroot/js/site.js`

## Runtime

**Environment:**
- .NET 8.0 (all five projects target `net8.0`)

**Package Manager:**
- NuGet (dotnet restore)
- No lockfile committed (no `packages.lock.json` detected)

## Frameworks

**Core:**
- ASP.NET Core 8 MVC (`Microsoft.NET.Sdk.Web`) — web layer in `EuphoriaInn.Service/`
- ASP.NET Core Identity 8.0.11 — authentication and user management
  - `Microsoft.AspNetCore.Identity` 2.3.1 — domain layer
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.11 — repository layer
  - `Microsoft.AspNetCore.Identity.UI` 8.0.11 — service layer scaffolding

**Mapping:**
- AutoMapper 14.0.0 — entity-to-model and model-to-viewmodel mapping
  - Used in both `EuphoriaInn.Domain` and `EuphoriaInn.Service`
  - Profiles: `EuphoriaInn.Domain/Automapper/EntityProfile.cs`, `EuphoriaInn.Service/Automapper/ViewModelProfile.cs`

**Testing:**
- xUnit 2.5.3 — test runner for both `EuphoriaInn.UnitTests` and `EuphoriaInn.IntegrationTests`
- FluentAssertions 8.8.0 — assertion library in both test projects
- NSubstitute 5.3.0 — mocking framework (unit tests only)
- Microsoft.AspNetCore.Mvc.Testing 8.0.11 — integration test host (`EuphoriaInn.IntegrationTests`)
- coverlet.collector 6.0.0 — code coverage collection

**Build/Dev:**
- .NET SDK 8.0 (dotnet CLI)
- Entity Framework Core Tools 9.0.6 (`Microsoft.EntityFrameworkCore.Tools`) — migration tooling
- Visual Studio solution format (`EuphoriaInn.sln`, VS version 18.2)

## Key Dependencies

**Critical:**
- `Microsoft.EntityFrameworkCore` 9.0.6 — ORM base (`EuphoriaInn.Repository`)
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.6 — SQL Server provider (`EuphoriaInn.Repository`)
- `Microsoft.EntityFrameworkCore.Design` 9.0.6 — design-time tools for migrations (`EuphoriaInn.Repository`)
- `AutoMapper` 14.0.0 — cross-layer object mapping (`EuphoriaInn.Domain`, `EuphoriaInn.Service`)
- `System.Security.Cryptography.Xml` 8.0.3 — cryptographic support (`EuphoriaInn.Domain`)

**Testing Infrastructure:**
- `Microsoft.EntityFrameworkCore.InMemory` 8.0.11 — in-memory EF provider (integration tests)
- `Microsoft.EntityFrameworkCore.Sqlite` 9.0.6 — SQLite provider for integration test database (`EuphoriaInn.IntegrationTests`)
- `Microsoft.Data.Sqlite` — SQLite connection used by `TestDatabase` helper
- `Microsoft.NET.Test.Sdk` 17.8.0 — test SDK

**Utility:**
- `Microsoft.Extensions.Configuration.Binder` 9.0.6 — configuration binding (`EuphoriaInn.Domain`)
- `System.Net.Http` 4.3.4 — HTTP client (both test projects)
- `System.Text.RegularExpressions` 4.3.1 — regex support (both test projects)

## Frontend Libraries (CDN)

All loaded via CDN in `EuphoriaInn.Service/Views/Shared/_Layout.cshtml`:
- Bootstrap 5.3.0 — CSS framework and JS components
- Font Awesome 6.4.0 — icon library
- jQuery 3.6.0 — DOM utilities
- Google Fonts (Cinzel) — D&D themed typography

Custom CSS files:
- `EuphoriaInn.Service/wwwroot/css/site.css`
- `EuphoriaInn.Service/wwwroot/css/calendar.css`
- `EuphoriaInn.Service/wwwroot/css/quests.css`
- `EuphoriaInn.Service/wwwroot/css/shop.css`
- `EuphoriaInn.Service/wwwroot/css/guild-members.css`

## Database

**Production:**
- Microsoft SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`)
- EF Core code-first with explicit migrations in `EuphoriaInn.Repository/Migrations/`
- Auto-applied on startup via `context.Database.Migrate()` in `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs`
- `DbContext`: `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — extends `IdentityDbContext<UserEntity, IdentityRole<int>, int>`

**Testing:**
- SQLite in-memory (`:memory:`) via persistent `SqliteConnection` — see `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs`
- Schema initialised with `EnsureCreated()` per test run

## Configuration

**Environment:**
- `EuphoriaInn.Service/appsettings.json` — base config (connection string, email settings, security config, logging)
- `EuphoriaInn.Service/appsettings.Development.json` — development overrides (logging levels only)
- Docker/production overrides via environment variables (double-underscore convention: `ConnectionStrings__DefaultConnection`)

**Security config keys:**
- `Security:PasswordIterations` — PBKDF2 iteration count
- `Security:SaltSize` — salt byte length
- `Security:HashSize` — hash byte length

**Build:**
- Multi-stage Dockerfile at `Dockerfile` — base `mcr.microsoft.com/dotnet/aspnet:8.0`, build `mcr.microsoft.com/dotnet/sdk:8.0`
- `docker-compose.yml` — orchestrates `questboard` app + `sqlserver` containers

## Platform Requirements

**Development:**
- .NET 8 SDK
- SQL Server (runs on Windows host when developing in WSL)
- dotnet-ef global tool for migration management

**Production:**
- Docker + Docker Compose
- External Docker network `net-dnd`
- Published image: `ghcr.io/theunschut/dnd-quest-board:latest` (GitHub Container Registry)
- Port 7080 (host) → 8080 (container)
- Health check endpoint: `GET /health`

---

*Stack analysis: 2026-04-15*
