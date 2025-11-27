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
            throw new Exception("Tenant not found or inactive");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(tenant.ConnectionString);

        return optionsBuilder.Options;
    }

    public async Task<DbContextOptions<TenantDbContext>> GetTenantDbContextOptionsBySubdomainAsync(string subdomain)
    {
        var tenant = await _masterContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);

        if (tenant == null)
        {
            throw new Exception("Tenant not found or inactive");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(tenant.ConnectionString);

        return optionsBuilder.Options;
    }

    public async Task<string> CreateTenantDatabaseAsync(Guid tenantId, string tenantName, string subdomain)
    {
        try
        {
            var masterConnectionString = _configuration.GetConnectionString("MasterDatabase");
            var dbName = $"tenant_{subdomain.ToLower()}_{tenantId:N}";

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
            var tenantConnectionString = masterConnectionString?.Replace("Database=master", $"Database={dbName}");

            // Run migrations on new tenant database
            var tenantOptionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            tenantOptionsBuilder.UseNpgsql(tenantConnectionString);

            using (var tenantContext = new TenantDbContext(tenantOptionsBuilder.Options))
            {
                await tenantContext.Database.MigrateAsync();
            }

            _logger.LogInformation($"Created database for tenant: {tenantName} ({subdomain})");

            return tenantConnectionString ?? throw new Exception("Failed to create connection string");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating database for tenant: {tenantName}");
            throw;
        }
    }
}
