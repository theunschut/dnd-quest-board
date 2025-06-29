#!/bin/bash
cd /mnt/c/Repos/quest-board/QuestBoard.Service

# Add EF tools locally to the project
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Create the initial migration
dotnet ef migrations add InitialSqlServerMigration --project ../QuestBoard.Repository --context QuestBoardContext

echo "Migration created successfully!"