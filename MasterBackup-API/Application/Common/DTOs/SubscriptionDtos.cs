namespace MasterBackup_API.Application.Common.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanDisplayName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool AutoRenew { get; set; }
    public DateTime? CanceledAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public int DaysRemaining { get; set; }
    public PlanLimitsDto Limits { get; set; } = new();
    public CurrentUsageDto Usage { get; set; } = new();
}

public class PlanLimitsDto
{
    public int MaxDatabases { get; set; }
    public int MaxUsers { get; set; }
    public long MaxStorageGB { get; set; }
    public int BackupRetentionDays { get; set; }
    public bool CloudStorageEnabled { get; set; }
    public bool ScheduledBackupsEnabled { get; set; }
    public bool ApiAccessEnabled { get; set; }
    public bool PrioritySupport { get; set; }
    public bool CustomBrandingEnabled { get; set; }
}

public class CurrentUsageDto
{
    public int DatabasesCount { get; set; }
    public int UsersCount { get; set; }
    public decimal StorageUsedGB { get; set; }
    public int StoragePercentage { get; set; }
}

public class CreateSubscriptionDto
{
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly"; // Monthly o Yearly
    public bool UseTrial { get; set; }
}

public class UpgradeSubscriptionDto
{
    public Guid NewPlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
}

public class CancelSubscriptionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
