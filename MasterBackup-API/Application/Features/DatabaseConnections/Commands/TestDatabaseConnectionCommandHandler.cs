using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public class TestDatabaseConnectionCommandHandler : IRequestHandler<TestDatabaseConnectionCommand, bool>
{
    private readonly TenantDbContext _tenantContext;
    private readonly MasterDbContext _masterContext;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ITenantService _tenantService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TestDatabaseConnectionCommandHandler> _logger;

    public TestDatabaseConnectionCommandHandler(
        TenantDbContext tenantContext,
        MasterDbContext masterContext,
        IMessageQueueService messageQueueService,
        ITenantService tenantService,
        IEncryptionService encryptionService,
        ILogger<TestDatabaseConnectionCommandHandler> logger)
    {
        _tenantContext = tenantContext;
        _masterContext = masterContext;
        _messageQueueService = messageQueueService;
        _tenantService = tenantService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<bool> Handle(TestDatabaseConnectionCommand request, CancellationToken cancellationToken)
    {
        var connection = await _tenantContext.DatabaseConnections
            .FirstOrDefaultAsync(x => x.Id == request.ConnectionId, cancellationToken);

        if (connection == null)
        {
            _logger.LogWarning("Connection {ConnectionId} not found", request.ConnectionId);
            return false;
        }

        var tenantId = _tenantService.GetCurrentTenantId();

        // Validar worker assignment según el modo
        if (connection.AssignmentMode == WorkerAssignmentMode.Dedicated)
        {
            if (!connection.AssignedWorkerId.HasValue)
            {
                _logger.LogWarning("Connection {ConnectionId} is in Dedicated mode but has no assigned worker", connection.Id);
                throw new InvalidOperationException("La conexión está en modo Dedicado pero no tiene un worker asignado. Por favor, asigne un worker antes de probar la conexión.");
            }

            var assignedWorker = await _masterContext.Workers
                .FirstOrDefaultAsync(w => w.Id == connection.AssignedWorkerId.Value 
                    && w.TenantId == tenantId, cancellationToken);

            if (assignedWorker == null)
            {
                _logger.LogWarning("Assigned worker {WorkerId} not found for connection {ConnectionId}", 
                    connection.AssignedWorkerId.Value, connection.Id);
                throw new InvalidOperationException("El worker asignado no existe.");
            }

            if (!assignedWorker.IsActive)
            {
                _logger.LogWarning("Assigned worker {WorkerId} is inactive for connection {ConnectionId}", 
                    assignedWorker.Id, connection.Id);
                throw new InvalidOperationException($"El worker asignado '{assignedWorker.Name}' está inactivo. Por favor, active el worker o asigne uno diferente.");
            }

            if (assignedWorker.Status != WorkerStatus.Online)
            {
                _logger.LogWarning("Assigned worker {WorkerId} is {Status} for connection {ConnectionId}", 
                    assignedWorker.Id, assignedWorker.Status, connection.Id);
                throw new InvalidOperationException($"El worker asignado '{assignedWorker.Name}' no está disponible (Estado: {assignedWorker.Status}). Por favor, espere a que el worker esté en línea o asigne uno diferente.");
            }

            _logger.LogInformation("Connection {ConnectionId} validated with dedicated worker {WorkerId} ({WorkerName})", 
                connection.Id, assignedWorker.Id, assignedWorker.Name);
        }
        else if (connection.AssignmentMode == WorkerAssignmentMode.Auto)
        {
            // Verificar que haya al menos un worker online con tags coincidentes
            var hasMatchingWorker = await _masterContext.Workers
                .Where(w => w.TenantId == tenantId 
                    && w.IsActive 
                    && w.Status == WorkerStatus.Online)
                .AnyAsync(w => connection.Tags.Any(tag => w.Tags.Contains(tag)), cancellationToken);

            if (!hasMatchingWorker)
            {
                _logger.LogWarning("No online workers found matching tags [{Tags}] for connection {ConnectionId}", 
                    string.Join(", ", connection.Tags), connection.Id);
                throw new InvalidOperationException($"No hay workers disponibles con los tags requeridos: [{string.Join(", ", connection.Tags)}]. Por favor, active un worker con estos tags o cambie los tags de la conexión.");
            }

            _logger.LogInformation("Connection {ConnectionId} validated with auto-assignment mode using tags: [{Tags}]", 
                connection.Id, string.Join(", ", connection.Tags));
        }

        // Desencriptar password para enviar al worker
        var decryptedPassword = _encryptionService.Decrypt(connection.EncryptedPassword);

        // Crear mensaje para el worker
        var message = new TestConnectionMessage
        {
            ConnectionId = connection.Id,
            TenantId = tenantId,
            Type = connection.Type,
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = decryptedPassword,
            SSLMode = connection.SSLMode,
            AssignmentMode = connection.AssignmentMode.ToString(),
            AssignedWorkerId = connection.AssignedWorkerId,
            Tags = connection.Tags
        };

        // Enviar a RabbitMQ
        await _messageQueueService.PublishTestConnectionAsync(tenantId, message);

        _logger.LogInformation("Test connection message published for connection {ConnectionId} to RabbitMQ (Mode: {Mode})", 
            connection.Id, connection.AssignmentMode);

        return true;
    }
}
