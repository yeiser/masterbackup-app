using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class ValidateEmailCommandHandler : IRequestHandler<ValidateEmailCommand, EmailValidationResponse>
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<ValidateEmailCommandHandler> _logger;

    public ValidateEmailCommandHandler(
        MasterDbContext masterContext,
        ILogger<ValidateEmailCommandHandler> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    public async Task<EmailValidationResponse> Handle(ValidateEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Validando email: {Email}", request.Dto.Email);

            // Find user by email (email is unique globally)
            var user = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Dto.Email && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("Usuario no encontrado con email: {Email}", request.Dto.Email);
                return new EmailValidationResponse { Exists = false };
            }

            _logger.LogInformation("Usuario encontrado: {Email}, 2FA: {TwoFactorEnabled}", 
                request.Dto.Email, user.TwoFactorEnabled);

            return new EmailValidationResponse
            {
                Exists = true,
                TwoFactorEnabled = user.TwoFactorEnabled,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar email: {Email}", request.Dto.Email);
            throw;
        }
    }
}
