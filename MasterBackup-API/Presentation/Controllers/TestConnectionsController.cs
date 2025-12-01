using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Features.DatabaseConnections.Commands;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller for handling test connection callbacks from Workers
/// </summary>
[ApiController]
[Route("api/test-connections")]
public class TestConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TestConnectionsController> _logger;

    public TestConnectionsController(
        IMediator mediator,
        ILogger<TestConnectionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Callback endpoint for Workers to report test connection results
    /// </summary>
    /// <remarks>
    /// This endpoint is called by Workers after testing a database connection.
    /// It updates the DatabaseConnection entity with the test result.
    /// 
    /// Authentication: Requires API Key in X-API-Key header (Worker authentication)
    /// </remarks>
    [HttpPost("complete")]
    [AllowAnonymous] // Will use API Key authentication
    public async Task<IActionResult> CompleteTestConnection([FromBody] CompleteTestConnectionRequest request)
    {
        // TODO: Validate API Key from Worker in header
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Test connection callback received without API Key");
            return Unauthorized(new { message = "API Key is required" });
        }

        // TODO: Validate that API Key belongs to a valid Worker
        // For now, we'll accept any request

        _logger.LogInformation("Received test connection result for ConnectionId {ConnectionId}: Success={Success}, Message={Message}",
            request.ConnectionId, request.Success, request.Message);

        var command = new UpdateTestResultCommand
        {
            ConnectionId = request.ConnectionId,
            Success = request.Success,
            Status = request.Message,
            ServerVersion = request.ServerVersion
        };

        var result = await _mediator.Send(command);

        if (!result)
        {
            _logger.LogWarning("Failed to update test result for ConnectionId {ConnectionId}", request.ConnectionId);
            return NotFound(new { message = "Connection not found" });
        }

        return Ok(new { message = "Test result updated successfully" });
    }
}

/// <summary>
/// Request model for test connection callback
/// </summary>
public record CompleteTestConnectionRequest
{
    public Guid ConnectionId { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ServerVersion { get; init; }
}
