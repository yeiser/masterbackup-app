using System.ComponentModel.DataAnnotations;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    
    public Guid TenantId { get; set; }
    
    public Guid PlanId { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public SubscriptionStatus Status { get; set; }
    
    public BillingCycle BillingCycle { get; set; }
    
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public bool AutoRenew { get; set; } = true;
    
    public DateTime? CanceledAt { get; set; }
    
    [MaxLength(500)]
    public string? CancellationReason { get; set; }
    
    public DateTime? TrialEndDate { get; set; }
    
    public bool IsTrialUsed { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
