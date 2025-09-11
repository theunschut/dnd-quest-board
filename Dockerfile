# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and all project files for proper dependency resolution
COPY ["EuphoriaInn.sln", "./"]
COPY ["EuphoriaInn.Domain/EuphoriaInn.Domain.csproj", "EuphoriaInn.Domain/"]
COPY ["EuphoriaInn.Repository/EuphoriaInn.Repository.csproj", "EuphoriaInn.Repository/"]
COPY ["EuphoriaInn.Service/EuphoriaInn.Service.csproj", "EuphoriaInn.Service/"]

# Restore packages for the entire solution
RUN dotnet restore "EuphoriaInn.sln"

# Copy all source code
COPY . .

# Build the entire solution
RUN dotnet build "EuphoriaInn.sln" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "EuphoriaInn.Service/EuphoriaInn.Service.csproj" -c Release -o /app/publish --no-build

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EuphoriaInn.Service.dll"]