using FluentValidation;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Application.Common.Validators;

/// <summary>
/// Validador para expresiones CRON usando Quartz.NET
/// </summary>
public class CronExpressionValidator : AbstractValidator<string>
{
    private readonly IBackupSchedulerService _schedulerService;

    public CronExpressionValidator(IBackupSchedulerService schedulerService)
    {
        _schedulerService = schedulerService;

        RuleFor(cron => cron)
            .NotEmpty()
            .WithMessage("CRON expression is required.")
            .Must(BeValidCronExpression)
            .WithMessage("Invalid CRON expression format. " +
                "Use format: 'seconds minutes hours dayOfMonth month dayOfWeek [year]'. " +
                "Example: '0 0 2 * * ?' for daily at 2:00 AM.");
    }

    private bool BeValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        return _schedulerService.IsValidCronExpression(cronExpression);
    }
}
