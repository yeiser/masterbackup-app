using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public class CreateDatabaseConnectionCommandHandler : IRequestHandler<CreateDatabaseConnectionCommand, DatabaseConnectionDto>
{
    private readonly TenantDbContext _context;
    private readonly MasterDbContext _masterDbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly ISubscriptionValidationService _subscriptionValidation;
    private readonly ILogger<CreateDatabaseConnectionCommandHandler> _logger;

    public CreateDatabaseConnectionCommandHandler(
        TenantDbContext context,
        MasterDbContext masterDbContext,
        IEncryptionService encryptionService,
        ISubscriptionValidationService subscriptionValidation,
        ILogger<CreateDatabaseConnectionCommandHandler> logger)
    {
        _context = context;
        _masterDbContext = masterDbContext;
        _encryptionService = encryptionService;
        _subscriptionValidation = subscriptionValidation;
        _logger = logger;
    }

    public async Task<DatabaseConnectionDto> Handle(CreateDatabaseConnectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Dto;

            // Validar límites de suscripción
            var (canCreate, errorMessage) = await _subscriptionValidation.CanCreateDatabaseAsync(cancellationToken);
            if (!canCreate)
            {
                _logger.LogWarning("Database creation blocked: {Error}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Validar que el worker existe si el modo es Dedicado
            if (dto.AssignmentMode == WorkerAssignmentMode.Dedicated && dto.AssignedWorkerId.HasValue)
            {
                var workerExists = await _masterDbContext.Workers
                    .AnyAsync(w => w.Id == dto.AssignedWorkerId.Value, cancellationToken);

                if (!workerExists)
                    throw new InvalidOperationException($"Worker with ID {dto.AssignedWorkerId.Value} not found");
            }

            // Encriptar password
            var encryptedPassword = _encryptionService.Encrypt(dto.Password);

            var connection = new DatabaseConnection
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type,
                Host = dto.Host,
                Port = dto.Port,
                Database = dto.Database,
                Username = dto.Username,
                EncryptedPassword = encryptedPassword,
                EngineVersion = dto.EngineVersion,
                SSLMode = dto.SSLMode,
                IsActive = dto.IsActive,
                CreatedBy = request.UserId,
                CreatedAt = DateTime.UtcNow,
                AssignedWorkerId = dto.AssignedWorkerId,
                Tags = dto.Tags,
                AssignmentMode = dto.AssignmentMode
            };

            _context.DatabaseConnections.Add(connection);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database connection {Name} created successfully by user {UserId}", connection.Name, request.UserId);

            return new DatabaseConnectionDto
            {
                Id = connection.Id,
                Name = connection.Name,
                Description = connection.Description,
                Type = ((int)connection.Type).ToString(),
                Host = connection.Host,
                Port = connection.Port,
                Database = connection.Database,
                Username = connection.Username,
                EngineVersion = connection.EngineVersion,
                SSLMode = connection.SSLMode,
                IsActive = connection.IsActive,
                CreatedBy = connection.CreatedBy,
                CreatedAt = connection.CreatedAt,
                UpdatedAt = connection.UpdatedAt,
                LastTestedAt = connection.LastTestedAt,
                LastTestSuccessful = connection.LastTestSuccessful,
                LastTestStatus = connection.LastTestStatus,
                BackupSchedulesCount = 0,
                AssignedWorkerId = connection.AssignedWorkerId,
                Tags = connection.Tags,
                AssignmentMode = connection.AssignmentMode.ToString()
            };
        }
        catch(Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
}
