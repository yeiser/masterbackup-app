using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Domain.Entities;

public class Plan
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // "free", "basic", "pro", "enterprise"
    
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty; // "Free", "Basic", "Professional", "Enterprise"
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public decimal MonthlyPrice { get; set; }
    
    public decimal YearlyPrice { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public int MaxDatabases { get; set; } // -1 para ilimitado
    
    public int MaxUsers { get; set; } // -1 para ilimitado
    
    public long MaxStorageGB { get; set; }
    
    public int BackupRetentionDays { get; set; }
    
    public bool CloudStorageEnabled { get; set; }
    
    public bool ScheduledBackupsEnabled { get; set; }
    
    public bool ApiAccessEnabled { get; set; }
    
    public bool PrioritySupport { get; set; }
    
    public bool CustomBrandingEnabled { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsFeatured { get; set; }
    
    [MaxLength(50)]
    public string? BadgeText { get; set; } // "Most Popular", "Best Value"
    
    [MaxLength(20)]
    public string? BadgeColor { get; set; } // "primary", "success", "warning"
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<PlanFeature> Features { get; set; } = new List<PlanFeature>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
