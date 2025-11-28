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

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        MasterDbContext masterContext,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _masterContext = masterContext;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find invitation in master database
            var invitation = await _masterContext.UserInvitations
                .FirstOrDefaultAsync(i => i.InvitationToken == request.Token &&
                                        !i.IsAccepted &&
                                        i.ExpiresAt > DateTime.UtcNow,
                                        cancellationToken);

            if (invitation == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired invitation"
                };
            }

            // Get inviter to find tenant
            var inviter = await _masterContext.Users
                .FirstOrDefaultAsync(u => u.Id == invitation.InvitedByUserId, cancellationToken);

            if (inviter == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid invitation"
                };
            }

            // Create user in master database with same tenant as inviter
            var user = new ApplicationUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = invitation.Role,
                TenantId = inviter.TenantId,
                TwoFactorEnabled = request.EnableTwoFactor,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            invitation.IsAccepted = true;
            invitation.AcceptedAt = DateTime.UtcNow;
            await _masterContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendWelcomeEmailAsync(user);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
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
            _logger.LogError(ex, "Error accepting invitation");
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred while accepting the invitation"
            };
        }
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured")));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("TenantId", user.TenantId.ToString()),
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
