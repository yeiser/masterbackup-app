using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Common.Interfaces;

public interface ITenantService
{
    Task<DbContextOptions<TenantDbContext>> GetTenantDbContextOptionsAsync(Guid tenantId);
    Task<DbContextOptions<TenantDbContext>> GetTenantDbContextOptionsBySubdomainAsync(string subdomain);
    Task<string> CreateTenantDatabaseAsync(Guid tenantId, string tenantName, string subdomain);
}
