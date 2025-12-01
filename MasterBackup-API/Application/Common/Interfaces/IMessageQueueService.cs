using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Interfaces;

public interface IMessageQueueService
{
    Task PublishTestConnectionJob(Guid tenantId, TestConnectionMessage message);
    void CreateTenantQueue(Guid tenantId);
    void DeleteTenantQueue(Guid tenantId);
}
