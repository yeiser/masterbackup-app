using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, bool>
{
    private readonly MasterDbContext _masterContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<InviteUserCommandHandler> _logger;

    public InviteUserCommandHandler(
        MasterDbContext masterContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<InviteUserCommandHandler> logger)
    {
        _masterContext = masterContext;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user already exists in master database
            var existingUser = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == request.TenantId, cancellationToken);
            
            if (existingUser != null)
            {
                return false;
            }

            // Check if there's a pending invitation
            var existingInvitation = await _masterContext.UserInvitations
                .FirstOrDefaultAsync(i => i.Email == request.Email && !i.IsAccepted, cancellationToken);

            if (existingInvitation != null)
            {
                return false;
            }

            var inviter = await _userManager.FindByIdAsync(request.InvitedByUserId);

            var invitation = new UserInvitation
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Role = request.Role,
                InvitationToken = GenerateSecureToken(),
                InvitedByUserId = request.InvitedByUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _masterContext.UserInvitations.Add(invitation);
            await _masterContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendInvitationEmailAsync(
                invitation.Email,
                invitation.InvitationToken,
                $"{inviter?.FirstName} {inviter?.LastName}"
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user");
            return false;
        }
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
