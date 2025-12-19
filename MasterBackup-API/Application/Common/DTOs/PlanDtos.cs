namespace MasterBackup_API.Application.Common.DTOs;

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int MaxDatabases { get; set; }
    public int MaxUsers { get; set; }
    public long MaxStorageGB { get; set; }
    public int BackupRetentionDays { get; set; }
    public bool CloudStorageEnabled { get; set; }
    public bool ScheduledBackupsEnabled { get; set; }
    public bool ApiAccessEnabled { get; set; }
    public bool PrioritySupport { get; set; }
    public bool CustomBrandingEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public List<PlanFeatureDto> Features { get; set; } = new();
}

public class PlanFeatureDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsIncluded { get; set; }
    public string? Value { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreatePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int MaxDatabases { get; set; }
    public int MaxUsers { get; set; }
    public long MaxStorageGB { get; set; }
    public int BackupRetentionDays { get; set; }
    public bool CloudStorageEnabled { get; set; }
    public bool ScheduledBackupsEnabled { get; set; }
    public bool ApiAccessEnabled { get; set; }
    public bool PrioritySupport { get; set; }
    public bool CustomBrandingEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public List<string> Features { get; set; } = new();
}
