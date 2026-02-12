# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only production project files (exclude test projects)
COPY ["EuphoriaInn.Domain/EuphoriaInn.Domain.csproj", "EuphoriaInn.Domain/"]
COPY ["EuphoriaInn.Repository/EuphoriaInn.Repository.csproj", "EuphoriaInn.Repository/"]
COPY ["EuphoriaInn.Service/EuphoriaInn.Service.csproj", "EuphoriaInn.Service/"]

# Restore packages with BuildKit cache mount for faster rebuilds
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "EuphoriaInn.Service/EuphoriaInn.Service.csproj"

# Copy source code for production projects only
COPY ["EuphoriaInn.Domain/", "EuphoriaInn.Domain/"]
COPY ["EuphoriaInn.Repository/", "EuphoriaInn.Repository/"]
COPY ["EuphoriaInn.Service/", "EuphoriaInn.Service/"]

# Build only the Service project (which transitively builds Domain and Repository)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build "EuphoriaInn.Service/EuphoriaInn.Service.csproj" -c Release --no-restore

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish "EuphoriaInn.Service/EuphoriaInn.Service.csproj" -c Release -o /app/publish --no-build

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EuphoriaInn.Service.dll"]