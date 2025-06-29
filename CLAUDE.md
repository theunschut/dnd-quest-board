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
dotnet run --project QuestBoard.Service

# Restore packages for solution
dotnet restore
```

### From QuestBoard.Service Directory
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
dotnet ef migrations add MigrationName --project ../QuestBoard.Repository
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
- **QuestBoard.Domain**: Contains business models, enums, and core domain logic
- **QuestBoard.Repository**: Data access layer with Entity Framework Core, repositories, and AutoMapper profiles
- **QuestBoard.Service**: MVC web application with controllers, views, services, and view models

### Technology Stack
- **Backend**: ASP.NET Core 8 MVC with Repository Pattern
- **Database**: Microsoft SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript with D&D theming
- **Email**: .NET SMTP with Gmail integration
- **Mapping**: AutoMapper for entity-to-model mapping
- **Deployment**: Docker containerization

### Core Domain Models (QuestBoard.Domain)
- `Quest`: Main quest entity with title, description, difficulty, DM info, and scheduling
- `User`: User account management with authentication and profile information
- `ProposedDate`: Date options for each quest with voting capabilities
- `PlayerSignup`: Player registration for quests with user association
- `PlayerDateVote`: Player votes on proposed dates (Yes/No/Maybe)
- `Difficulty`: Enum for quest difficulty levels (Easy, Medium, Hard, Deadly)
- `VoteType`: Enum for player vote types (Yes, No, Maybe)

### Domain Services (QuestBoard.Domain/Services)
- `QuestService`: Business logic for quest management and coordination
- `UserService`: User account management and authentication logic
- `PlayerSignupService`: Player registration and quest participation logic
- `EmailService`: Email notification service with Gmail SMTP integration
- `BaseService`: Abstract base class providing common service functionality

### Domain Configuration
- `SecurityConfiguration`: Centralized security settings and authentication configuration
- `ServiceExtensions`: Dependency injection configuration for domain services

### Repository Layer (QuestBoard.Repository)
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

### Service Layer (QuestBoard.Service)
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
cd QuestBoard.Service
dotnet ef migrations add MigrationName --project ../QuestBoard.Repository

# Apply migrations to database
dotnet ef database update --project ../QuestBoard.Repository

# Remove the last migration (if needed)
dotnet ef migrations remove --project ../QuestBoard.Repository
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
   dotnet ef migrations remove --project ../QuestBoard.Repository
   
   # Create initial migration (from current schema)
   dotnet ef migrations add InitialCreate --project ../QuestBoard.Repository
   
   # Delete database and restart app to apply migrations cleanly
   ```
3. The `ConfigureDatabase()` method includes fallback handling for existing databases