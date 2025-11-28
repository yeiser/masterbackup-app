using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private Guid? _tenantId;
    private string? _connectionString;

    public Guid? TenantId => _tenantId;
    public string? ConnectionString => _connectionString;

    public void SetTenant(Guid tenantId, string connectionString)
    {
        _tenantId = tenantId;
        _connectionString = connectionString;
    }

    public void Clear()
    {
        _tenantId = null;
        _connectionString = null;
    }
}
