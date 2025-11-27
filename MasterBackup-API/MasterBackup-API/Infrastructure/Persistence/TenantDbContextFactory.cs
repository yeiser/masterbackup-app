using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MasterBackup_API.Infrastructure.Persistence;

public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        // This connection string is only used for migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=tenant_template;Username=postgres;Password=postgres");

        return new TenantDbContext(optionsBuilder.Options);
    }
}
