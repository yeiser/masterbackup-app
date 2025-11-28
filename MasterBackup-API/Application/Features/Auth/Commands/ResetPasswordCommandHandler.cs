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
            // Buscar usuario por token en lugar de email
            var user = await _masterContext.Users
                .FirstOrDefaultAsync(u => 
                    u.PasswordResetToken == request.Token && 
                    u.IsActive,
                    cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password reset attempted with invalid token");
                return false;
            }

            // Verificar que el token no haya expirado
            if (user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {Email}", user.Email);
                return false;
            }

            // Hash de la nueva contraseña
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
            
            // Limpiar el token de restablecimiento
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _masterContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successful for user: {Email}", user.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return false;
        }
    }
}
