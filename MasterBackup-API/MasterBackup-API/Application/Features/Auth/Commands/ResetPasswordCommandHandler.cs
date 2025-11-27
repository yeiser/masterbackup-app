using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        MasterDbContext masterContext,
        ITenantService tenantService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _masterContext = masterContext;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var allTenants = await _masterContext.Tenants
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user != null &&
                    user.PasswordResetToken == request.Token &&
                    user.PasswordResetTokenExpiry > DateTime.UtcNow)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

                    if (result.Succeeded)
                    {
                        user.PasswordResetToken = null;
                        user.PasswordResetTokenExpiry = null;
                        await userManager.UpdateAsync(user);
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
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
}
