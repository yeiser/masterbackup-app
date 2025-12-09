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
        // Validate worker exists and is active
        var worker = await _context.Workers
            .Include(w => w.Tenant)
            .FirstOrDefaultAsync(w => w.Id == request.WorkerId && w.IsActive, cancellationToken);

        if (worker == null)
        {
            _logger.LogWarning("Worker {WorkerId} not found or inactive", request.WorkerId);
            throw new UnauthorizedAccessException("Worker not found or inactive");
        }

        // Get RabbitMQ credentials from configuration
        var rabbitMQHost = _configuration["RabbitMQ:Host"] 
            ?? throw new InvalidOperationException("RabbitMQ Host not configured");
        var rabbitMQPort = _configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var rabbitMQUsername = _configuration["RabbitMQ:Username"] 
            ?? throw new InvalidOperationException("RabbitMQ Username not configured");
        var rabbitMQPassword = _configuration["RabbitMQ:Password"] 
            ?? throw new InvalidOperationException("RabbitMQ Password not configured");
        var rabbitMQVirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";

        // Queue name is tenant-specific
        var queueName = $"backup-jobs-{worker.TenantId}";

        // Get Azure Storage connection string
        var azureStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
            ?? _configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");

        var containerPrefix = _configuration["AzureStorage:ContainerPrefix"] ?? "backups";

        _logger.LogInformation("Providing credentials to worker {WorkerId} ({WorkerName}) for tenant {TenantId}",
            worker.Id, worker.Name, worker.TenantId);

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
