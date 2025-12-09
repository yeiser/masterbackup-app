using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Queries;

public class GetWorkerCredentialsQueryHandler : IRequestHandler<GetWorkerCredentialsQuery, WorkerCredentialsDto>
{
    private readonly MasterDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GetWorkerCredentialsQueryHandler> _logger;

    public GetWorkerCredentialsQueryHandler(
        MasterDbContext context,
        IConfiguration configuration,
        ILogger<GetWorkerCredentialsQueryHandler> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WorkerCredentialsDto> Handle(GetWorkerCredentialsQuery request, CancellationToken cancellationToken)
    {
        // Validate tenant exists and is active
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found or inactive", request.TenantId);
            throw new UnauthorizedAccessException("Tenant not found or inactive");
        }

        // Verify at least one active worker exists for this tenant
        var hasActiveWorker = await _context.Workers
            .AnyAsync(w => w.TenantId == request.TenantId && w.IsActive, cancellationToken);

        if (!hasActiveWorker)
        {
            _logger.LogWarning("No active workers found for tenant {TenantId}", request.TenantId);
            throw new UnauthorizedAccessException("No active workers found for this tenant");
        }

        // Get RabbitMQ credentials from configuration
        var rabbitMQHost = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("RabbitMQ Host not configured");
        var rabbitMQPort = _configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var rabbitMQUsername = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("RabbitMQ Username not configured");
        var rabbitMQPassword = _configuration["RabbitMQ:Password"] 
            ?? throw new InvalidOperationException("RabbitMQ Password not configured");
        var rabbitMQVirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

        // Queue name is tenant-specific
        var queueName = $"backup-jobs-{request.TenantId}";

        // Get Azure Storage connection string
        var azureStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? _configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");

        var containerPrefix = _configuration["AzureStorage:ContainerPrefix"] ?? "backups";

        _logger.LogInformation("Providing credentials for tenant {TenantId}", request.TenantId);

        return new WorkerCredentialsDto
        {
            RabbitMQ = new RabbitMQCredentials
            {
                Host = rabbitMQHost,
                Port = rabbitMQPort,
                Username = rabbitMQUsername,
                Password = rabbitMQPassword,
                VirtualHost = rabbitMQVirtualHost,
                QueueName = queueName
            },
            AzureStorageConnectionString = azureStorageConnectionString,
            ContainerPrefix = containerPrefix
        };
    }
}
