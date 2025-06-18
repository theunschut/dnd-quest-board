# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["QuestBoard.Service/QuestBoard.csproj", "QuestBoard.Service/"]
RUN dotnet restore "QuestBoard.Service/QuestBoard.csproj"
COPY . .
RUN dotnet build "QuestBoard.Service/QuestBoard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuestBoard.Service/QuestBoard.csproj" -c Release -o /app/publish

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "QuestBoard.dll"]