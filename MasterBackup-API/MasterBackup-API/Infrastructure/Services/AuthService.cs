using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        MasterDbContext masterContext,
        ITenantService tenantService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _masterContext = masterContext;
        _tenantService = tenantService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Check if subdomain already exists
            var existingTenant = await _masterContext.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == registerDto.Subdomain.ToLower());

            if (existingTenant != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Subdomain already exists"
                };
            }

            // Create tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = registerDto.TenantName,
                Subdomain = registerDto.Subdomain.ToLower(),
                ConnectionString = string.Empty // Will be set after database creation
            };

            // Create tenant database
            var connectionString = await _tenantService.CreateTenantDatabaseAsync(
                tenant.Id,
                tenant.Name,
                tenant.Subdomain
            );

            tenant.ConnectionString = connectionString;
            _masterContext.Tenants.Add(tenant);
            await _masterContext.SaveChangesAsync();

            // Create user in tenant database
            var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
            using var tenantContext = new TenantDbContext(tenantOptions);

            var userManager = CreateUserManager(tenantContext);

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = UserRole.Admin,
                TenantId = tenant.Id,
                TwoFactorEnabled = registerDto.EnableTwoFactor,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            // Find user's tenant by email (we need to search across all tenants or use a user lookup table)
            // For simplicity, we'll implement a basic approach - in production, consider a user-tenant mapping table
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByEmailAsync(loginDto.Email);

                if (user != null && user.IsActive)
                {
                    var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);

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
                        if (string.IsNullOrEmpty(loginDto.TwoFactorCode))
                        {
                            // Generate and send 2FA code
                            var code = GenerateTwoFactorCode();
                            user.TwoFactorCode = code;
                            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                            await userManager.UpdateAsync(user);

                            await _emailService.SendTwoFactorCodeAsync(user.Email, code);

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
                            if (user.TwoFactorCode != loginDto.TwoFactorCode ||
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
                            Email = user.Email,
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

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByEmailAsync(forgotPasswordDto.Email);

                if (user != null && user.IsActive)
                {
                    var resetToken = GenerateSecureToken();
                    user.PasswordResetToken = resetToken;
                    user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                    await userManager.UpdateAsync(user);

                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
                    return true;
                }
            }

            // Return true even if user not found (security best practice)
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

                if (user != null &&
                    user.PasswordResetToken == resetPasswordDto.Token &&
                    user.PasswordResetTokenExpiry > DateTime.UtcNow)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, token, resetPasswordDto.NewPassword);

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

    public async Task<bool> InviteUserAsync(InviteUserDto inviteUserDto, string invitedByUserId, Guid tenantId)
    {
        try
        {
            var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenantId);
            using var tenantContext = new TenantDbContext(tenantOptions);

            var userManager = CreateUserManager(tenantContext);

            // Check if user already exists
            var existingUser = await userManager.FindByEmailAsync(inviteUserDto.Email);
            if (existingUser != null)
            {
                return false;
            }

            // Check if there's a pending invitation
            var existingInvitation = await tenantContext.UserInvitations
                .FirstOrDefaultAsync(i => i.Email == inviteUserDto.Email && !i.IsAccepted);

            if (existingInvitation != null)
            {
                return false;
            }

            var inviter = await userManager.FindByIdAsync(invitedByUserId);

            var invitation = new UserInvitation
            {
                Id = Guid.NewGuid(),
                Email = inviteUserDto.Email,
                Role = inviteUserDto.Role,
                InvitationToken = GenerateSecureToken(),
                InvitedByUserId = invitedByUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            tenantContext.UserInvitations.Add(invitation);
            await tenantContext.SaveChangesAsync();

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

    public async Task<AuthResponseDto> AcceptInvitationAsync(AcceptInvitationDto acceptInvitationDto)
    {
        try
        {
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var invitation = await tenantContext.UserInvitations
                    .FirstOrDefaultAsync(i => i.InvitationToken == acceptInvitationDto.Token &&
                                            !i.IsAccepted &&
                                            i.ExpiresAt > DateTime.UtcNow);

                if (invitation != null)
                {
                    var userManager = CreateUserManager(tenantContext);

                    var user = new ApplicationUser
                    {
                        UserName = invitation.Email,
                        Email = invitation.Email,
                        FirstName = acceptInvitationDto.FirstName,
                        LastName = acceptInvitationDto.LastName,
                        Role = invitation.Role,
                        TenantId = tenant.Id,
                        TwoFactorEnabled = acceptInvitationDto.EnableTwoFactor,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, acceptInvitationDto.Password);

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
                    await tenantContext.SaveChangesAsync();

                    await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

                    var token = GenerateJwtToken(user);

                    return new AuthResponseDto
                    {
                        Success = true,
                        Token = token,
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
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
                Message = "Invalid or expired invitation"
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

    public async Task<bool> Enable2FAAsync(string userId)
    {
        try
        {
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    user.TwoFactorEnabled = true;
                    await userManager.UpdateAsync(user);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling 2FA");
            return false;
        }
    }

    public async Task<bool> Disable2FAAsync(string userId)
    {
        try
        {
            var allTenants = await _masterContext.Tenants.Where(t => t.IsActive).ToListAsync();

            foreach (var tenant in allTenants)
            {
                var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
                using var tenantContext = new TenantDbContext(tenantOptions);

                var userManager = CreateUserManager(tenantContext);
                var user = await userManager.FindByIdAsync(userId);

                if (user != null)
                {
                    user.TwoFactorEnabled = false;
                    user.TwoFactorCode = null;
                    user.TwoFactorCodeExpiry = null;
                    await userManager.UpdateAsync(user);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA");
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

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
