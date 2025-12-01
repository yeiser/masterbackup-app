using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Persistence;

public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    // Mock TenantContext for design-time operations
    private class DesignTimeTenantContext : ITenantContext
    {
        private readonly string _connectionString;

        public DesignTimeTenantContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Guid? TenantId => Guid.Empty;
        public string? ConnectionString => _connectionString;
        public void SetTenant(Guid tenantId, string connectionString) { }
        public void Clear() { }
    }

    public TenantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        // Use environment variable for tenant template database
        var connectionString = Environment.GetEnvironmentVariable("TENANT_TEMPLATE_CONNECTION") 
            ?? "Host=localhost;Database=tenant_template;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        var tenantContext = new DesignTimeTenantContext(connectionString);
        return new TenantDbContext(optionsBuilder.Options, tenantContext);
    }
}
