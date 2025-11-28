using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Domain.Entities;

namespace MasterBackup_API.Infrastructure.Persistence;

public class MasterDbContext : IdentityDbContext<ApplicationUser>
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<ApplicationLog> Logs { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.Role).HasConversion<string>();
            entity.HasIndex(e => e.Email).IsUnique(); // Email must be globally unique
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.ApiKey).IsUnique();
        });

        modelBuilder.Entity<ApplicationLog>(entity =>
        {
            entity.ToTable("Logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TimeStamp).IsRequired();
            entity.HasIndex(e => e.TimeStamp);
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.InvitationToken).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });
    }
}
