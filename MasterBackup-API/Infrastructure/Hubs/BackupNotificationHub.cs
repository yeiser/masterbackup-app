using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MasterBackup_API.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time backup notifications
/// Clients connect and are automatically added to their tenant group
/// </summary>
public class BackupNotificationHub : Hub
{
    private readonly ILogger<BackupNotificationHub> _logger;

    public BackupNotificationHub(ILogger<BackupNotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Automatically adds the client to their tenant group
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        if (tenantId != Guid.Empty)
        {
            // Add connection to tenant group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            
            _logger.LogInformation(
                "Client connected to BackupNotificationHub. ConnectionId: {ConnectionId}, UserId: {UserId}, TenantId: {TenantId}",
                Context.ConnectionId, userId, tenantId);
        }
        else
        {
            _logger.LogWarning(
                "Client connected without tenant information. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        if (tenantId != Guid.Empty)
        {
            // Remove connection from tenant group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            
            _logger.LogInformation(
                "Client disconnected from BackupNotificationHub. ConnectionId: {ConnectionId}, UserId: {UserId}, TenantId: {TenantId}",
                Context.ConnectionId, userId, tenantId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client method to subscribe to specific backup schedule notifications
    /// </summary>
    public async Task SubscribeToSchedule(Guid scheduleId)
    {
        var tenantId = GetTenantId();
        var groupName = $"schedule_{scheduleId}";
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation(
            "Client subscribed to schedule notifications. ConnectionId: {ConnectionId}, ScheduleId: {ScheduleId}, TenantId: {TenantId}",
            Context.ConnectionId, scheduleId, tenantId);
    }

    /// <summary>
    /// Client method to unsubscribe from specific backup schedule notifications
    /// </summary>
    public async Task UnsubscribeFromSchedule(Guid scheduleId)
    {
        var tenantId = GetTenantId();
        var groupName = $"schedule_{scheduleId}";
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation(
            "Client unsubscribed from schedule notifications. ConnectionId: {ConnectionId}, ScheduleId: {ScheduleId}, TenantId: {TenantId}",
            Context.ConnectionId, scheduleId, tenantId);
    }

    /// <summary>
    /// Client method to get current connection info
    /// </summary>
    public async Task<object> GetConnectionInfo()
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        return await Task.FromResult(new
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            TenantId = tenantId,
            ConnectedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Extract tenant ID from JWT claims
    /// </summary>
    private Guid GetTenantId()
    {
        var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Extract user ID from JWT claims
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}
