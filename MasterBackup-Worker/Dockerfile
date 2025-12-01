# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MasterBackup-Worker.csproj", "./"]
RUN dotnet restore "MasterBackup-Worker.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "MasterBackup-Worker.csproj" -c Release -o /app/build
RUN dotnet publish "MasterBackup-Worker.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install PostgreSQL client tools (includes pg_dump, pg_restore, and other utilities)
# Also install common compression tools
RUN apt-get update && \
    apt-get install -y \
    postgresql-client \
    gzip \
    bzip2 \
    xz-utils \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Verify PostgreSQL tools are installed
RUN pg_dump --version && pg_restore --version

# Copy published app
COPY --from=build /app/publish .

# Copy appsettings (will be overridden by environment variables)
COPY appsettings.json .

# Create directory for temporary backup files
RUN mkdir -p /app/backups && chmod 777 /app/backups

# Set environment variables (these should be overridden at runtime)
ENV DOTNET_ENVIRONMENT=Production
ENV Worker__WorkerId=""
ENV Worker__TenantId=""
ENV Worker__ApiKey=""
ENV BACKUP_TEMP_PATH="/app/backups"

# Run as non-root user for security (optional but recommended)
# RUN useradd -m -u 1000 workeruser && chown -R workeruser:workeruser /app
# USER workeruser

ENTRYPOINT ["dotnet", "MasterBackup-Worker.dll"]
