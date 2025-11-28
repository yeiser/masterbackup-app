namespace MasterBackup_API.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? ConnectionString { get; }
    void SetTenant(Guid tenantId, string connectionString);
    void Clear();
}
