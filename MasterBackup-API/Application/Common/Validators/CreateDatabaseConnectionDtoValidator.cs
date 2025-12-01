using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Validators;

public class CreateDatabaseConnectionDtoValidator : AbstractValidator<CreateDatabaseConnectionDto>
{
    public CreateDatabaseConnectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Host)
            .NotEmpty().WithMessage("Host is required")
            .MaximumLength(255).WithMessage("Host must not exceed 255 characters");

        RuleFor(x => x.Port)
            .GreaterThan(0).WithMessage("Port must be greater than 0")
            .LessThanOrEqualTo(65535).WithMessage("Port must be less than or equal to 65535");

        RuleFor(x => x.Database)
            .NotEmpty().WithMessage("Database name is required")
            .MaximumLength(100).WithMessage("Database name must not exceed 100 characters");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MaximumLength(255).WithMessage("Password must not exceed 255 characters");

        RuleFor(x => x.EngineVersion)
            .MaximumLength(50).WithMessage("Engine Version must not exceed 50 characters");

        RuleFor(x => x.SSLMode)
            .MaximumLength(50).WithMessage("SSL Mode must not exceed 50 characters");

        // Worker Assignment validation
        When(x => x.AssignmentMode == Domain.Enums.WorkerAssignmentMode.Dedicated, () =>
        {
            RuleFor(x => x.AssignedWorkerId)
                .NotEmpty()
                .WithMessage("Worker asignado es requerido cuando el modo es Dedicado");
        });

        When(x => x.AssignmentMode == Domain.Enums.WorkerAssignmentMode.Auto, () =>
        {
            RuleFor(x => x.Tags)
                .NotEmpty()
                .WithMessage("Tags son requeridos cuando el modo es Auto");
        });
    }
}
