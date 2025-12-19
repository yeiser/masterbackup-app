using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Domain.Entities;

public class PlanFeature
{
    public Guid Id { get; set; }
    
    public Guid PlanId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsIncluded { get; set; } = true;
    
    [MaxLength(100)]
    public string? Value { get; set; } // "Unlimited", "10 GB", "5 users", etc.
    
    public int DisplayOrder { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Plan Plan { get; set; } = null!;
}
