[![.NET CI](https://github.com/theunschut/quest-board/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/theunschut/quest-board/actions/workflows/dotnet.yml) [![Docker](https://github.com/theunschut/quest-board/actions/workflows/docker-publish.yml/badge.svg?branch=main)](https://github.com/theunschut/quest-board/actions/workflows/docker-publish.yml)

# D&D Quest Board

A comprehensive web application for managing D&D campaign quests with multiple DMs and players. Built with ASP.NET Core 8 MVC architecture following clean architecture principles with domain, repository, and service layers.

## Features

### Core Quest Management
- **DM Registration**: Dungeon Masters can register in the guild directory
- **Quest Creation**: DMs can create quests with multiple proposed dates and difficulty levels
- **Player Signup**: Players can sign up and vote on available dates (Yes/No/Maybe)
- **Quest Management**: DMs can review votes and finalize quest details
- **Email Notifications**: Automatic email notifications when quests are finalized

### User Management & Authentication
- **User Accounts**: Full user registration and authentication system
- **Session Management**: Secure session-based authentication for DMs
- **Authorization**: Role-based access control with Dungeon Master requirements

### Calendar & Scheduling
- **Calendar View**: Monthly calendar displaying all scheduled quests
- **Date Management**: Comprehensive date voting and scheduling system
- **Quest Timeline**: Visual representation of upcoming adventures

### User Experience
- **DM Directory**: Browse and contact registered Dungeon Masters
- **Responsive Design**: Bootstrap 5-based UI with D&D themed styling
- **Real-time Updates**: Auto-refresh on quest detail pages every 30 seconds
- **Quest Difficulty System**: Color-coded difficulty badges (Easy, Medium, Hard, Deadly)

## Quick Start

### Using Docker (Recommended)

1. Clone the repository
2. Configure your Gmail SMTP settings in environment variables or `.env` file
3. Run with Docker Compose from the root directory:

```bash
docker-compose up -d
```

The application will be available at `http://localhost:8080`

### Local Development

1. Install .NET 8 SDK
2. Configure email settings in `QuestBoard.Service/appsettings.json`
3. Run the application from root directory:

```bash
dotnet run --project QuestBoard.Service
```

### Using Solution File

From the root directory, you can also build and run using the solution:

```bash
dotnet build
dotnet run --project QuestBoard.Service
```

## Project Structure

The application follows a clean architecture pattern with three main projects:

- **QuestBoard.Domain**: Core business models, enums, services, and domain logic
- **QuestBoard.Repository**: Data access layer with Entity Framework Core, repositories, and entity configurations
- **QuestBoard.Service**: MVC web application with controllers, views, services, authorization, and view models

### Key Components

#### Domain Layer
- **Models**: Quest, User, PlayerSignup, ProposedDate, PlayerDateVote
- **Services**: QuestService, UserService, PlayerSignupService, EmailService
- **Configuration**: SecurityConfiguration for application security settings
- **Enums**: Difficulty levels (Easy, Medium, Hard, Deadly), VoteType (Yes, No, Maybe)

#### Repository Layer
- **Entities**: Database entity classes with EF Core configurations
- **Repositories**: Data access implementations with dependency injection
- **Context**: QuestBoardContext with SQLite database configuration

#### Service Layer
- **Controllers**: Home, Quest, DungeonMaster, Account, Calendar
- **Authorization**: Custom Dungeon Master authorization handlers and requirements
- **ViewModels**: Strongly-typed models for form binding and data transfer
- **Views**: Razor templates with Bootstrap 5 and D&D theming

## Configuration

### Email Settings

Configure Gmail SMTP in `QuestBoard.Service/appsettings.json` or environment variables:

```json
{
  "EmailSettings": {
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com"
  }
}
```

Or use environment variables:
- `SMTP_USERNAME`: Your Gmail address
- `SMTP_PASSWORD`: Gmail app-specific password
- `FROM_EMAIL`: Email address to send from

### Database

The application uses SQLite with Entity Framework Core. The database file (`quests.db`) will be created automatically in the root directory on first run.

## Usage

1. **DM Registration**: Visit the Dungeon Masters page to register as a new DM
2. **Create Quest**: Select a registered DM and create quest details with multiple date options
3. **Player Signup**: Players can view quests and sign up with date preferences (Yes/No/Maybe)
4. **Manage Quest**: DMs can review signups and voting, then finalize the quest
5. **My Quests**: DMs can manage all their created quests from a personal dashboard
6. **Email Notifications**: Selected players receive automatic email notifications

## Key Pages

### Public Pages
- `/` - Main quest board displaying all available quests
- `/Account/Login` - User authentication
- `/Account/Register` - New user registration
- `/DungeonMaster` - Browse registered Dungeon Masters and register as new DM

### Quest Management
- `/Quest/Create` - DM quest creation with multiple date options
- `/Quest/Details/{id}` - Quest details and player signup with date voting
- `/Quest/Manage/{id}` - DM interface for finalizing quests and selecting players
- `/Quest/MyQuests` - DM's personal quest management dashboard

### Calendar & Organization
- `/Calendar` - Monthly calendar view of all scheduled quests
- `/Account/Profile` - User profile management

## Docker Deployment

### Development
```bash
docker-compose up -d
```

### Production Notes

- Database is stored in `./quests.db` in the root directory and persisted via Docker volume
- Configure reverse proxy (nginx) for HTTPS and custom domain
- Backup the `quests.db` file regularly
- Set email configuration via environment variables in production

## Tech Stack

- **Backend**: ASP.NET Core 8 MVC with Repository Pattern
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript with D&D theming
- **Email**: .NET SMTP client with Gmail integration
- **Deployment**: Docker + Docker Compose
- **Architecture**: Domain-Repository-Service pattern with AutoMapper