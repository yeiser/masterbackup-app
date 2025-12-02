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
    /// Publish a test connection message to the tenant's test-connection queue
    /// </summary>
    Task PublishTestConnectionAsync(Guid tenantId, object message);
    
    /// <summary>
    /// Create exchange and queues for a new tenant
    /// </summary>
    Task CreateTenantQueueAsync(Guid tenantId);
    
    /// <summary>
    /// Delete tenant queues and unbind from exchange
    /// </summary>
    Task DeleteTenantQueueAsync(Guid tenantId);
    
    /// <summary>
    /// Subscribe to backup result messages from workers
    /// </summary>
    Task SubscribeToBackupResultsAsync(Guid tenantId, Func<BackupJobResult, Task> onMessageReceived);
    
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
