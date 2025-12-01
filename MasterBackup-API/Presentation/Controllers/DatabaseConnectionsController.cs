using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.DatabaseConnections.Commands;
using MasterBackup_API.Application.Features.DatabaseConnections.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DatabaseConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<CreateDatabaseConnectionDto> _createValidator;
    private readonly IValidator<UpdateDatabaseConnectionDto> _updateValidator;
    private readonly ILogger<DatabaseConnectionsController> _logger;

    public DatabaseConnectionsController(
        IMediator mediator,
        IValidator<CreateDatabaseConnectionDto> createValidator,
        IValidator<UpdateDatabaseConnectionDto> updateValidator,
        ILogger<DatabaseConnectionsController> logger)
    {
        _mediator = mediator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all database connections for the current tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _mediator.Send(new GetDatabaseConnectionsQuery());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database connections");
            return StatusCode(500, new { message = "An error occurred while retrieving database connections" });
        }
    }

    /// <summary>
    /// Get a database connection by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetDatabaseConnectionByIdQuery(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database connection {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the database connection" });
        }
    }

    /// <summary>
    /// Create a new database connection
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDatabaseConnectionDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _mediator.Send(new CreateDatabaseConnectionCommand(dto, userId));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating database connection");
            return StatusCode(500, new { message = "An error occurred while creating the database connection" });
        }
    }

    /// <summary>
    /// Update an existing database connection
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDatabaseConnectionDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        try
        {
            var result = await _mediator.Send(new UpdateDatabaseConnectionCommand(id, dto));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database connection {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the database connection" });
        }
    }

    /// <summary>
    /// Delete a database connection
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new DeleteDatabaseConnectionCommand(id));
            return Ok(new { message = "Database connection deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting database connection {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the database connection" });
        }
    }

    /// <summary>
    /// Test an existing database connection (sends job to Worker via RabbitMQ)
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new TestDatabaseConnectionCommand(id));

            if (!result)
                return NotFound(new { message = "Connection not found" });

            return Accepted(new { message = "Test connection job sent to worker. Result will arrive via SignalR." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test connection job for {Id}", id);
            return StatusCode(500, new { message = "An error occurred while sending test connection job" });
        }
    }

    /// <summary>
    /// Update test result (called by Worker)
    /// </summary>
    [HttpPut("{id}/test-result")]
    public async Task<IActionResult> UpdateTestResult(Guid id, [FromBody] UpdateTestResultCommand command)
    {
        command.ConnectionId = id;

        try
        {
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound(new { message = "Connection not found" });

            return Ok(new { message = "Test result updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test result for connection {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating test result" });
        }
    }
}
