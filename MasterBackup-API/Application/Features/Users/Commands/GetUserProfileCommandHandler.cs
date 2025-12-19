using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Users.Commands;

public class GetUserProfileCommandHandler : IRequestHandler<GetUserProfileCommand, UserProfileDto?>
{
    private readonly MasterDbContext _masterContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GetUserProfileCommandHandler> _logger;

    public GetUserProfileCommandHandler(
        MasterDbContext masterContext,
        UserManager<ApplicationUser> userManager,
        ILogger<GetUserProfileCommandHandler> logger)
    {
        _masterContext = masterContext;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return null;
            }

            // Obtener información del tenant
            var tenant = await _masterContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                TenantId = user.TenantId.ToString(),
                TenantName = tenant?.Name,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return null;
        }
    }
}
