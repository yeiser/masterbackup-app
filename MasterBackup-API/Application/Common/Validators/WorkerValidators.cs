using FluentValidation;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Validators;

public class RegisterWorkerDtoValidator : AbstractValidator<RegisterWorkerDto>
{
    public RegisterWorkerDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del worker es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Hostname)
            .MaximumLength(100).WithMessage("El hostname no puede exceder 100 caracteres");

        RuleFor(x => x.OsInfo)
            .MaximumLength(200).WithMessage("La información del OS no puede exceder 200 caracteres");

        RuleFor(x => x.SupportedDatabaseTypes)
            .NotEmpty().WithMessage("Debe especificar al menos un tipo de base de datos soportado");

        RuleFor(x => x.MaxConcurrentJobs)
            .GreaterThan(0).WithMessage("El número máximo de jobs concurrentes debe ser mayor a 0")
            .LessThanOrEqualTo(10).WithMessage("El número máximo de jobs concurrentes no puede exceder 10");

        RuleFor(x => x.Version)
            .MaximumLength(20).WithMessage("La versión no puede exceder 20 caracteres");
    }
}

public class WorkerHeartbeatDtoValidator : AbstractValidator<WorkerHeartbeatDto>
{
    public WorkerHeartbeatDtoValidator()
    {
        RuleFor(x => x.WorkerId)
            .NotEmpty().WithMessage("El ID del worker es requerido");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es requerido")
            .Must(BeAValidStatus).WithMessage("Estado inválido. Valores permitidos: Online, Offline, Busy, Error, Maintenance");

        RuleFor(x => x.CurrentActiveJobs)
            .GreaterThanOrEqualTo(0).WithMessage("El número de jobs activos no puede ser negativo");

        RuleFor(x => x.TotalBackupsProcessed)
            .GreaterThanOrEqualTo(0).WithMessage("El total de backups procesados no puede ser negativo")
            .When(x => x.TotalBackupsProcessed.HasValue);

        RuleFor(x => x.TotalBytesProcessed)
            .GreaterThanOrEqualTo(0).WithMessage("El total de bytes procesados no puede ser negativo")
            .When(x => x.TotalBytesProcessed.HasValue);
    }

    private bool BeAValidStatus(string status)
    {
        var validStatuses = new[] { "Online", "Offline", "Busy", "Error", "Maintenance" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
