using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Domain.Entities;

namespace MasterBackup_API.Infrastructure.Persistence;

public class TenantDbContext : IdentityDbContext<ApplicationUser>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<UserInvitation> UserInvitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Role).HasConversion<string>();
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.InvitationToken).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });
    }
}
