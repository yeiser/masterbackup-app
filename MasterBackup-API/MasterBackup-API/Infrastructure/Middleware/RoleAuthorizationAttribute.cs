using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Infrastructure.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _roles;

    public RoleAuthorizationAttribute(params UserRole[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userRole) || !_roles.Any(r => r.ToString() == userRole))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
