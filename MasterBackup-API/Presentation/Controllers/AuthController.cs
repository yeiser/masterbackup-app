using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<ValidateEmailDto> _validateEmailValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<Verify2FADto> _verify2FAValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
    private readonly IValidator<AcceptInvitationDto> _acceptInvitationValidator;
    private readonly IValidator<InviteUserDto> _inviteUserValidator;

    public AuthController(
        IMediator mediator, 
        ILogger<AuthController> logger,
        IValidator<RegisterDto> registerValidator,
        IValidator<ValidateEmailDto> validateEmailValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<Verify2FADto> verify2FAValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<AcceptInvitationDto> acceptInvitationValidator,
        IValidator<InviteUserDto> inviteUserValidator)
    {
        _mediator = mediator;
        _logger = logger;
        _registerValidator = registerValidator;
        _validateEmailValidator = validateEmailValidator;
        _loginValidator = loginValidator;
        _verify2FAValidator = verify2FAValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _acceptInvitationValidator = acceptInvitationValidator;
        _inviteUserValidator = inviteUserValidator;
    }

    /// <summary>
    /// Register a new tenant and admin user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var validationResult = await _registerValidator.ValidateAsync(registerDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new RegisterCommand(
            registerDto.Email,
            registerDto.Password,
            registerDto.FirstName,
            registerDto.LastName,
            registerDto.TenantName,
            registerDto.EnableTwoFactor
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Validate if email exists in the system
    /// </summary>
    [HttpPost("validate-email")]
    public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailDto validateEmailDto)
    {
        var validationResult = await _validateEmailValidator.ValidateAsync(validateEmailDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new ValidateEmailCommand(validateEmailDto);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var validationResult = await _loginValidator.ValidateAsync(loginDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new LoginCommand(loginDto);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.RequiresTwoFactor || result.TwoFactorRequired)
            {
                return Ok(new { twoFactorRequired = true, message = result.Message });
            }
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify 2FA code and complete login
    /// </summary>
    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto verify2FADto)
    {
        var validationResult = await _verify2FAValidator.ValidateAsync(verify2FADto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new Verify2FACommand(verify2FADto);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(forgotPasswordDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new ForgotPasswordCommand(forgotPasswordDto.Email);

        var (success, emailFound) = await _mediator.Send(command);

        if (!success)
        {
            return StatusCode(500, new { success = false, message = "Error al procesar la solicitud" });
        }

        if (!emailFound)
        {
            return NotFound(new { success = false, message = "No existe una cuenta registrada con este correo electrónico" });
        }

        return Ok(new { success = true, message = "Se ha enviado un correo con las instrucciones para restablecer tu contraseña" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(resetPasswordDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new ResetPasswordCommand("", resetPasswordDto.Token, resetPasswordDto.NewPassword);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Token de restablecimiento inválido o expirado" });
        }

        return Ok(new { message = "Contraseña restablecida exitosamente" });
    }

    /// <summary>
    /// Accept invitation and create account
    /// </summary>
    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto acceptInvitationDto)
    {
        var validationResult = await _acceptInvitationValidator.ValidateAsync(acceptInvitationDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var command = new AcceptInvitationCommand(
            acceptInvitationDto.Token,
            acceptInvitationDto.Password,
            acceptInvitationDto.FirstName,
            acceptInvitationDto.LastName,
            acceptInvitationDto.EnableTwoFactor
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    [Authorize]
    [HttpPost("enable-2fa")]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new Enable2FACommand(userId);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Error al habilitar 2FA" });
        }

        return Ok(new { message = "2FA habilitado exitosamente" });
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [Authorize]
    [HttpPost("disable-2fa")]
    public async Task<IActionResult> Disable2FA()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new Disable2FACommand(userId);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Error al deshabilitar 2FA" });
        }

        return Ok(new { message = "2FA deshabilitado exitosamente" });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var tenantId = User.FindFirst("TenantId")?.Value;

        return Ok(new
        {
            userId,
            email,
            role,
            tenantId
        });
    }
}
