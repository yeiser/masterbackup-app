using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly MasterDbContext _masterContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        MasterDbContext masterContext,
        IConfiguration configuration,
        ILogger<TenantService> logger)
    {
        _masterContext = masterContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DbContextOptions<TenantDbContext>> GetTenantDbContextOptionsAsync(Guid tenantId)
    {
        var tenant = await _masterContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);

        if (tenant == null)
        {
            throw new Exception("Tenant no encontrado o inactivo");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(tenant.ConnectionString);

        return optionsBuilder.Options;
    }

    public async Task<string> CreateTenantDatabaseAsync(Guid tenantId, string tenantName)
    {
        try
        {
            // Get master connection string from environment variable or appsettings
            var masterConnectionString = Environment.GetEnvironmentVariable("MASTER_DATABASE_CONNECTION") 
                ?? _configuration.GetConnectionString("MasterDatabase");

            if (string.IsNullOrEmpty(masterConnectionString))
            {
                throw new InvalidOperationException("Master database connection string is not configured. Set MASTER_DATABASE_CONNECTION environment variable or ConnectionStrings:MasterDatabase in appsettings.");
            }

            var dbName = $"tenant_{tenantId:N}";

            _logger.LogInformation($"Creating database '{dbName}' for tenant: {tenantName}");

            // Create new database
            var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
            optionsBuilder.UseNpgsql(masterConnectionString);

            using (var context = new DbContext(optionsBuilder.Options))
            {
                #pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync($"CREATE DATABASE \"{dbName}\"");
                #pragma warning restore EF1002
            }

            // Build connection string for new tenant database
            // Extract components from master connection string
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(masterConnectionString);
            builder.Database = dbName;
            var tenantConnectionString = builder.ConnectionString;

            _logger.LogInformation($"Tenant connection string created: Database={dbName}");

            // Run migrations on new tenant database
            var tenantOptionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            tenantOptionsBuilder.UseNpgsql(tenantConnectionString);

            using (var tenantContext = new TenantDbContext(tenantOptionsBuilder.Options))
            {
                await tenantContext.Database.MigrateAsync();
            }

            _logger.LogInformation($"Successfully created and migrated database for tenant: {tenantName}");

            return tenantConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating database for tenant: {tenantName}");
            throw;
        }
    }
}
