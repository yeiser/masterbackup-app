#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER root
WORKDIR /app

# Install PostgreSQL client tools
RUN apt-get update && \
    apt-get install -y postgresql-client && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create backup directories
RUN mkdir -p /app/backups/temp && chmod -R 755 /app/backups

# Verify pg_dump installation
RUN pg_dump --version

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["MasterBackup-Worker.csproj", "."]
RUN dotnet restore "./MasterBackup-Worker.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "./MasterBackup-Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MasterBackup-Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables (can be overridden in docker-compose or launchSettings)
ENV ASPNETCORE_ENVIRONMENT=Development
ENV PATH="${PATH}:/usr/bin"

ENTRYPOINT ["dotnet", "MasterBackup-Worker.dll"]