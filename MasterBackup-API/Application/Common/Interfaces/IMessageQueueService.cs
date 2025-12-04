using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Models;

namespace MasterBackup_API.Application.Common.Interfaces;

/// <summary>
/// Service for managing RabbitMQ message queue operations
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// Publish a backup job to the tenant's queue for worker processing
    /// </summary>
    Task PublishBackupJobAsync(Guid tenantId, BackupJobMessage message);
    
    /// <summary>
    /// Publish a test connection message to the tenant's unified queue with routing key
    /// </summary>
    Task PublishTestConnectionAsync(Guid tenantId, TestConnectionMessage message);
    
    /// <summary>
    /// Create exchange and queues for a new tenant
    /// </summary>
    Task CreateTenantQueueAsync(Guid tenantId);
    
    /// <summary>
    /// Check if RabbitMQ connection is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();
    
    /// <summary>
    /// Get queue statistics (message count, consumer count)
    /// </summary>
    Task<QueueStats> GetQueueStatsAsync(Guid tenantId);
}

/// <summary>
/// Statistics for a RabbitMQ queue
/// </summary>
public class QueueStats
{
    public uint MessageCount { get; set; }
    public uint ConsumerCount { get; set; }
    public string QueueName { get; set; } = string.Empty;
}
