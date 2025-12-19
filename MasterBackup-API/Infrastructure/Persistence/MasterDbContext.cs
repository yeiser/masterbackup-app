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
    public DbSet<Worker> Workers { get; set; }
    public DbSet<ApiKeyLog> ApiKeyLogs { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<PlanFeature> PlanFeatures { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Payment> Payments { get; set; }

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

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastHeartbeat);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Tags).HasColumnType("text[]");
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiKeyLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt });
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
            entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
            entity.Property(e => e.YearlyPrice).HasPrecision(10, 2);
        });

        modelBuilder.Entity<PlanFeature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlanId);
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Features)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.PlanId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.BillingCycle).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TransactionId);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Method).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.HasOne(e => e.Subscription)
                .WithMany(s => s.Payments)
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
