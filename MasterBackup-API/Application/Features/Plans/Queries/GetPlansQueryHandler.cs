using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Plans.Queries;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, List<PlanDto>>
{
    private readonly MasterDbContext _context;

    public GetPlansQueryHandler(MasterDbContext context)
    {
        _context = context;
    }

    public async Task<List<PlanDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Plans
            .Include(p => p.Features)
            .AsQueryable();

        if (request.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        var plans = await query
            .OrderBy(p => p.DisplayOrder)
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
            .ToListAsync(cancellationToken);

        return plans;
    }
}
