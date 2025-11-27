using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MasterBackup_API.Infrastructure.Persistence;

public class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();

        // This connection string is only used for migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=master_saas;Username=postgres;Password=postgres");

        return new MasterDbContext(optionsBuilder.Options);
    }
}
