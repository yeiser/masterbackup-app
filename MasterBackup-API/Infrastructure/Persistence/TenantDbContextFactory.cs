using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MasterBackup_API.Infrastructure.Persistence;

public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        // Use environment variable for tenant template database
        var connectionString = Environment.GetEnvironmentVariable("TENANT_TEMPLATE_CONNECTION") 
            ?? "Host=localhost;Database=tenant_template;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new TenantDbContext(optionsBuilder.Options);
    }
}
