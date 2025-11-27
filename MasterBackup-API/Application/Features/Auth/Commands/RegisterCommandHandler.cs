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

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        MasterDbContext masterContext,
        ITenantService tenantService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterCommandHandler> logger)
    {
        _masterContext = masterContext;
        _tenantService = tenantService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if subdomain already exists
            var existingTenant = await _masterContext.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == request.Subdomain.ToLower(), cancellationToken);

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
                Name = request.TenantName,
                Subdomain = request.Subdomain.ToLower(),
                ConnectionString = string.Empty
            };

            // Create tenant database
            var connectionString = await _tenantService.CreateTenantDatabaseAsync(
                tenant.Id,
                tenant.Name,
                tenant.Subdomain
            );

            tenant.ConnectionString = connectionString;
            _masterContext.Tenants.Add(tenant);
            await _masterContext.SaveChangesAsync(cancellationToken);

            // Create user in tenant database
            var tenantOptions = await _tenantService.GetTenantDbContextOptionsAsync(tenant.Id);
            using var tenantContext = new TenantDbContext(tenantOptions);

            var userManager = CreateUserManager(tenantContext);

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = Domain.Enums.UserRole.Admin,
                TenantId = tenant.Id,
                TwoFactorEnabled = request.EnableTwoFactor,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            await _emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);

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
            _logger.LogError(ex, "Error during registration");
            return new AuthResponseDto
            {
                Success = false,
                Message = "An error occurred during registration"
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
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
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
