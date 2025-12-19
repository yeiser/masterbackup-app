using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Users.Commands;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserProfileDto?>
{
    private readonly MasterDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        MasterDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserProfileDto?> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return null;
            }

            // Update user profile fields
            user.FirstName = request.ProfileDto.FirstName;
            user.LastName = request.ProfileDto.LastName;
            user.PhoneNumber = request.ProfileDto.PhoneNumber;

            // Update email if changed
            if (user.Email != request.ProfileDto.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, request.ProfileDto.Email);
                if (!setEmailResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update email for user {UserId}", request.UserId);
                    return null;
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to update user {UserId}: {Errors}", 
                    request.UserId, 
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                return null;
            }

            // Get tenant name
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);

            // Return updated profile
            var roles = await _userManager.GetRolesAsync(user);
            
            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "User",
                TenantId = user.TenantId.ToString(),
                TenantName = tenant?.Name ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for {UserId}", request.UserId);
            return null;
        }
    }
}
