using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Features.Notifications.Commands;
using MasterBackup_API.Application.Features.Notifications.Queries;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener notificaciones del usuario
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int pageSize = 50, [FromQuery] int pageNumber = 1)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var query = new GetUserNotificationsQuery
        {
            UserId = userId,
            IsRead = isRead,
            PageSize = pageSize,
            PageNumber = pageNumber
        };

        var notifications = await _mediator.Send(query);
        return Ok(notifications);
    }

    /// <summary>
    /// Marcar notificación como leída
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var command = new MarkNotificationAsReadCommand
        {
            NotificationId = id,
            UserId = userId
        };

        await _mediator.Send(command);
        return Ok(new { success = true, message = "Notification marked as read" });
    }

    /// <summary>
    /// Obtener cantidad de notificaciones no leídas
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var query = new GetUnreadCountQuery { UserId = userId };
        var count = await _mediator.Send(query);
        
        return Ok(new { count });
    }
}
