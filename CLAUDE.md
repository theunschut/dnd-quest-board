# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a D&D Quest Board web application built with ASP.NET Core 8 MVC following a clean architecture pattern. The application allows DMs to create quests and players to sign up and vote on proposed dates. It handles quest management, player coordination, and email notifications through a layered architecture with domain, repository, and service layers.

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
```

### Docker Development (from root directory)
```bash
# Build and run with Docker Compose
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
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript with D&D theming
- **Email**: .NET SMTP with Gmail integration
- **Mapping**: AutoMapper for entity-to-model mapping
- **Deployment**: Docker containerization

### Core Domain Models (QuestBoard.Domain)
- `Quest`: Main quest entity with title, description, difficulty, DM info
- `DungeonMaster`: DM registration with name and optional email
- `ProposedDate`: Date options for each quest
- `PlayerSignup`: Player registration for quests
- `PlayerDateVote`: Player votes on proposed dates (Yes/No/Maybe)
- `Difficulty`: Enum for quest difficulty levels (Easy, Medium, Hard, Deadly)
- `VoteType`: Enum for player vote types (Yes, No, Maybe)

### Repository Layer (QuestBoard.Repository)
- `IQuestRepository`: Interface defining quest data operations
- `QuestRepository`: Implementation with Entity Framework Core
- `IDungeonMasterRepository`: Interface for DM management operations
- `DungeonMasterRepository`: Implementation for DM CRUD operations
- `QuestBoardContext`: Database context with entity configurations
- Entity classes with database-specific configurations
- AutoMapper profiles for entity-to-domain model mapping

### Service Layer (QuestBoard.Service)
- `HomeController`: Handles main quest board display
- `QuestController`: Manages quest CRUD operations and player interactions
- `DungeonMasterController`: Handles DM registration and directory
- `IEmailService` & `EmailService`: Gmail SMTP email notifications
- View models for form binding and data transfer
- Razor views with Bootstrap 5 styling

### Key Pages & Functionality
- `/` - Main quest board displaying all available quests
- `/DungeonMaster` - Browse registered Dungeon Masters with registration form sidebar
- `/Quest/Create` - DM quest creation with DM selection and multiple date options
- `/Quest/Details/{id}` - Quest details and player signup with date voting
- `/Quest/Manage/{id}` - DM interface for finalizing quests and selecting players
- `/Quest/MyQuests` - DM's personal quest management dashboard

### Database Context
- Uses `QuestBoardContext` with Entity Framework Core
- SQLite database stored as `quests.db` in the root directory
- Automatic database creation on application startup
- Entity configurations handle relationships and constraints

### Email Service
- `IEmailService` interface with Gmail SMTP implementation
- Sends notifications to selected players when quests are finalized
- Configuration in `appsettings.json` EmailSettings section
- Dependency injection for service registration

### Security & Session Management
- Simple session-based authentication for DM verification
- No complex user management - relies on DM name matching
- Session storage for player signup tracking

## Configuration

### Required Settings (appsettings.json)
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `EmailSettings:SmtpUsername` - Gmail account for sending emails
- `EmailSettings:SmtpPassword` - Gmail app-specific password
- `EmailSettings:FromEmail` - From email address

### Environment Variables (Docker)
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
- DM registration system with name and optional email
- Quest creation now requires selecting from registered DMs
- First-come-first-served player selection with 6-player maximum
- Automatic date recommendation based on most "Yes" votes
- Session-based DM authentication for quest management
- Email notifications sent only to selected players when quest is finalized

### Architecture Patterns
- Repository pattern with dependency injection
- AutoMapper for clean separation between entities and domain models
- MVC pattern with view models for form binding
- Service layer pattern for business logic (EmailService)
- Clean separation of concerns across three projects