using MasterBackup_API.Application.Features.Plans.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtener todos los planes disponibles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] bool activeOnly = true)
    {
        var query = new GetPlansQuery { ActiveOnly = activeOnly };
        var plans = await _mediator.Send(query);
        return Ok(plans);
    }

    /// <summary>
    /// Obtener un plan por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        var query = new GetPlanByIdQuery { PlanId = id };
        var plan = await _mediator.Send(query);
        
        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        return Ok(plan);
    }
}
