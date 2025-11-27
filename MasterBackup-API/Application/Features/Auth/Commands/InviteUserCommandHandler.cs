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
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly ILogger<InviteUserCommandHandler> _logger;

    public InviteUserCommandHandler(
        ITenantService tenantService,
        IEmailService emailService,
        ILogger<InviteUserCommandHandler> logger)
    {
        _tenantService = tenantService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(request.TenantId);
            using var tenantContext = new TenantDbContext(tenantOptions);

            var userManager = CreateUserManager(tenantContext);

            // Check if user already exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return false;
            }

            // Check if there's a pending invitation
            var existingInvitation = await tenantContext.UserInvitations
                .FirstOrDefaultAsync(i => i.Email == request.Email && !i.IsAccepted, cancellationToken);

            if (existingInvitation != null)
            {
                return false;
            }

            var inviter = await userManager.FindByIdAsync(request.InvitedByUserId);

            var invitation = new UserInvitation
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Role = request.Role,
                InvitationToken = GenerateSecureToken(),
                InvitedByUserId = request.InvitedByUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            tenantContext.UserInvitations.Add(invitation);
            await tenantContext.SaveChangesAsync(cancellationToken);

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

    private UserManager<ApplicationUser> CreateUserManager(TenantDbContext context)
    {
        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
        var options = new IdentityOptions();
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new Logger<UserManager<ApplicationUser>>(new LoggerFactory());

        return new UserManager<ApplicationUser>(
            userStore,
            Microsoft.Extensions.Options.Options.Create(options),
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger
        );
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
