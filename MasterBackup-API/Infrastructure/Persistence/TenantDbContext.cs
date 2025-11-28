using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Infrastructure.Persistence;

/// <summary>
/// Context for tenant-specific business data.
/// Does NOT include Identity tables (Users, Roles) - those are in MasterDbContext.
/// Connection string is resolved dynamically per request via TenantMiddleware.
/// </summary>
public class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    // Add tenant-specific business entities here
    // Example: public DbSet<Order> Orders { get; set; }
    // Example: public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure tenant-specific entities here
    }
}
