using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MediatR;

namespace MasterBackup_API.Application.Features.Dashboard.Queries;

public class GetBackupStatsQueryHandler : IRequestHandler<GetBackupStatsQuery, BackupStatsDto>
{
    private readonly ITenantContext _tenantContext;

    public GetBackupStatsQueryHandler(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task<BackupStatsDto> Handle(GetBackupStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = new BackupStatsDto
        {
            Pending = 0,
            InProgress = 0,
            Failed = 0,
            Completed = 0,
            Total = 0
        };

        if (string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            return stats;
        }

        try
        {
            using (var connection = new Npgsql.NpgsqlConnection(_tenantContext.ConnectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // Contar backups por estado
                using (var cmd = new Npgsql.NpgsqlCommand(
                    @"SELECT 
                        COUNT(*) FILTER (WHERE ""Status"" = 1) as pending,
                        COUNT(*) FILTER (WHERE ""Status"" = 2) as inprogress,
                        COUNT(*) FILTER (WHERE ""Status"" = 4) as failed,
                        COUNT(*) FILTER (WHERE ""Status"" = 3) as completed,
                        COUNT(*) as total
                      FROM ""BackupHistories""", connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            stats.Pending = reader.GetInt32(0);
                            stats.InProgress = reader.GetInt32(1);
                            stats.Failed = reader.GetInt32(2);
                            stats.Completed = reader.GetInt32(3);
                            stats.Total = reader.GetInt32(4);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Si hay error al conectar a la BD del tenant, devolver stats en 0
        }

        return stats;
    }
}
