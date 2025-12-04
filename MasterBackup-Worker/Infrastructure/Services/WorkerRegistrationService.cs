using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Infrastructure.Services;

/// <summary>
/// Background service that registers the worker with the API and sends periodic heartbeats
/// </summary>
public class WorkerRegistrationService : BackgroundService
{
    private readonly ILogger<WorkerRegistrationService> _logger;
    private readonly IApiClient _apiClient;
    private readonly WorkerConfiguration _workerConfig;
    private Guid _registeredWorkerId;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);

    public WorkerRegistrationService(
        ILogger<WorkerRegistrationService> logger,
        IApiClient apiClient,
        WorkerConfiguration workerConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _workerConfig = workerConfig ?? throw new ArgumentNullException(nameof(workerConfig));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker registration service starting...");

        // Register worker on startup
        var registered = await RegisterWorkerAsync(stoppingToken);

        if (!registered)
        {
            _logger.LogError("Failed to register worker. Service will not continue.");
            return;
        }

        _logger.LogInformation("Worker registered successfully. Starting heartbeat loop...");

        // Send periodic heartbeats
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, stoppingToken);
                await SendHeartbeatAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Heartbeat loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in heartbeat loop");
            }
        }

        _logger.LogInformation("Worker registration service stopped");
    }

    private async Task<bool> RegisterWorkerAsync(CancellationToken cancellationToken)
    {
        int retryCount = 0;
        const int maxRetries = 5;
        const int retryDelaySeconds = 5;

        while (retryCount < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Attempting to register worker (attempt {Attempt}/{MaxAttempts})...",
                    retryCount + 1, maxRetries);

                var hostname = Environment.MachineName;
                var osInfo = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
                var version = "1.0.0"; // TODO: Get from assembly version

                // Pass existing WorkerId if configured (for consistent development Worker IDs)
                var existingWorkerId = _workerConfig.WorkerId != Guid.Empty ? _workerConfig.WorkerId : (Guid?)null;
                
                var (success, workerId, tenantId, message) = await _apiClient.RegisterWorkerAsync(
                    _workerConfig.WorkerName,
                    hostname,
                    osInfo,
                    _workerConfig.SupportedDatabaseTypes,
                    _workerConfig.MaxConcurrentJobs,
                    version,
                    _workerConfig.Tags,
                    existingWorkerId);

                if (success)
                {
                    _registeredWorkerId = workerId;
                    
                    // Update worker config with the registered ID AND TenantId from API
                    _workerConfig.WorkerId = workerId;
                    _workerConfig.TenantId = tenantId;
                    
                    _logger.LogInformation("✓ {Message}", message);
                    _logger.LogInformation("  Worker ID: {WorkerId}", workerId);
                    _logger.LogInformation("  Tenant ID: {TenantId}", tenantId);
                    _logger.LogInformation("  Worker Name: {WorkerName}", _workerConfig.WorkerName);
                    
                    return true;
                }

                _logger.LogWarning("Registration failed: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during worker registration");
            }

            retryCount++;

            if (retryCount < maxRetries)
            {
                _logger.LogInformation("Retrying in {Seconds} seconds...", retryDelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
            }
        }

        _logger.LogError("Failed to register worker after {MaxRetries} attempts", maxRetries);
        return false;
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {
            // For now, assume "Online" status with 0 active jobs
            // These values should be updated based on actual worker state
            await _apiClient.SendHeartbeatAsync(
                _registeredWorkerId,
                "Online",
                currentActiveJobs: 0);

            _logger.LogDebug("Heartbeat sent for worker {WorkerId}", _registeredWorkerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat");
        }
    }
}
