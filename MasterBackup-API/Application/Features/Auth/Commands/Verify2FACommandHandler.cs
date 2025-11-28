using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Verify2FACommandHandler> _logger;

    public Verify2FACommandHandler(
        MasterDbContext masterContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<Verify2FACommandHandler> logger)
    {
        _masterContext = masterContext;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("2FA verification attempt for email: {Email}", request.Dto.Email);

            // Find user by email (email is unique globally)
            var user = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Dto.Email && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive: {Email}", request.Dto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid credentials"
                };
            }

            // Verificar contraseña
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Dto.Password);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Invalid password for user: {Email}", request.Dto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Credenciales inválidas"
                };
            }

            // Verificar código 2FA
            if (string.IsNullOrEmpty(user.TwoFactorCode) ||
                user.TwoFactorCode != request.Dto.TwoFactorCode ||
                user.TwoFactorCodeExpiry == null ||
                user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired 2FA code for user: {Email}", request.Dto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Código 2FA inválido o expirado"
                };
            }

            // Limpiar código 2FA
            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            await _userManager.UpdateAsync(user);

            // Generar token JWT
            var token = GenerateJwtToken(user, user.TenantId);

            _logger.LogInformation("2FA verification successful for user: {Email}", request.Dto.Email);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                TenantId = user.TenantId.ToString(),
                TwoFactorRequired = false,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA verification");
            return new AuthResponseDto
            {
                Success = false,
                Message = "Ocurrió un error durante la verificación 2FA"
            };
        }
    }

    private string GenerateJwtToken(ApplicationUser user, Guid tenantId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured")));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("TenantId", tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
