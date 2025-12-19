using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Subscriptions.Commands;
using MasterBackup_API.Application.Features.Subscriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(IMediator mediator, ILogger<SubscriptionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtener la suscripción actual del tenant
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        try
        {
            var query = new GetCurrentSubscriptionQuery();
            var subscription = await _mediator.Send(query);

            if (subscription == null)
            {
                return NotFound(new { message = "No active subscription found" });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current subscription");
            return StatusCode(500, new { message = "An error occurred while getting the subscription" });
        }
    }

    /// <summary>
    /// Crear una nueva suscripción
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto dto)
    {
        try
        {
            var command = new CreateSubscriptionCommand
            {
                PlanId = dto.PlanId,
                BillingCycle = dto.BillingCycle,
                UseTrial = dto.UseTrial
            };

            var subscription = await _mediator.Send(command);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return StatusCode(500, new { message = "An error occurred while creating the subscription" });
        }
    }

    /// <summary>
    /// Actualizar/Upgrade de suscripción
    /// </summary>
    [HttpPut("upgrade")]
    public async Task<IActionResult> UpgradeSubscription([FromBody] UpgradeSubscriptionDto dto)
    {
        try
        {
            var command = new UpgradeSubscriptionCommand
            {
                NewPlanId = dto.NewPlanId,
                BillingCycle = dto.BillingCycle
            };

            var subscription = await _mediator.Send(command);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading subscription");
            return StatusCode(500, new { message = "An error occurred while upgrading the subscription" });
        }
    }

    /// <summary>
    /// Cancelar suscripción
    /// </summary>
    [HttpDelete("cancel")]
    public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionDto dto)
    {
        try
        {
            var command = new CancelSubscriptionCommand
            {
                Reason = dto.Reason
            };

            await _mediator.Send(command);
            return Ok(new { message = "Subscription canceled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling subscription");
            return StatusCode(500, new { message = "An error occurred while canceling the subscription" });
        }
    }
}
