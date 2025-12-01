using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class RegisterWorkerCommandHandler : IRequestHandler<RegisterWorkerCommand, RegisterWorkerResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public RegisterWorkerCommandHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<RegisterWorkerResponseDto> Handle(RegisterWorkerCommand request, CancellationToken cancellationToken)
    {
        // 1. Extraer TenantId del contexto (del ApiKey en el header)
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el tenant");
        }

        // 2. Buscar si ya existe un worker con mismo nombre para este tenant
        var existingWorker = await _masterContext.Workers
            .FirstOrDefaultAsync(w => w.TenantId == tenantId.Value && w.Name == request.Name, cancellationToken);
        
        // 3. Obtener IP del cliente
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        Worker worker;
        string message;

        if (existingWorker != null)
        {
            // 4a. Actualizar worker existente
            existingWorker.Description = request.Description;
            existingWorker.Hostname = request.Hostname;
            existingWorker.IpAddress = ipAddress;
            existingWorker.OsInfo = request.OsInfo;
            existingWorker.SupportedDatabaseTypes = request.SupportedDatabaseTypes;
            existingWorker.MaxConcurrentJobs = request.MaxConcurrentJobs;
            existingWorker.Version = request.Version;
            existingWorker.Tags = request.Tags;
            existingWorker.Status = WorkerStatus.Online;
            existingWorker.LastHeartbeat = DateTime.UtcNow;
            existingWorker.IsActive = true;
            existingWorker.UpdatedAt = DateTime.UtcNow;

            _masterContext.Workers.Update(existingWorker);
            worker = existingWorker;
            message = "Worker actualizado exitosamente";
        }
        else
        {
            // 4b. Crear nuevo Worker
            worker = new Worker
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                Name = request.Name,
                Description = request.Description,
                Hostname = request.Hostname,
                IpAddress = ipAddress,
                OsInfo = request.OsInfo,
                SupportedDatabaseTypes = request.SupportedDatabaseTypes,
                MaxConcurrentJobs = request.MaxConcurrentJobs,
                Version = request.Version,
                Tags = request.Tags,
                Status = WorkerStatus.Online,
                LastHeartbeat = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CurrentActiveJobs = 0,
                TotalBackupsProcessed = 0,
                TotalBytesProcessed = 0
            };
            
            _masterContext.Workers.Add(worker);
            message = "Worker registrado exitosamente";
        }

        await _masterContext.SaveChangesAsync(cancellationToken);

        // 5. Obtener configuración de RabbitMQ
        var rabbitMQHost = _configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMQPort = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672");
        var rabbitMQUsername = _configuration["RabbitMQ:WorkerUsername"] ?? "guest";
        var rabbitMQPassword = _configuration["RabbitMQ:WorkerPassword"] ?? "guest";
        var queueName = $"{tenantId}.backup.queue";

        // 6. Retornar respuesta con configuración de RabbitMQ
        return new RegisterWorkerResponseDto
        {
            WorkerId = worker.Id,
            TenantId = worker.TenantId,
            RabbitMQSettings = new RabbitMQSettingsDto
            {
                Host = rabbitMQHost,
                Port = rabbitMQPort,
                QueueName = queueName,
                Username = rabbitMQUsername,
                Password = rabbitMQPassword
            },
            Message = message
        };
    }
}
