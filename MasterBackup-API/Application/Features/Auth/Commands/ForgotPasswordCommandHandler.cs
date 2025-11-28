using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, (bool Success, bool EmailFound)>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        MasterDbContext masterContext,
        ITenantService tenantService,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _masterContext = masterContext;
        _tenantService = tenantService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, bool EmailFound)> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Buscar usuario en la base de datos master
            var user = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return (true, false);
            }

            // Generar token de restablecimiento
            var resetToken = GenerateSecureToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            
            await _masterContext.SaveChangesAsync(cancellationToken);

            // Enviar email con el token
            await _emailService.SendPasswordResetEmailAsync(user, resetToken);
            
            _logger.LogInformation("Password reset email sent to: {Email}", request.Email);
            return (true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email: {Email}", request.Email);
            return (false, false);
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
