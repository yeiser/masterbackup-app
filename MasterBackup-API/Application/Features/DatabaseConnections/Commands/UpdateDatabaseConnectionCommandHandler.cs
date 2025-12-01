using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public class UpdateDatabaseConnectionCommandHandler : IRequestHandler<UpdateDatabaseConnectionCommand, DatabaseConnectionDto>
{
    private readonly TenantDbContext _context;
    private readonly MasterDbContext _masterContext;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<UpdateDatabaseConnectionCommandHandler> _logger;

    public UpdateDatabaseConnectionCommandHandler(
        TenantDbContext context,
        MasterDbContext masterContext,
        IEncryptionService encryptionService,
        ILogger<UpdateDatabaseConnectionCommandHandler> logger)
    {
        _context = context;
        _masterContext = masterContext;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<DatabaseConnectionDto> Handle(UpdateDatabaseConnectionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await _context.DatabaseConnections
            .Include(c => c.BackupSchedules)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (connection == null)
                throw new KeyNotFoundException($"Database connection with ID {request.Id} not found");

            var dto = request.Dto;

            // Validar que el worker existe si el modo es Dedicado
            if (dto.AssignmentMode == Domain.Enums.WorkerAssignmentMode.Dedicated && dto.AssignedWorkerId.HasValue)
            {
                var workerExists = await _masterContext.Workers
                    .AnyAsync(w => w.Id == dto.AssignedWorkerId.Value, cancellationToken);

                if (!workerExists)
                    throw new InvalidOperationException($"Worker with ID {dto.AssignedWorkerId.Value} not found");
            }

            connection.Name = dto.Name;
            connection.Description = dto.Description;
            connection.Host = dto.Host;
            connection.Port = dto.Port;
            connection.Database = dto.Database;
            connection.Username = dto.Username;
            connection.SSLMode = dto.SSLMode;
            connection.IsActive = dto.IsActive;
            connection.AssignedWorkerId = dto.AssignedWorkerId;
            connection.Tags = dto.Tags;
            connection.AssignmentMode = dto.AssignmentMode;
            connection.UpdatedAt = DateTime.UtcNow;

            // Solo actualizar password si se proporciona uno nuevo
            if (!string.IsNullOrEmpty(dto.Password))
            {
                connection.EncryptedPassword = _encryptionService.Encrypt(dto.Password);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database connection {Name} updated successfully", connection.Name);

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
                BackupSchedulesCount = connection.BackupSchedules.Count,
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
