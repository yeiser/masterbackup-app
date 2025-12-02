using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Query para obtener una lista de schedules de backup con filtros y paginación
/// </summary>
public class GetBackupSchedulesQuery : IRequest<List<BackupScheduleDto>>
{
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Filtrar por DatabaseConnectionId (opcional)
    /// </summary>
    public Guid? DatabaseConnectionId { get; set; }
    
    /// <summary>
    /// Filtrar por estado activo/inactivo (opcional)
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Búsqueda por nombre (opcional)
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Número de página (default: 1)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Tamaño de página (default: 10)
    /// </summary>
    public int PageSize { get; set; } = 10;
}
