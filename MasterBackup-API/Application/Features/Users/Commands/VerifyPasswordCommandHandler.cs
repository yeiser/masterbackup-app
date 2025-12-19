using MasterBackup_API.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Users.Commands;

public class VerifyPasswordCommandHandler : IRequestHandler<VerifyPasswordCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VerifyPasswordCommandHandler> _logger;

    public VerifyPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<VerifyPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> Handle(VerifyPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.UserId);
                return false;
            }

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password for user {UserId}", request.UserId);
            return false;
        }
    }
}
