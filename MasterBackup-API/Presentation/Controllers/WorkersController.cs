using MediatR;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Workers.Commands;
using MasterBackup_API.Application.Features.Workers.Queries;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/workers")]
public class WorkersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkersController> _logger;

    public WorkersController(IMediator mediator, ILogger<WorkersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Registrar un nuevo worker (usado por el worker al iniciar)
    /// Requiere ApiKey del tenant en header: X-API-Key
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterWorkerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RegisterWorkerResponseDto>> Register([FromBody] RegisterWorkerDto dto)
    {
        try
        {
            var command = new RegisterWorkerCommand
            {
                WorkerId = dto.WorkerId,
                Name = dto.Name,
                Description = dto.Description,
                Hostname = dto.Hostname,
                OsInfo = dto.OsInfo,
                SupportedDatabaseTypes = dto.SupportedDatabaseTypes,
                MaxConcurrentJobs = dto.MaxConcurrentJobs,
                Version = dto.Version,
                Tags = dto.Tags
            };

            var result = await _mediator.Send(command);
            
            _logger.LogInformation("Worker {WorkerName} registrado exitosamente para tenant {TenantId}", 
                dto.Name, result.TenantId);

            return CreatedAtAction(nameof(GetById), new { id = result.WorkerId }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Intento no autorizado de registro de worker: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Error al registrar worker: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar worker");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar heartbeat del worker (enviar cada 30 segundos)
    /// Requiere ApiKey del tenant en header: X-API-Key
    /// </summary>
    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Heartbeat([FromBody] WorkerHeartbeatDto dto)
    {
        try
        {
            var command = new UpdateWorkerHeartbeatCommand
            {
                WorkerId = dto.WorkerId,
                Status = dto.Status,
                CurrentActiveJobs = dto.CurrentActiveJobs,
                TotalBackupsProcessed = dto.TotalBackupsProcessed,
                TotalBytesProcessed = dto.TotalBytesProcessed
            };

            await _mediator.Send(command);
            
            return Ok(new { message = "Heartbeat actualizado" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar heartbeat del worker {WorkerId}", dto.WorkerId);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los workers del tenant autenticado
    /// Requiere autenticación JWT o ApiKey
    /// </summary>
    [HttpGet]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    [ProducesResponseType(typeof(List<WorkerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkerDto>>> GetAll()
    {
        try
        {
            var query = new GetWorkersQuery();
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener workers");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener un worker específico por ID
    /// Requiere autenticación JWT o ApiKey
    /// </summary>
    [HttpGet("{id}")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    [ProducesResponseType(typeof(WorkerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerDto>> GetById(Guid id)
    {
        try
        {
            var query = new GetWorkerByIdQuery { WorkerId = id };
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener worker {WorkerId}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener estadísticas de los workers del tenant
    /// Requiere autenticación JWT o ApiKey
    /// </summary>
    [HttpGet("stats")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    [ProducesResponseType(typeof(WorkerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkerStatsDto>> GetStats()
    {
        try
        {
            var query = new GetWorkerStatsQuery();
            var result = await _mediator.Send(query);
            
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de workers");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Desactivar un worker (solo Admin)
    /// Requiere autenticación JWT
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [RoleAuthorization(UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            var command = new DeactivateWorkerCommand { WorkerId = id };
            await _mediator.Send(command);
            
            _logger.LogInformation("Worker {WorkerId} desactivado", id);
            
            return Ok(new { message = "Worker desactivado exitosamente" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar worker {WorkerId}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Activar un worker (solo Admin)
    /// Requiere autenticación JWT
    /// </summary>
    [HttpPost("{id}/activate")]
    [RoleAuthorization(UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            var command = new ActivateWorkerCommand { WorkerId = id };
            await _mediator.Send(command);
            
            _logger.LogInformation("Worker {WorkerId} activado", id);
            
            return Ok(new { message = "Worker activado exitosamente" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al activar worker {WorkerId}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Get worker credentials (RabbitMQ + Azure Storage)
    /// This endpoint provides sensitive credentials needed by the worker to operate
    /// Requiere ApiKey del tenant en header: X-API-Key
    /// </summary>
    [HttpGet("credentials")]
    [ProducesResponseType(typeof(WorkerCredentialsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkerCredentialsDto>> GetCredentials()
    {
        try
        {
            // Get worker ID from API Key middleware context
            var workerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "WorkerId");
            if (workerIdClaim == null || !Guid.TryParse(workerIdClaim.Value, out var workerId))
            {
                return Unauthorized(new { message = "Invalid API Key or Worker not found" });
            }

            var query = new GetWorkerCredentialsQuery { WorkerId = workerId };
            var credentials = await _mediator.Send(query);

            return Ok(credentials);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized attempt to get credentials: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker credentials");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}
