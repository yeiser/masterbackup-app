using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;
using MasterBackup_Worker.Domain.Enums;

namespace MasterBackup_Worker.Application.Services;

/// <summary>
/// Service responsible for validating if a worker is authorized to process jobs
/// based on the assignment mode (Dedicated or Auto with tags)
/// </summary>
public class WorkerAuthorizationService : IWorkerAuthorizationService
{
    private readonly WorkerConfiguration _workerConfig;
    private readonly ILogger<WorkerAuthorizationService> _logger;
    private string _lastFailureReason = string.Empty;

    public WorkerAuthorizationService(
        WorkerConfiguration workerConfig,
        ILogger<WorkerAuthorizationService> logger)
    {
        _workerConfig = workerConfig ?? throw new ArgumentNullException(nameof(workerConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> CanProcessTestConnectionAsync(TestConnectionMessage message)
    {
        if (message == null)
        {
            _lastFailureReason = "Message is null";
            return Task.FromResult(false);
        }

        // Validate tenant
        if (message.TenantId != _workerConfig.TenantId)
        {
            _lastFailureReason = $"Worker belongs to tenant {_workerConfig.TenantId} but job is for tenant {message.TenantId}";
            _logger.LogWarning("Authorization failed for test connection {ConnectionId}: {Reason}", 
                message.ConnectionId, _lastFailureReason);
            return Task.FromResult(false);
        }

        // Validate based on assignment mode
        var isAuthorized = ValidateAssignment(
            message.AssignmentMode, 
            message.AssignedWorkerId, 
            message.Tags,
            $"test connection {message.ConnectionId}");

        if (isAuthorized)
        {
            _logger.LogInformation(
                "Worker {WorkerId} ({WorkerName}) authorized to process test connection {ConnectionId} (Mode: {Mode})",
                _workerConfig.WorkerId, _workerConfig.WorkerName, message.ConnectionId, message.AssignmentMode);
        }

        return Task.FromResult(isAuthorized);
    }

    public Task<bool> CanProcessBackupJobAsync(BackupJobMessage message)
    {
        if (message == null)
        {
            _lastFailureReason = "Message is null";
            return Task.FromResult(false);
        }

        // Validate tenant
        if (message.TenantId != _workerConfig.TenantId)
        {
            _lastFailureReason = $"Worker belongs to tenant {_workerConfig.TenantId} but job is for tenant {message.TenantId}";
            _logger.LogWarning("Authorization failed for backup job {JobId}: {Reason}", 
                message.JobId, _lastFailureReason);
            return Task.FromResult(false);
        }

        // Validate database type support
        var databaseTypeString = message.DatabaseConnection.DatabaseType;
        if (!_workerConfig.SupportedDatabaseTypes.Contains(databaseTypeString, StringComparer.OrdinalIgnoreCase))
        {
            _lastFailureReason = $"Worker does not support database type {databaseTypeString}. Supported types: [{string.Join(", ", _workerConfig.SupportedDatabaseTypes)}]";
            _logger.LogWarning("Authorization failed for backup job {JobId}: {Reason}", 
                message.JobId, _lastFailureReason);
            return Task.FromResult(false);
        }

        // Worker is authorized if tenant and database type match
        _logger.LogInformation(
            "Worker {WorkerId} ({WorkerName}) authorized to process backup job {JobId} (Database: {DatabaseType})",
            _workerConfig.WorkerId, _workerConfig.WorkerName, message.JobId, 
            message.DatabaseConnection.DatabaseType);

        return Task.FromResult(true);
    }

    public string GetAuthorizationFailureReason()
    {
        return _lastFailureReason;
    }

    /// <summary>
    /// Validates the worker assignment based on the mode (Dedicated or Auto)
    /// </summary>
    private bool ValidateAssignment(string assignmentMode, Guid? assignedWorkerId, string[] tags, string jobDescription)
    {
        if (string.IsNullOrEmpty(assignmentMode))
        {
            _lastFailureReason = "Assignment mode is not specified";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        // Parse assignment mode
        if (!Enum.TryParse<WorkerAssignmentMode>(assignmentMode, true, out var mode))
        {
            _lastFailureReason = $"Invalid assignment mode: {assignmentMode}";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        switch (mode)
        {
            case WorkerAssignmentMode.Dedicated:
                return ValidateDedicatedMode(assignedWorkerId, jobDescription);

            case WorkerAssignmentMode.Auto:
                return ValidateAutoMode(tags, jobDescription);

            default:
                _lastFailureReason = $"Unsupported assignment mode: {mode}";
                _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
                return false;
        }
    }

    /// <summary>
    /// Validates Dedicated mode: Worker must be explicitly assigned
    /// </summary>
    private bool ValidateDedicatedMode(Guid? assignedWorkerId, string jobDescription)
    {
        if (!assignedWorkerId.HasValue)
        {
            _lastFailureReason = "Dedicated mode requires an assigned worker ID, but none was provided";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        if (assignedWorkerId.Value != _workerConfig.WorkerId)
        {
            _lastFailureReason = $"Job is assigned to worker {assignedWorkerId.Value}, but this is worker {_workerConfig.WorkerId}";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        _logger.LogDebug("Worker {WorkerId} validated for {JobDescription} in Dedicated mode", 
            _workerConfig.WorkerId, jobDescription);
        return true;
    }

    /// <summary>
    /// Validates Auto mode: Worker must have at least one matching tag
    /// </summary>
    private bool ValidateAutoMode(string[] tags, string jobDescription)
    {
        if (tags == null || tags.Length == 0)
        {
            _lastFailureReason = "Auto mode requires tags, but none were provided";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        if (_workerConfig.Tags == null || _workerConfig.Tags.Length == 0)
        {
            _lastFailureReason = $"Worker has no tags configured. Job requires tags: [{string.Join(", ", tags)}]";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        // Check for tag intersection (at least one common tag)
        var commonTags = _workerConfig.Tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).ToArray();
        
        if (commonTags.Length == 0)
        {
            _lastFailureReason = $"No matching tags. Worker tags: [{string.Join(", ", _workerConfig.Tags)}], Job requires: [{string.Join(", ", tags)}]";
            _logger.LogWarning("Authorization failed for {JobDescription}: {Reason}", jobDescription, _lastFailureReason);
            return false;
        }

        _logger.LogDebug("Worker {WorkerId} validated for {JobDescription} in Auto mode with matching tags: [{Tags}]", 
            _workerConfig.WorkerId, jobDescription, string.Join(", ", commonTags));
        return true;
    }
}
