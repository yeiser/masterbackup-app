using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        MasterDbContext masterContext,
        ITenantService tenantService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<LoginCommandHandler> logger)
    {
        _masterContext = masterContext;
        _tenantService = tenantService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user's tenant by email (we need to search across all tenants)
            var allTenants = await _masterContext.Tenants
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user != null && user.IsActive)
                {
                    var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);

                    if (!isPasswordValid)
                    {
                        return new AuthResponseDto
                        {
                            Success = false,
                            Message = "Invalid credentials"
                        };
                    }

                    // Check if 2FA is enabled
                    if (user.TwoFactorEnabled)
                    {
                        if (string.IsNullOrEmpty(request.TwoFactorCode))
                        {
                            // Generate and send 2FA code
                            var code = GenerateTwoFactorCode();
                            user.TwoFactorCode = code;
                            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                            await userManager.UpdateAsync(user);

                            await _emailService.SendTwoFactorCodeAsync(user.Email!, code);

                            return new AuthResponseDto
                            {
                                Success = false,
                                RequiresTwoFactor = true,
                                Message = "2FA code sent to your email"
                            };
                        }
                        else
                        {
                            // Verify 2FA code
                            if (user.TwoFactorCode != request.TwoFactorCode ||
                                user.TwoFactorCodeExpiry < DateTime.UtcNow)
                            {
                                return new AuthResponseDto
                                {
                                    Success = false,
                                    Message = "Invalid or expired 2FA code"
                                };
                            }

                            // Clear 2FA code
                            user.TwoFactorCode = null;
                            user.TwoFactorCodeExpiry = null;
                            await userManager.UpdateAsync(user);
                        }
                    }

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
            }

            return new AuthResponseDto
            {
                Success = false,
                Message = "Invalid credentials"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during login"
            };
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

    private string GenerateTwoFactorCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
}
