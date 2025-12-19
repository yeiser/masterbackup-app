using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Plans.Queries;

public class GetPlanByIdQueryHandler : IRequestHandler<GetPlanByIdQuery, PlanDto?>
{
    private readonly MasterDbContext _context;

    public GetPlanByIdQueryHandler(MasterDbContext context)
    {
        _context = context;
    }

    public async Task<PlanDto?> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var plan = await _context.Plans
            .Include(p => p.Features)
            .Where(p => p.Id == request.PlanId)
            .Select(p => new PlanDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                MonthlyPrice = p.MonthlyPrice,
                YearlyPrice = p.YearlyPrice,
                Currency = p.Currency,
                MaxDatabases = p.MaxDatabases,
                MaxUsers = p.MaxUsers,
                MaxStorageGB = p.MaxStorageGB,
                BackupRetentionDays = p.BackupRetentionDays,
                CloudStorageEnabled = p.CloudStorageEnabled,
                ScheduledBackupsEnabled = p.ScheduledBackupsEnabled,
                ApiAccessEnabled = p.ApiAccessEnabled,
                PrioritySupport = p.PrioritySupport,
                CustomBrandingEnabled = p.CustomBrandingEnabled,
                DisplayOrder = p.DisplayOrder,
                IsFeatured = p.IsFeatured,
                BadgeText = p.BadgeText,
                BadgeColor = p.BadgeColor,
                Features = p.Features
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new PlanFeatureDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Description = f.Description,
                        IsIncluded = f.IsIncluded,
                        Value = f.Value,
                        DisplayOrder = f.DisplayOrder
                    }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return plan;
    }
}
