using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Application.Common.Validators;

/// <summary>
/// Validador para UpdateBackupScheduleDto
/// </summary>
public class UpdateBackupScheduleDtoValidator : AbstractValidator<UpdateBackupScheduleDto>
{
    public UpdateBackupScheduleDtoValidator(IBackupSchedulerService schedulerService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CronExpression)
            .NotEmpty()
            .WithMessage("CRON expression is required.")
            .Must(cron => schedulerService.IsValidCronExpression(cron))
            .WithMessage("Invalid CRON expression. Examples: " +
                "'0 0 2 * * ?' = Daily at 2:00 AM, " +
                "'0 0 0 * * SUN' = Weekly on Sundays, " +
                "'0 0 0 1 * ?' = Monthly on day 1");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .WithMessage("Time zone is required.")
            .MaximumLength(50)
            .WithMessage("Time zone cannot exceed 50 characters.");

        RuleFor(x => x.RetentionDays)
            .GreaterThan(0)
            .WithMessage("Retention days must be greater than 0.")
            .LessThanOrEqualTo(3650)
            .WithMessage("Retention days cannot exceed 3650 (10 years).");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max retries cannot be negative.")
            .LessThanOrEqualTo(10)
            .WithMessage("Max retries cannot exceed 10.");

        RuleFor(x => x.TimeoutMinutes)
            .GreaterThan(0)
            .WithMessage("Timeout minutes must be greater than 0.")
            .LessThanOrEqualTo(1440)
            .WithMessage("Timeout minutes cannot exceed 1440 (24 hours).");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Priority must be between 0 and 10.")
            .LessThanOrEqualTo(10)
            .WithMessage("Priority must be between 0 and 10.");

        RuleFor(x => x.NotifyOnlyOnFailure)
            .Must((dto, notifyOnlyOnFailure) => {
                if (notifyOnlyOnFailure)
                {
                    return !dto.NotifyOnCompletion;
                }
                return true;
            })
            .WithMessage("When NotifyOnlyOnFailure is true, NotifyOnCompletion must be false.");
    }
}
