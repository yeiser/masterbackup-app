using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public class UpdateTestResultCommandHandler : IRequestHandler<UpdateTestResultCommand, bool>
{
    private readonly TenantDbContext _tenantContext;
    private readonly ILogger<UpdateTestResultCommandHandler> _logger;

    public UpdateTestResultCommandHandler(
        TenantDbContext tenantContext,
        ILogger<UpdateTestResultCommandHandler> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateTestResultCommand request, CancellationToken cancellationToken)
    {
        var connection = await _tenantContext.DatabaseConnections
            .FirstOrDefaultAsync(x => x.Id == request.ConnectionId, cancellationToken);

        if (connection == null)
        {
            _logger.LogWarning("Connection {ConnectionId} not found for test result update", request.ConnectionId);
            return false;
        }

        connection.LastTestedAt = DateTime.UtcNow;
        connection.LastTestSuccessful = request.Success;
        connection.LastTestStatus = request.Status;

        if (request.Success && !string.IsNullOrEmpty(request.ServerVersion))
        {
            connection.EngineVersion = request.ServerVersion;
        }

        await _tenantContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Test result updated for connection {ConnectionId}: Success={Success}, Status={Status}", 
            connection.Id, request.Success, request.Status);

        return true;
    }
}
