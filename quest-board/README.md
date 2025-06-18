# D&D Quest Board

A web application for managing D&D campaign quests with multiple DMs and players.

## Features

- **Quest Creation**: DMs can create quests with multiple proposed dates
- **Player Signup**: Players can sign up and vote on available dates
- **Quest Management**: DMs can review votes and finalize quest details
- **Email Notifications**: Automatic email notifications when quests are finalized
- **Responsive Design**: Bootstrap-based UI that works on all devices

## Quick Start

### Using Docker (Recommended)

1. Clone the repository
2. Copy `.env.example` to `.env` and configure your Gmail SMTP settings (these files are in the root directory)
3. Run with Docker Compose from the root directory:

```bash
docker-compose up -d
```

The application will be available at `http://localhost:8080`

### Local Development

1. Install .NET 8 SDK
2. Navigate to the quest-board directory: `cd quest-board`
3. Configure email settings in `appsettings.json`
4. Run the application:

```bash
dotnet run
```

### Using Solution File

From the root directory, you can also build and run using the solution:

```bash
dotnet build
dotnet run --project quest-board
```

## Configuration

### Email Settings

To enable email notifications, configure Gmail SMTP in your environment variables or `appsettings.json`:

- `SMTP_USERNAME`: Your Gmail address
- `SMTP_PASSWORD`: Gmail app-specific password
- `FROM_EMAIL`: Email address to send from

### Database

The application uses SQLite by default. The database file will be created automatically on first run.

## Usage

1. **Create Quest**: DMs enter their name and quest details with multiple date options
2. **Player Signup**: Players can view quests and sign up with date preferences
3. **Manage Quest**: DMs can review signups and voting, then finalize the quest
4. **Email Notifications**: Selected players receive automatic email notifications

## Docker Deployment

### Raspberry Pi 5

1. Copy the project to your Pi
2. Configure environment variables in `.env` (in the root directory)
3. Run from the root directory: `docker-compose up -d`
4. Access via `http://your-pi-ip:8080`

### Production Notes

- Database is stored in `./quests.db` in the root directory and persisted via Docker volume
- Configure reverse proxy (nginx) for HTTPS and custom domain
- Backup the `quests.db` file regularly

## Tech Stack

- **Backend**: ASP.NET Core 8 with Razor Pages
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5 + vanilla JavaScript
- **Email**: .NET SMTP client with Gmail
- **Deployment**: Docker + Docker Compose