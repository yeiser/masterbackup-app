using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters");

        RuleFor(x => x.TwoFactorCode)
            .Matches(@"^\d{6}$").WithMessage("2FA code must be 6 digits")
            .When(x => !string.IsNullOrEmpty(x.TwoFactorCode));
    }
}
