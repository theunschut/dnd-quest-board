# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and all project files for proper dependency resolution
COPY ["QuestBoard.sln", "./"]
COPY ["QuestBoard.Domain/QuestBoard.Domain.csproj", "QuestBoard.Domain/"]
COPY ["QuestBoard.Repository/QuestBoard.Repository.csproj", "QuestBoard.Repository/"]
COPY ["QuestBoard.Service/QuestBoard.Service.csproj", "QuestBoard.Service/"]

# Restore packages for the entire solution
RUN dotnet restore "QuestBoard.sln"

# Copy all source code
COPY . .

# Build the entire solution
RUN dotnet build "QuestBoard.sln" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "QuestBoard.Service/QuestBoard.Service.csproj" -c Release -o /app/publish --no-build

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite database with proper permissions
RUN mkdir -p /app/data && chmod 755 /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "QuestBoard.Service.dll"]