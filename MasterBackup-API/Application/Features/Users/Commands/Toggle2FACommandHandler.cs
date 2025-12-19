using MasterBackup_API.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Users.Commands;

public class Toggle2FACommandHandler : IRequestHandler<Toggle2FACommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<Toggle2FACommandHandler> _logger;

    public Toggle2FACommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<Toggle2FACommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> Handle(Toggle2FACommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return false;
            }

            user.TwoFactorEnabled = request.Enable;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to toggle 2FA for user {UserId}: {Errors}",
                    request.UserId,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            _logger.LogInformation("2FA {Status} for user {UserId}",
                request.Enable ? "enabled" : "disabled",
                request.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling 2FA for user {UserId}", request.UserId);
            return false;
        }
    }
}
