using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Persistence;

/// <summary>
/// Context for tenant-specific business data.
/// Does NOT include Identity tables (Users, Roles) - those are in MasterDbContext.
/// Connection string is resolved dynamically per request via TenantMiddleware.
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Tenant-specific business entities
    public DbSet<DatabaseConnection> DatabaseConnections { get; set; }
    public DbSet<BackupSchedule> BackupSchedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDatabaseConnection(modelBuilder);
        ConfigureBackupSchedule(modelBuilder);
    }

    private void ConfigureDatabaseConnection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatabaseConnection>(entity =>
        {
            entity.ToTable("DatabaseConnections");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);
                
            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<int>();
                
            entity.Property(e => e.Host)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(e => e.Port)
                .IsRequired();
                
            entity.Property(e => e.Database)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.EncryptedPassword)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.EngineVersion)
                .HasMaxLength(50);

            entity.Property(e => e.SSLMode)
                .HasMaxLength(50);
                
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedBy)
                .IsRequired();
                
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.LastTestStatus)
                .HasMaxLength(1000);

            // Worker Assignment fields
            entity.Property(e => e.Tags)
                .HasColumnType("text[]");
                
            entity.Property(e => e.AssignmentMode)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(Domain.Enums.WorkerAssignmentMode.Auto);

            // Foreign key to Worker in MasterDb (cross-database reference)
            // Note: This is a logical relationship. The worker ID is stored but EF Core
            // cannot enforce referential integrity across databases.
            entity.Property(e => e.AssignedWorkerId)
                .IsRequired(false);
                
            // Índices
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedBy);
            
            // Relaciones
            entity.HasMany(e => e.BackupSchedules)
                .WithOne(bs => bs.DatabaseConnection)
                .HasForeignKey(bs => bs.DatabaseConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureBackupSchedule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupSchedule>(entity =>
        {
            entity.ToTable("BackupSchedules");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.DatabaseConnectionId)
                .IsRequired();

            entity.Property(e => e.CronExpression)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.RetentionDays)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Índices
            entity.HasIndex(e => e.DatabaseConnectionId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.NextRun);
        });
    }
}
