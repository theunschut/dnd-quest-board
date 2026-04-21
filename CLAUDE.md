# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a comprehensive D&D Quest Board web application built with ASP.NET Core 8 MVC following a clean architecture pattern. The application provides a complete solution for D&D campaign management including user authentication, quest creation, calendar scheduling, player coordination, and email notifications. It features a layered architecture with domain, repository, and service layers following SOLID principles and dependency injection patterns.

## Development Environment

**Important**: This project is developed in a WSL Ubuntu environment, but SQL Server runs on the Windows host machine. When running the application locally, it connects to SQL Server on the Windows host.

## Development Commands

### From Root Directory (using solution)
```bash
# Build the entire solution
dotnet build

# Run the application
dotnet run --project EuphoriaInn.Service

# Restore packages for solution
dotnet restore
```

### From EuphoriaInn.Service Directory
```bash
# Run the application
dotnet run

# Build the application  
dotnet build

# Restore packages
dotnet restore

# Update database (if adding migrations)
dotnet ef database update

# Create Entity Framework migrations (run from Service project)
dotnet ef migrations add MigrationName --project ../EuphoriaInn.Repository
```

### Connection String Notes
- **Development**: Uses `localhost` to connect to SQL Server running on Windows host from WSL
- **Docker**: Uses `sqlserver` service name for container-to-container communication

### Docker Development (from root directory)
```bash
# Build and run with Docker Compose (includes SQL Server)
docker-compose up -d

# View logs
docker-compose logs -f questboard

# Stop containers
docker-compose down
```

## Project Architecture

The application follows a clean architecture pattern with three main layers:

### Project Structure
- **EuphoriaInn.Domain**: Contains business models, enums, and core domain logic
- **EuphoriaInn.Repository**: Data access layer with Entity Framework Core, repositories, and AutoMapper profiles
- **EuphoriaInn.Service**: MVC web application with controllers, views, services, and view models

### Technology Stack
- **Backend**: ASP.NET Core 8 MVC with Repository Pattern
- **Database**: Microsoft SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript with D&D theming
- **Email**: .NET SMTP with Gmail integration
- **Mapping**: AutoMapper for entity-to-model mapping
- **Deployment**: Docker containerization

### Core Domain Models (EuphoriaInn.Domain)
- `Quest`: Main quest entity with title, description, difficulty, DM info, and scheduling
- `User`: User account management with authentication and profile information
- `ProposedDate`: Date options for each quest with voting capabilities
- `PlayerSignup`: Player registration for quests with user association
- `PlayerDateVote`: Player votes on proposed dates (Yes/No/Maybe)
- `Difficulty`: Enum for quest difficulty levels (Easy, Medium, Hard, Deadly)
- `VoteType`: Enum for player vote types (Yes, No, Maybe)

### Domain Services (EuphoriaInn.Domain/Services)
- `QuestService`: Business logic for quest management and coordination
- `UserService`: User account management and authentication logic
- `PlayerSignupService`: Player registration and quest participation logic
- `EmailService`: Email notification service with Gmail SMTP integration
- `BaseService`: Abstract base class providing common service functionality

### Domain Configuration
- `SecurityConfiguration`: Centralized security settings and authentication configuration
- `ServiceExtensions`: Dependency injection configuration for domain services

### Repository Layer (EuphoriaInn.Repository)
- `IQuestRepository`: Interface defining quest data operations
- `QuestRepository`: Implementation with Entity Framework Core
- `IUserRepository`: Interface for user account data operations
- `UserRepository`: Implementation for user CRUD operations
- `IPlayerSignupRepository`: Interface for player signup data operations
- `PlayerSignupRepository`: Implementation for signup management
- `BaseRepository<T>`: Generic base repository providing common CRUD operations
- `QuestBoardContext`: Database context with entity configurations
- Entity classes with database-specific configurations and relationships
- AutoMapper profiles for entity-to-domain model mapping
- `ServiceExtensions`: Repository dependency injection configuration

### Service Layer (EuphoriaInn.Service)
#### Controllers
- `HomeController`: Handles main quest board display and dashboard
- `QuestController`: Manages quest CRUD operations and player interactions
- `DungeonMasterController`: Handles DM registration and directory management
- `AccountController`: User authentication, registration, and profile management
- `CalendarController`: Calendar view and quest scheduling functionality

#### Authorization & Security
- `DungeonMasterRequirement`: Custom authorization requirement for DM operations
- `DungeonMasterHandler`: Authorization handler implementing DM access control
- Session-based authentication with role-based access control

#### View Models & Data Transfer
- `QuestViewModels`: CreateQuestViewModel, QuestViewModel for quest operations
- `AccountViewModels`: LoginViewModel, RegisterViewModel for authentication
- `DungeonMasterViewModels`: CreateDungeonMasterViewModel, DungeonMasterIndexViewModel
- `CalendarViewModels`: CalendarViewModel, CalendarDay, QuestOnDay for scheduling
- `ViewModelProfile`: AutoMapper configuration for view model mapping

#### Views & UI
- Razor views with Bootstrap 5 styling and D&D theming
- Responsive design with mobile-first approach
- JavaScript enhancements for dynamic form interactions

### Key Pages & Functionality
#### Public & Authentication
- `/` - Main quest board displaying all available quests
- `/Account/Login` - User authentication with session management
- `/Account/Register` - New user registration with validation
- `/Account/Profile` - User profile management and settings

#### DM Management
- `/DungeonMaster` - Browse registered Dungeon Masters with registration form
- `/DungeonMaster/Create` - DM registration with validation

#### Quest Operations
- `/Quest/Create` - DM quest creation with date options and difficulty selection
- `/Quest/Details/{id}` - Quest details and player signup with date voting
- `/Quest/Manage/{id}` - DM interface for finalizing quests and selecting players
- `/Quest/MyQuests` - DM's personal quest management dashboard

#### Calendar & Scheduling
- `/Calendar` - Monthly calendar view displaying all scheduled quests
- Calendar navigation with quest scheduling visualization

### Database Context
- Uses `QuestBoardContext` with Entity Framework Core
- Microsoft SQL Server database with automatic migration on startup
- Entity configurations handle relationships and constraints

### Email Service
- `IEmailService` interface with Gmail SMTP implementation
- Sends notifications to selected players when quests are finalized
- Configuration in `appsettings.json` EmailSettings section
- Dependency injection for service registration

### Security & Session Management
- Comprehensive user authentication system with ASP.NET Core Identity-like features
- Session-based authentication with secure cookie management
- Custom authorization handlers for Dungeon Master role verification
- Role-based access control protecting DM-only operations
- Secure password handling and user profile management
- Session storage for maintaining user state across requests

## Configuration

### Required Settings (appsettings.json)
- `ConnectionStrings:DefaultConnection` - SQL Server connection string
- `EmailSettings:SmtpUsername` - Gmail account for sending emails
- `EmailSettings:SmtpPassword` - Gmail app-specific password
- `EmailSettings:FromEmail` - From email address

### Environment Variables (Docker)
- `SA_PASSWORD` - SQL Server SA password for Docker container
- `SMTP_USERNAME`, `SMTP_PASSWORD`, `FROM_EMAIL` for email configuration
- Can be set in `.env` file for Docker Compose
- Overrides appsettings.json values when present

## Development Notes

### Frontend Features
- Uses Bootstrap 5 for responsive UI with custom CSS for difficulty badges
- JavaScript handles dynamic form elements (adding/removing date options)
- Auto-refresh on quest detail pages every 30 seconds
- Color-coded difficulty system (Easy=green, Medium=yellow, Hard=red, Deadly=purple)
- D&D themed styling with quest poster imagery
- DM registration form with validation and themed styling
- Responsive table layout for DM directory

### Business Logic
- Complete user account system with registration and authentication
- DM registration system with name and optional email
- Quest creation with DM selection and multiple proposed dates
- First-come-first-served player selection with configurable maximum
- Comprehensive date voting system with Yes/No/Maybe options
- Automatic date recommendation based on most "Yes" votes
- Role-based authorization for DM operations
- Calendar integration for quest scheduling and visualization
- Email notifications sent only to selected players when quest is finalized
- Session management for maintaining user state and preferences

### Architecture Patterns
- Clean Architecture with domain-driven design principles
- Repository pattern with generic base implementation and dependency injection
- AutoMapper for clean separation between entities, domain models, and view models
- MVC pattern with strongly-typed view models for form binding and data transfer
- Service layer pattern for business logic (QuestService, UserService, EmailService)
- Custom authorization patterns with handlers and requirements
- Dependency injection throughout all layers for loose coupling
- Entity Framework Core with code-first approach and migrations
- Clean separation of concerns across three distinct projects

## Entity Framework Guidelines

### Package Management
- **IMPORTANT**: Entity Framework packages should ONLY be added to the Repository project
- Never add EF packages (like Microsoft.EntityFrameworkCore.Design) to the Service project
- The Repository project contains all EF-related dependencies and database context

### Migration Commands
```bash
# Install EF tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Create a new migration (run from Service project directory)
cd EuphoriaInn.Service
dotnet ef migrations add MigrationName --project ../EuphoriaInn.Repository

# Apply migrations to database
dotnet ef database update --project ../EuphoriaInn.Repository

# Remove the last migration (if needed)
dotnet ef migrations remove --project ../EuphoriaInn.Repository
```

### Automatic Migration Application
- The application automatically applies pending migrations on startup
- This is handled in `ServiceExtensions.ConfigureDatabase()` using `context.Database.Migrate()`
- No manual database update commands needed - just run the application after creating migrations
- This ensures the database schema is always up to date in development environments

### Migration Troubleshooting
If you encounter "table already exists" errors when switching from EnsureCreated to migrations:

1. **For development**: Delete the `quests.db` file and restart the application
2. **For clean migration setup**: 
   ```bash
   # Remove current migrations
   dotnet ef migrations remove --project ../EuphoriaInn.Repository
   
   # Create initial migration (from current schema)
   dotnet ef migrations add InitialCreate --project ../EuphoriaInn.Repository
   
   # Delete database and restart app to apply migrations cleanly
   ```
3. The `ConfigureDatabase()` method includes fallback handling for existing databases

## UI/UX Design Guidelines

### Modern Card Styling
- **IMPORTANT**: All new views should use the modern card styling pattern for consistency
- Use `modern-card` class for the main card container
- Use `modern-card-header` class for card headers with consistent styling
- Use `modern-card-body` class for card body content
- Card headers should include an icon and title using this pattern:
  ```html
  <div class="card-header modern-card-header">
      <h2 class="mb-0">
          <i class="fas fa-icon-name text-color me-2"></i>
          Page Title
      </h2>
  </div>
  ```
- Always include a horizontal rule (`<hr>`) before the button section
- Use consistent button styling with icons:
  ```html
  <div class="d-flex justify-content-between">
      <a href="..." class="btn btn-secondary">
          <i class="fas fa-arrow-left me-2"></i>
          Cancel/Back
      </a>
      <button type="submit" class="btn btn-primary">
          <i class="fas fa-save me-2"></i>
          Action Text
      </button>
  </div>
  ```

### Button Guidelines
- Always use filled in colored buttons instead of outline only
- Include FontAwesome icons with `me-2` spacing class
- Use semantic colors (primary, secondary, success, danger, warning)
- Maintain consistent button layout with `d-flex justify-content-between`

<!-- GSD:project-start source:PROJECT.md -->
## Project

**D&D Quest Board — Milestone 2: Refactor + Feature Expansion**

A D&D campaign management web application for a group of players and Dungeon Masters. It handles quest creation and scheduling, player signup with date voting, a character/guild system, a shop with gold economy, and email notifications. Built with ASP.NET Core 8 MVC, SQL Server, and Docker — deployed as a single container to a self-hosted environment.

**Core Value:** The quest board must reliably let DMs post quests and players sign up — everything else enhances that loop.

### Constraints

- **Compatibility:** No user-facing functionality may be removed or broken — all existing flows must work after the refactor
- **Tech stack:** Stay on ASP.NET Core 8 MVC + SQL Server + EF Core — no framework changes
- **Deployment:** Must remain deployable via `docker-compose up` with no additional setup steps
- **Database:** All schema changes require EF Core migrations; auto-applied on startup
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# 12 (implicit via .NET 8 SDK) — all backend logic, domain models, repositories, services, controllers
- HTML/Razor — server-side views in `EuphoriaInn.Service/Views/`
- CSS — custom stylesheets in `EuphoriaInn.Service/wwwroot/css/`
- JavaScript — vanilla JS in `EuphoriaInn.Service/wwwroot/js/site.js`
## Runtime
- .NET 8.0 (all five projects target `net8.0`)
- NuGet (dotnet restore)
- No lockfile committed (no `packages.lock.json` detected)
## Frameworks
- ASP.NET Core 8 MVC (`Microsoft.NET.Sdk.Web`) — web layer in `EuphoriaInn.Service/`
- ASP.NET Core Identity 8.0.11 — authentication and user management
- AutoMapper 14.0.0 — entity-to-model and model-to-viewmodel mapping
- xUnit 2.5.3 — test runner for both `EuphoriaInn.UnitTests` and `EuphoriaInn.IntegrationTests`
- FluentAssertions 8.8.0 — assertion library in both test projects
- NSubstitute 5.3.0 — mocking framework (unit tests only)
- Microsoft.AspNetCore.Mvc.Testing 8.0.11 — integration test host (`EuphoriaInn.IntegrationTests`)
- coverlet.collector 6.0.0 — code coverage collection
- .NET SDK 8.0 (dotnet CLI)
- Entity Framework Core Tools 9.0.6 (`Microsoft.EntityFrameworkCore.Tools`) — migration tooling
- Visual Studio solution format (`EuphoriaInn.sln`, VS version 18.2)
## Key Dependencies
- `Microsoft.EntityFrameworkCore` 9.0.6 — ORM base (`EuphoriaInn.Repository`)
- `Microsoft.EntityFrameworkCore.SqlServer` 9.0.6 — SQL Server provider (`EuphoriaInn.Repository`)
- `Microsoft.EntityFrameworkCore.Design` 9.0.6 — design-time tools for migrations (`EuphoriaInn.Repository`)
- `AutoMapper` 14.0.0 — cross-layer object mapping (`EuphoriaInn.Domain`, `EuphoriaInn.Service`)
- `System.Security.Cryptography.Xml` 8.0.3 — cryptographic support (`EuphoriaInn.Domain`)
- `Microsoft.EntityFrameworkCore.InMemory` 8.0.11 — in-memory EF provider (integration tests)
- `Microsoft.EntityFrameworkCore.Sqlite` 9.0.6 — SQLite provider for integration test database (`EuphoriaInn.IntegrationTests`)
- `Microsoft.Data.Sqlite` — SQLite connection used by `TestDatabase` helper
- `Microsoft.NET.Test.Sdk` 17.8.0 — test SDK
- `Microsoft.Extensions.Configuration.Binder` 9.0.6 — configuration binding (`EuphoriaInn.Domain`)
- `System.Net.Http` 4.3.4 — HTTP client (both test projects)
- `System.Text.RegularExpressions` 4.3.1 — regex support (both test projects)
## Frontend Libraries (CDN)
- Bootstrap 5.3.0 — CSS framework and JS components
- Font Awesome 6.4.0 — icon library
- jQuery 3.6.0 — DOM utilities
- Google Fonts (Cinzel) — D&D themed typography
- `EuphoriaInn.Service/wwwroot/css/site.css`
- `EuphoriaInn.Service/wwwroot/css/calendar.css`
- `EuphoriaInn.Service/wwwroot/css/quests.css`
- `EuphoriaInn.Service/wwwroot/css/shop.css`
- `EuphoriaInn.Service/wwwroot/css/guild-members.css`
## Database
- Microsoft SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`)
- EF Core code-first with explicit migrations in `EuphoriaInn.Repository/Migrations/`
- Auto-applied on startup via `context.Database.Migrate()` in `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs`
- `DbContext`: `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — extends `IdentityDbContext<UserEntity, IdentityRole<int>, int>`
- SQLite in-memory (`:memory:`) via persistent `SqliteConnection` — see `EuphoriaInn.IntegrationTests/Helpers/TestDatabase.cs`
- Schema initialised with `EnsureCreated()` per test run
## Configuration
- `EuphoriaInn.Service/appsettings.json` — base config (connection string, email settings, security config, logging)
- `EuphoriaInn.Service/appsettings.Development.json` — development overrides (logging levels only)
- Docker/production overrides via environment variables (double-underscore convention: `ConnectionStrings__DefaultConnection`)
- `Security:PasswordIterations` — PBKDF2 iteration count
- `Security:SaltSize` — salt byte length
- `Security:HashSize` — hash byte length
- Multi-stage Dockerfile at `Dockerfile` — base `mcr.microsoft.com/dotnet/aspnet:8.0`, build `mcr.microsoft.com/dotnet/sdk:8.0`
- `docker-compose.yml` — orchestrates `questboard` app + `sqlserver` containers
## Platform Requirements
- .NET 8 SDK
- SQL Server (runs on Windows host when developing in WSL)
- dotnet-ef global tool for migration management
- Docker + Docker Compose
- External Docker network `net-dnd`
- Published image: `ghcr.io/theunschut/dnd-quest-board:latest` (GitHub Container Registry)
- Port 7080 (host) → 8080 (container)
- Health check endpoint: `GET /health`
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- Domain model files: PascalCase, no suffix — `Quest.cs`, `User.cs`, `PlayerSignup.cs`
- Entity files: PascalCase with `Entity` suffix — `QuestEntity.cs`, `UserEntity.cs`, `CharacterEntity.cs`
- Repository files: PascalCase with `Repository` suffix — `QuestRepository.cs`, `BaseRepository.cs`
- Interface files: PascalCase with `I` prefix — `IQuestService.cs`, `IBaseRepository.cs`
- Service files: PascalCase with `Service` suffix — `QuestService.cs`, `BaseService.cs`
- ViewModel files: PascalCase with `ViewModel` suffix — `EditQuestViewModel.cs`, `RegisterViewModel.cs`
- Controller files: PascalCase with `Controller` suffix — `QuestController.cs`
- AutoMapper profile files: PascalCase with `Profile` suffix — `ViewModelProfile.cs`, `EntityProfile.cs`
- Authorization files: PascalCase with `Handler` or `Requirement` suffix — `DungeonMasterHandler.cs`, `AdminRequirement.cs`
- Layer directories: PascalCase project names — `EuphoriaInn.Domain`, `EuphoriaInn.Repository`, `EuphoriaInn.Service`
- Feature subdirectories within controllers: PascalCase by domain area — `Controllers/QuestBoard/`, `Controllers/Admin/`, `Controllers/Shop/`
- ViewModel directories: PascalCase with `ViewModels` suffix — `QuestViewModels/`, `AccountViewModels/`
- All classes: PascalCase
- Internal service implementations: `internal class QuestService`
- Abstract base classes: `abstract class BaseService<TModel, TEntity>`, `abstract class BaseRepository<T>`
- Interfaces: `IQuestService`, `IBaseRepository<T>`
- All async methods: PascalCase with `Async` suffix — `GetQuestWithDetailsAsync`, `FinalizeQuestAsync`, `AddAsync`
- Private helper methods: PascalCase — `IsSameDateTime`, `UpdateProposedDatesIntelligentlyAsync`
- Local variables and parameters: camelCase — `questId`, `currentUser`, `dmName`
- Private fields (from constructor injection): camelCase — not stored as fields; primary constructor parameters used directly
- Public properties: PascalCase — `IsFinalized`, `FinalizedDate`, `TotalPlayerCount`
- Model type parameter: `TModel`
- Entity type parameter: `TEntity`
- Generic type: `T` — e.g., `BaseRepository<T>`, `IBaseService<T>`
## Code Style
## Import Organization
- `EuphoriaInn.IntegrationTests/GlobalUsings.cs` — `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore`, etc.
- Unit test projects declare `<Using Include="Xunit" />` in the `.csproj`
## Error Handling
- `return NotFound();` — resource does not exist
- `return Forbid();` — user lacks permission
- `return BadRequest("message");` — invalid request state
- `return Challenge();` — unauthenticated user
## Comments
## Patterns Used in Controllers
- `"DungeonMasterOnly"` — requires DungeonMaster or Admin role
- `"AdminOnly"` — requires Admin role
## Patterns Used in Services
- Map entity → model: `Mapper.Map<Quest>(entity)` or `Mapper.Map<IList<Quest>>(entities)`
- Map model → entity: `mapper.Map<TEntity>(model)`
- Update existing entity from model: `Mapper.Map(model, entity)`
## Patterns Used in Repositories
## View Model and DTO Conventions
- `EntityProfile` (`EuphoriaInn.Domain/Automapper/EntityProfile.cs`) — maps between domain models and repository entities
- `ViewModelProfile` (`EuphoriaInn.Service/Automapper/ViewModelProfile.cs`) — maps between domain models and view models
- Enums stored as `int` in entities; cast explicitly in AutoMapper mappings: `opt => opt.MapFrom(src => (int)src.Type)`
## Common Abstractions and Base Classes
| Abstraction | Location | Purpose |
|---|---|---|
| `IModel` | `EuphoriaInn.Domain/Models/IModel.cs` | Marker interface requiring `int Id` on all domain models |
| `IEntity` | `EuphoriaInn.Repository/Entities/IEntity.cs` | Marker interface requiring `int Id` on all EF entities |
| `IBaseService<T>` | `EuphoriaInn.Domain/Interfaces/IBaseService.cs` | CRUD interface contract for all services |
| `BaseService<TModel, TEntity>` | `EuphoriaInn.Domain/Services/BaseService.cs` | Generic CRUD implementation delegating to repository |
| `IBaseRepository<T>` | `EuphoriaInn.Repository/Interfaces/IBaseRepository.cs` | CRUD interface contract for all repositories |
| `BaseRepository<T>` | `EuphoriaInn.Repository/BaseRepository.cs` | Generic CRUD implementation using EF `DbSet<T>` |
- `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` → `AddDomainServices()`
- `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` → `AddRepositoryServices()`
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Strict dependency direction: Service → Domain → Repository (Domain does NOT depend on Service; Repository does NOT depend on Domain directly — they share interfaces defined in Domain)
- All cross-layer communication goes through interfaces; concrete implementations are hidden (`internal class QuestService`, etc.)
- ASP.NET Core Identity is integrated at the Repository layer via `IdentityDbContext`; the Domain `UserService` wraps `UserManager<UserEntity>` and `SignInManager<UserEntity>`
- AutoMapper is used at two distinct boundaries: Entity↔DomainModel (in `EuphoriaInn.Domain/Automapper/EntityProfile.cs`) and DomainModel↔ViewModel (in `EuphoriaInn.Service/Automapper/ViewModelProfile.cs`)
- Every service and repository is registered as `Scoped` via extension methods; the DI container is the only wiring point
## Layers
- Purpose: Handle HTTP requests, render Razor views, enforce authorization, coordinate domain services
- Location: `EuphoriaInn.Service/`
- Contains: MVC Controllers, Razor Views, ViewModels, AutoMapper `ViewModelProfile`, Authorization handlers/requirements, `Program.cs`
- Depends on: `EuphoriaInn.Domain` (interfaces only)
- Used by: End users via browser
- Purpose: Business logic, domain models, service interfaces, AutoMapper entity profiles
- Location: `EuphoriaInn.Domain/`
- Contains: `Models/`, `Services/` (internal implementations), `Interfaces/` (public contracts), `Automapper/EntityProfile.cs`, `Enums/`, `Extensions/ServiceExtensions.cs`
- Depends on: `EuphoriaInn.Repository` (interfaces via `IBaseRepository<T>`, concrete entities for AutoMapper)
- Used by: `EuphoriaInn.Service`
- Purpose: Data persistence, Entity Framework Core, ASP.NET Core Identity store
- Location: `EuphoriaInn.Repository/`
- Contains: `Entities/` (EF entity classes + `QuestBoardContext`), `Interfaces/` (repository contracts), concrete repository implementations, `Migrations/`, `Extensions/ServiceExtensions.cs`
- Depends on: SQL Server via EF Core
- Used by: `EuphoriaInn.Domain`
## Data Flow
- Authentication state: ASP.NET Core Identity cookies; session (`IdleTimeout = 24h`) for supplemental state
- Database state: Managed entirely through EF Core change tracking; `SaveChangesAsync()` is called explicitly in services
- UI state: `ViewBag` for simple controller-to-view data; strongly-typed ViewModels for form binding
## Key Abstractions
- Purpose: Generic CRUD operations (Add, GetById, GetAll, Update, Remove, SaveChanges) common to all domain services
- Examples: `EuphoriaInn.Domain/Interfaces/IBaseService.cs`, `EuphoriaInn.Domain/Services/BaseService.cs`
- Pattern: Template Method — subclasses override specific methods (e.g., `QuestService.RemoveAsync` performs manual cascade cleanup before delegating)
- Purpose: Generic repository CRUD over EF Core entities
- Examples: `EuphoriaInn.Repository/Interfaces/IBaseRepository.cs`
- Pattern: Repository — concrete repos (`QuestRepository`, `UserRepository`) extend the base and add domain-specific queries (e.g., `GetQuestsWithDetailsAsync` with eager-loaded navigation properties)
- Purpose: Marker interface for all domain models, ensures `int Id` is present
- Location: `EuphoriaInn.Domain/Models/IModel.cs`
- Purpose: Marker interface for all EF Core entities
- Location: `EuphoriaInn.Repository/Entities/IEntity.cs`
- `EntityProfile` (`EuphoriaInn.Domain/Automapper/EntityProfile.cs`): Maps between `*Entity` ↔ domain `Model` classes; handles enum int↔enum conversions, password/security fields exclusions
- `ViewModelProfile` (`EuphoriaInn.Service/Automapper/ViewModelProfile.cs`): Maps between domain `Model` ↔ `*ViewModel`; registered in `Program.cs`
## Entry Points
- Location: `EuphoriaInn.Service/Program.cs`
- Triggers: ASP.NET Core host startup
- Responsibilities: Configure Identity, Authorization policies, Session, DI registrations (via `AddRepositoryServices()` + `AddDomainServices()`), AutoMapper, Kestrel limits; run migrations (`ConfigureDatabase()`); seed shop data; mount default MVC route
- Pattern: `{controller=Home}/{action=Index}/{id?}`
- All controllers are in subdirectories under `EuphoriaInn.Service/Controllers/` and use standard MVC conventions
## Error Handling
- Non-Development environments use the `/Error` exception handler page (`app.UseExceptionHandler("/Error")`)
- Controller actions return `NotFound()`, `BadRequest()`, `Challenge()`, or `Forbid()` where appropriate
- Services silently return `null` or early-exit when an entity is not found (e.g., `if (entity == null) return;`)
- `EmailService` catches all exceptions internally and logs a warning rather than propagating — email failure does not block the request
- Shop seed failures on startup are caught and logged without stopping the app
## Authorization
- `"DungeonMasterOnly"` — satisfied if user is in `DungeonMaster` OR `Admin` role; implemented in `EuphoriaInn.Service/Authorization/DungeonMasterHandler.cs`
- `"AdminOnly"` — satisfied if user is in `Admin` role; implemented in `EuphoriaInn.Service/Authorization/AdminHandler.cs`
```csharp
```
## Cross-Cutting Concerns
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
