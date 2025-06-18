# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a D&D Quest Board web application built with ASP.NET Core 8 MVC that allows DMs to create quests and players to sign up and vote on proposed dates. The application handles quest management, player coordination, and email notifications.

## Development Commands

### From Root Directory (using solution)
```bash
# Build the entire solution
dotnet build

# Run the application
dotnet run --project quest-board

# Restore packages for solution
dotnet restore
```

### From quest-board Directory
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

### Technology Stack
- **Backend**: ASP.NET Core 8 with Razor Pages
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript  
- **Email**: .NET SMTP with Gmail integration
- **Deployment**: Docker containerization

### Core Models
- `Quest`: Main quest entity with title, description, difficulty, DM info
- `ProposedDate`: Date options for each quest
- `PlayerSignup`: Player registration for quests
- `PlayerDateVote`: Player votes on proposed dates (Yes/No/Maybe)

### Key Pages & Functionality
- `/` - Main quest board displaying all available quests
- `/CreateQuest` - DM quest creation with multiple date options
- `/Quest/{id}` - Quest details and player signup with date voting
- `/ManageQuest/{id}` - DM interface for finalizing quests and selecting players
- `/MyQuests` - DM's personal quest management dashboard

### Database Context
- Uses `QuestBoardContext` with Entity Framework Core
- SQLite database stored as `quests.db` in the root directory
- Automatic database creation on application startup

### Email Service
- `IEmailService` interface with Gmail SMTP implementation
- Sends notifications to selected players when quests are finalized
- Configuration in `appsettings.json` EmailSettings section

### Security & Session Management
- Simple session-based authentication for DM verification
- No complex user management - relies on DM name matching
- Session storage for player signup tracking

## Configuration

### Required Settings
- `ConnectionStrings:DefaultConnection` - SQLite database path
- `EmailSettings:SmtpUsername` - Gmail account for sending emails
- `EmailSettings:SmtpPassword` - Gmail app-specific password
- `EmailSettings:FromEmail` - From email address

### Environment Variables (Docker)
- `SMTP_USERNAME`, `SMTP_PASSWORD`, `FROM_EMAIL` for email configuration
- Can be set in `.env` file for Docker Compose

## Development Notes

- Uses Bootstrap 5 for responsive UI with custom CSS for difficulty badges
- JavaScript handles dynamic form elements (adding/removing date options)
- Auto-refresh on quest detail pages every 30 seconds
- Color-coded difficulty system (Easy=green, Medium=yellow, Hard=red, Deadly=purple)
- First-come-first-served player selection with 6-player maximum
- Automatic date recommendation based on most "Yes" votes