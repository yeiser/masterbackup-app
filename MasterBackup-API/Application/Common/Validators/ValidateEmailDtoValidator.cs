using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Validators;

public class ValidateEmailDtoValidator : AbstractValidator<ValidateEmailDto>
{
    public ValidateEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}
