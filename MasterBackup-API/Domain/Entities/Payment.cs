using System.ComponentModel.DataAnnotations;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    
    public Guid SubscriptionId { get; set; }
    
    public Guid TenantId { get; set; }
    
    public decimal Amount { get; set; }
    
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    public PaymentStatus Status { get; set; }
    
    public PaymentMethod Method { get; set; }
    
    [MaxLength(200)]
    public string? TransactionId { get; set; } // ID de pasarela de pago
    
    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    [MaxLength(500)]
    public string? FailureReason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Subscription Subscription { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
