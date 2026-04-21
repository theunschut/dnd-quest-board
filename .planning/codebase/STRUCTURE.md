# Codebase Structure

**Analysis Date:** 2026-04-15

## Directory Layout

```
quest-board/
├── EuphoriaInn.Domain/          # Business logic, domain models, service interfaces
├── EuphoriaInn.Repository/      # EF Core data access, entities, migrations
├── EuphoriaInn.Service/         # ASP.NET Core MVC web app (entry point)
├── EuphoriaInn.UnitTests/       # xUnit unit tests for domain models and view models
├── EuphoriaInn.IntegrationTests/# Integration tests using WebApplicationFactory
├── EuphoriaInn.slnx             # Visual Studio solution file
├── Dockerfile                   # Container image for EuphoriaInn.Service
├── docker-compose.yml           # Compose config (app + SQL Server)
├── create-migration.sh          # Helper shell script for EF migrations
└── CLAUDE.md                    # Project instructions for AI assistants
```

## Directory Purposes

**EuphoriaInn.Domain:**
- Purpose: Framework-free business logic layer; defines what the application does
- Contains: Domain models, service interfaces (`Interfaces/`), internal service implementations (`Services/`), AutoMapper entity profile (`Automapper/`), enums (`Enums/`), DI extension (`Extensions/ServiceExtensions.cs`)
- Key files:
  - `EuphoriaInn.Domain/Models/IModel.cs` — marker interface for all domain models
  - `EuphoriaInn.Domain/Models/QuestBoard/Quest.cs` — core quest aggregate
  - `EuphoriaInn.Domain/Models/User.cs` — user domain model
  - `EuphoriaInn.Domain/Models/Character.cs` — character + multi-class model
  - `EuphoriaInn.Domain/Models/Shop/ShopItem.cs` — shop item domain model
  - `EuphoriaInn.Domain/Services/BaseService.cs` — generic CRUD base service
  - `EuphoriaInn.Domain/Services/QuestService.cs` — quest business logic
  - `EuphoriaInn.Domain/Services/UserService.cs` — user + Identity wrapper
  - `EuphoriaInn.Domain/Services/EmailService.cs` — SMTP email notifications
  - `EuphoriaInn.Domain/Automapper/EntityProfile.cs` — Entity ↔ DomainModel maps
  - `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs` — DI registrations for domain

**EuphoriaInn.Repository:**
- Purpose: All database concerns; EF Core entities, context, repositories, migrations
- Contains: EF entity classes (`Entities/`), repository interfaces (`Interfaces/`), repository implementations (root of project), migrations (`Migrations/`), DI extension (`Extensions/ServiceExtensions.cs`)
- Key files:
  - `EuphoriaInn.Repository/Entities/QuestBoardContext.cs` — EF `IdentityDbContext`; all `DbSet<>` registrations and `OnModelCreating` relationship configuration
  - `EuphoriaInn.Repository/Entities/UserEntity.cs` — extends `IdentityUser<int>`
  - `EuphoriaInn.Repository/Interfaces/IBaseRepository.cs` — generic CRUD contract
  - `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs` — registers DbContext + repositories; `ConfigureDatabase()` runs migrations

**EuphoriaInn.Service:**
- Purpose: The runnable web application; HTTP layer only
- Contains: `Program.cs`, controllers, Razor views, ViewModels, AutoMapper ViewModel profile, authorization handlers, static assets
- Subdirectories detailed below

**EuphoriaInn.UnitTests:**
- Purpose: Fast isolated tests for domain model logic and ViewModel validation
- Contains: `Models/`, `ViewModels/`, `Helpers/`

**EuphoriaInn.IntegrationTests:**
- Purpose: Full-stack HTTP tests using `WebApplicationFactory<Program>`
- Contains: `Controllers/` (one file per controller under test), `Helpers/`

## EuphoriaInn.Service Subdirectory Layout

```
EuphoriaInn.Service/
├── Program.cs                          # Application entry point and DI composition root
├── Authorization/
│   ├── DungeonMasterHandler.cs         # IAuthorizationHandler for "DungeonMasterOnly" policy
│   ├── DungeonMasterRequirement.cs
│   ├── AdminHandler.cs
│   └── AdminRequirement.cs
├── Automapper/
│   └── ViewModelProfile.cs             # DomainModel ↔ ViewModel AutoMapper profile
├── Controllers/
│   ├── Admin/
│   │   ├── AccountController.cs        # Login, Register, Profile, Logout
│   │   └── AdminController.cs          # User management, quest management (AdminOnly)
│   ├── Characters/
│   │   └── GuildMembersController.cs   # Guild/character directory
│   ├── QuestBoard/
│   │   ├── HomeController.cs           # Main quest board (/)
│   │   ├── QuestController.cs          # Quest CRUD and player actions
│   │   ├── QuestLogController.cs       # Completed quest log
│   │   ├── CalendarController.cs       # Monthly calendar view
│   │   └── PlayersController.cs        # Player/DM directory
│   └── Shop/
│       ├── ShopController.cs           # Player-facing shop (Authorize)
│       └── ShopManagementController.cs # DM shop management (DungeonMasterOnly)
├── ViewModels/
│   ├── AccountViewModels/              # LoginViewModel, RegisterViewModel, ProfileViewModel, etc.
│   ├── AdminViewModels/                # UserManagementViewModel
│   ├── CalendarViewModels/             # CalendarViewModel, CalendarDay, QuestOnDay
│   ├── CharacterViewModels/            # CharacterViewModel, CharacterClassViewModel
│   ├── GuildMembersViewModels/         # GuildMembersIndexViewModel
│   ├── QuestLogViewModels/             # QuestLogIndexViewModel
│   ├── QuestViewModels/                # QuestViewModel, EditQuestViewModel, MyQuestsViewModel, QuestSectionViewModel
│   └── ShopViewModels/                 # ShopIndexViewModel, ShopItemViewModel, CreateShopItemViewModel, etc.
├── Views/
│   ├── Account/                        # Login, Register, Profile views
│   ├── Admin/                          # User management, quest management views
│   ├── Calendar/                       # Calendar index view
│   ├── GuildMembers/                   # Guild member listing view
│   ├── Home/                           # Quest board index view
│   ├── Players/                        # Players index view
│   ├── Quest/                          # Create, Edit, Details, Manage, MyQuests views
│   ├── QuestLog/                       # Quest log index and details views
│   ├── Shared/
│   │   ├── _Layout.cshtml              # Global layout with navbar, footer, CSS/JS includes
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── Shop/                           # Shop index, details, purchase views
│   ├── ShopManagement/                 # DM shop management views
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
└── wwwroot/
    ├── css/                            # site.css, calendar.css, quests.css, shop.css, guild-members.css
    ├── js/                             # site.js and feature-specific scripts
    └── images/                         # D&D themed poster images, wax seals, blanks
```

## Key File Locations

**Entry Points:**
- `EuphoriaInn.Service/Program.cs`: Application host builder, all DI configuration, middleware pipeline, migrations trigger, shop seed

**Configuration:**
- `EuphoriaInn.Service/appsettings.json`: Connection string, EmailSettings (SMTP)
- `EuphoriaInn.Service/appsettings.Development.json`: Development overrides
- `docker-compose.yml`: Docker environment variables for SQL Server SA password and SMTP settings

**Core Logic:**
- `EuphoriaInn.Domain/Services/QuestService.cs`: Quest business logic including finalization, date management, cascade cleanup
- `EuphoriaInn.Domain/Services/UserService.cs`: User CRUD, Identity role management, authentication wrapper
- `EuphoriaInn.Domain/Services/PlayerSignupService.cs`: Player registration logic
- `EuphoriaInn.Domain/Services/ShopService.cs`: Shop item and transaction logic
- `EuphoriaInn.Domain/Services/CharacterService.cs`: Character management

**Database:**
- `EuphoriaInn.Repository/Entities/QuestBoardContext.cs`: All DbSets, FK constraints, cascade rules
- `EuphoriaInn.Repository/Migrations/`: EF Core migration history

**Testing:**
- `EuphoriaInn.UnitTests/`: Unit test project
- `EuphoriaInn.IntegrationTests/`: Integration test project; `Program` partial class in `Program.cs` makes it accessible to `WebApplicationFactory`

## Naming Conventions

**Files:**
- Domain models: `PascalCase.cs` matching the class name — `Quest.cs`, `PlayerSignup.cs`
- EF entities: Suffix `Entity` — `QuestEntity.cs`, `UserEntity.cs`
- Repository interfaces: Prefix `I`, suffix `Repository` — `IQuestRepository.cs`
- Service interfaces: Prefix `I`, suffix `Service` — `IQuestService.cs`
- ViewModels: Suffix `ViewModel` — `QuestViewModel.cs`, `LoginViewModel.cs`
- Controllers: Suffix `Controller` — `QuestController.cs`
- Authorization: Suffix `Handler` or `Requirement` — `DungeonMasterHandler.cs`

**Namespaces:**
- Follow project + folder path: `EuphoriaInn.Domain.Models.QuestBoard`, `EuphoriaInn.Service.Controllers.QuestBoard`

**Directories:**
- Feature grouping for controllers and views: `QuestBoard/`, `Admin/`, `Shop/`, `Characters/`
- Type grouping for ViewModels: `QuestViewModels/`, `AccountViewModels/`

## How Features Are Organized

Controllers, views, and ViewModels are organized **by feature group** (QuestBoard, Admin, Shop, Characters), but domain models and services are organized **by type** (all in `Models/`, all in `Services/`). The repository entities are all flat in `Entities/`.

New bounded contexts (e.g., Shop) get:
- Domain models in `EuphoriaInn.Domain/Models/Shop/`
- Service interface in `EuphoriaInn.Domain/Interfaces/IShopService.cs`
- Service implementation in `EuphoriaInn.Domain/Services/ShopService.cs`
- Entity in `EuphoriaInn.Repository/Entities/ShopItemEntity.cs`
- Repository interface in `EuphoriaInn.Repository/Interfaces/IShopRepository.cs`
- Repository implementation at root of `EuphoriaInn.Repository/`
- Controller in `EuphoriaInn.Service/Controllers/Shop/ShopController.cs`
- ViewModels in `EuphoriaInn.Service/ViewModels/ShopViewModels/`
- Views in `EuphoriaInn.Service/Views/Shop/`

## Where to Add New Code

**New Feature (e.g., new page/section):**
- Domain model: `EuphoriaInn.Domain/Models/{FeatureName}.cs`
- Service interface: `EuphoriaInn.Domain/Interfaces/I{Feature}Service.cs`
- Service implementation: `EuphoriaInn.Domain/Services/{Feature}Service.cs`
- Register service in: `EuphoriaInn.Domain/Extensions/ServiceExtensions.cs`
- EF entity: `EuphoriaInn.Repository/Entities/{Feature}Entity.cs`
- DbSet + relationships: `EuphoriaInn.Repository/Entities/QuestBoardContext.cs`
- Repository interface: `EuphoriaInn.Repository/Interfaces/I{Feature}Repository.cs`
- Repository implementation: `EuphoriaInn.Repository/{Feature}Repository.cs`
- Register repository in: `EuphoriaInn.Repository/Extensions/ServiceExtensions.cs`
- Create EF migration: run `dotnet ef migrations add {MigrationName} --project ../EuphoriaInn.Repository` from `EuphoriaInn.Service/`
- Controller: `EuphoriaInn.Service/Controllers/{Group}/{Feature}Controller.cs`
- ViewModels: `EuphoriaInn.Service/ViewModels/{Feature}ViewModels/`
- Views: `EuphoriaInn.Service/Views/{Feature}/`
- AutoMapper: add Entity↔Model map in `EuphoriaInn.Domain/Automapper/EntityProfile.cs`; add Model↔ViewModel map in `EuphoriaInn.Service/Automapper/ViewModelProfile.cs`

**New Controller Action on Existing Controller:**
- Add method to existing controller file
- Add corresponding Razor view in the matching `Views/{ControllerName}/` directory
- Add ViewModel class in matching `ViewModels/{Feature}ViewModels/` directory if needed

**Shared Utilities:**
- No dedicated `Helpers/` directory in production code; utility logic lives in the service it belongs to

## Special Directories

**EuphoriaInn.Repository/Migrations/:**
- Purpose: EF Core migration snapshots; one `.cs` file per migration plus a `.Designer.cs` file
- Generated: Yes (by `dotnet ef migrations add`)
- Committed: Yes

**EuphoriaInn.Service/wwwroot/:**
- Purpose: Static web assets served directly by the web server
- Generated: No (hand-authored)
- Committed: Yes
- Subfolders: `css/` (custom stylesheets per feature), `js/` (custom scripts), `images/` (D&D themed art assets)

**.planning/:**
- Purpose: GSD planning documents — codebase analysis, phase plans
- Generated: By GSD tooling
- Committed: Yes

---

*Structure analysis: 2026-04-15*
