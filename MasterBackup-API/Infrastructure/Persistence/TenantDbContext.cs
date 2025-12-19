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
    public DbSet<BackupHistory> BackupHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore ApplicationUser - it belongs to MasterDbContext only
        modelBuilder.Ignore<ApplicationUser>();

        ConfigureDatabaseConnection(modelBuilder);
        ConfigureBackupSchedule(modelBuilder);
        ConfigureBackupHistory(modelBuilder);
        ConfigureNotification(modelBuilder);
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
            
            // ============================================
            // IDENTIFICACIÓN Y RELACIONES
            // ============================================
            
            entity.Property(e => e.TenantId)
                .IsRequired();
            
            entity.Property(e => e.DatabaseConnectionId)
                .IsRequired();
            
            // ============================================
            // CONFIGURACIÓN DE PROGRAMACIÓN
            // ============================================
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Description)
                .HasMaxLength(500);
            
            entity.Property(e => e.CronExpression)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.TimeZone)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("UTC");
            
            // ============================================
            // RETENCIÓN
            // ============================================
            
            entity.Property(e => e.RetentionDays)
                .IsRequired();
            
            // ============================================
            // ESTADO Y SEGUIMIENTO
            // ============================================
            
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.NextRun)
                .IsRequired(false);
            
            entity.Property(e => e.LastRun)
                .IsRequired(false);
            
            entity.Property(e => e.LastExecutionStatus)
                .IsRequired(false)
                .HasConversion<int?>();
            
            entity.Property(e => e.LastExecutionError)
                .HasMaxLength(2000);
            
            // ============================================
            // OPCIONES AVANZADAS
            // ============================================
            
            entity.Property(e => e.MaxRetries)
                .IsRequired()
                .HasDefaultValue(3);
            
            entity.Property(e => e.TimeoutMinutes)
                .IsRequired()
                .HasDefaultValue(30);
            
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasDefaultValue(5);
            
            entity.Property(e => e.NotifyOnCompletion)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.NotifyOnlyOnFailure)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.AutoRestore)
                .IsRequired()
                .HasDefaultValue(false);
            
            // ============================================
            // AUDITORÍA
            // ============================================
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.CreatedBy)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);
            
            entity.Property(e => e.UpdatedBy)
                .IsRequired(false);
            
            // ============================================
            // ÍNDICES PARA PERFORMANCE
            // ============================================
            
            // Índice compuesto para queries por tenant y estado
            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("IX_BackupSchedules_TenantId_IsActive");
            
            // Índice para búsqueda por conexión
            entity.HasIndex(e => e.DatabaseConnectionId)
                .HasDatabaseName("IX_BackupSchedules_DatabaseConnectionId");
            
            // Índice para próximas ejecuciones (solo activos)
            entity.HasIndex(e => e.NextRun)
                .HasDatabaseName("IX_BackupSchedules_NextRun")
                .HasFilter("[NextRun] IS NOT NULL AND [IsActive] = 1");
            
            // Índice para queries por usuario creador
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_BackupSchedules_CreatedBy");
            
            // ============================================
            // RELACIONES
            // ============================================
            
            // Relación con DatabaseConnection
            entity.HasOne(e => e.DatabaseConnection)
                .WithMany(d => d.BackupSchedules)
                .HasForeignKey(e => e.DatabaseConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Nota: BackupExecutions se configurará cuando se cree esa entidad
        });
    }

    private void ConfigureBackupHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupHistory>(entity =>
        {
            entity.ToTable("BackupHistories");
            entity.HasKey(e => e.Id);
            
            // Properties
            entity.Property(e => e.JobId)
                .IsRequired();
            
            entity.Property(e => e.DatabaseConnectionId)
                .IsRequired();
            
            entity.Property(e => e.BackupScheduleId)
                .IsRequired(false); // Null for instant backups
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.StartTime)
                .IsRequired();
            
            entity.Property(e => e.EndTime)
                .IsRequired(false);
            
            entity.Property(e => e.Duration)
                .IsRequired(false);
            
            entity.Property(e => e.BlobUrl)
                .HasMaxLength(2000);
            
            entity.Property(e => e.BlobName)
                .HasMaxLength(500);
            
            entity.Property(e => e.BackupSizeBytes)
                .IsRequired(false);
            
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);
            
            entity.Property(e => e.ErrorCode)
                .HasMaxLength(100);
            
            entity.Property(e => e.StackTrace)
                .HasMaxLength(4000);
            
            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);
            
            entity.Property(e => e.CompressionType)
                .HasMaxLength(50);
            
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
            
            entity.Property(e => e.IsInstantBackup)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Relationships
            entity.HasOne(e => e.BackupSchedule)
                .WithMany()
                .HasForeignKey(e => e.BackupScheduleId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.DatabaseConnection)
                .WithMany()
                .HasForeignKey(e => e.DatabaseConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.JobId)
                .IsUnique()
                .HasDatabaseName("IX_BackupHistories_JobId");
            
            entity.HasIndex(e => e.BackupScheduleId)
                .HasDatabaseName("IX_BackupHistories_BackupScheduleId");
            
            entity.HasIndex(e => e.DatabaseConnectionId)
                .HasDatabaseName("IX_BackupHistories_DatabaseConnectionId");
            
            entity.HasIndex(e => new { e.Status, e.StartTime })
                .HasDatabaseName("IX_BackupHistories_Status_StartTime");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_BackupHistories_CreatedAt");
            
            entity.HasIndex(e => e.IsInstantBackup)
                .HasDatabaseName("IX_BackupHistories_IsInstantBackup");
        });
    }

    private void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id");
            
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .IsRequired();
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired()
                .HasMaxLength(450);
            
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Message)
                .HasColumnName("message")
                .IsRequired()
                .HasMaxLength(1000);
            
            entity.Property(e => e.RedirectUrl)
                .HasColumnName("redirect_url")
                .HasMaxLength(500);
            
            entity.Property(e => e.RelatedEntityId)
                .HasColumnName("related_entity_id");
            
            entity.Property(e => e.RelatedEntityType)
                .HasColumnName("related_entity_type")
                .HasMaxLength(100);
            
            entity.Property(e => e.IsRead)
                .HasColumnName("is_read")
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.ReadAt)
                .HasColumnName("read_at");
            
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata");
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            
            // Índices
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_notifications_user_id");
            
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_notifications_tenant_id");
            
            entity.HasIndex(e => new { e.UserId, e.IsRead })
                .HasDatabaseName("IX_notifications_user_id_is_read");
            
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_notifications_created_at");
            
            // NO hay foreign key a ApplicationUser porque los usuarios están en MasterDb
            // UserId es un string que hace referencia lógica pero sin constraint de FK
        });
    }
}
