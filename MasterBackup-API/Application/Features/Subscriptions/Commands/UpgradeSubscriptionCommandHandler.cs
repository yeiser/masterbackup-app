using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class UpgradeSubscriptionCommandHandler : IRequestHandler<UpgradeSubscriptionCommand, SubscriptionDto>
{
    private readonly MasterDbContext _context;
    private readonly ITenantContext _tenantContext;

    public UpgradeSubscriptionCommandHandler(MasterDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<SubscriptionDto> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        // Obtener la suscripción actual
        var currentSubscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && 
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            .FirstOrDefaultAsync(cancellationToken);

        if (currentSubscription == null)
        {
            throw new InvalidOperationException("No active subscription found");
        }

        // Obtener el nuevo plan
        var newPlan = await _context.Plans.FindAsync(new object[] { request.NewPlanId }, cancellationToken);
        if (newPlan == null)
        {
            throw new KeyNotFoundException("New plan not found");
        }

        // Cancelar la suscripción actual
        currentSubscription.Status = SubscriptionStatus.Canceled;
        currentSubscription.CanceledAt = DateTime.UtcNow;
        currentSubscription.CancellationReason = "Upgraded to a different plan";
        currentSubscription.UpdatedAt = DateTime.UtcNow;

        // Determinar el ciclo de facturación
        var billingCycle = request.BillingCycle.ToLower() == "yearly" 
            ? BillingCycle.Yearly 
            : BillingCycle.Monthly;

        // Calcular precio y fechas
        var amount = billingCycle == BillingCycle.Yearly ? newPlan.YearlyPrice : newPlan.MonthlyPrice;
        var startDate = DateTime.UtcNow;
        var endDate = billingCycle == BillingCycle.Yearly 
            ? startDate.AddYears(1) 
            : startDate.AddMonths(1);

        // Crear la nueva suscripción
        var newSubscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = request.NewPlanId,
            StartDate = startDate,
            EndDate = endDate,
            Status = SubscriptionStatus.Active,
            BillingCycle = billingCycle,
            Amount = amount,
            Currency = newPlan.Currency,
            AutoRenew = true,
            IsTrialUsed = currentSubscription.IsTrialUsed,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(newSubscription);
        await _context.SaveChangesAsync(cancellationToken);

        return new SubscriptionDto
        {
            Id = newSubscription.Id,
            TenantId = newSubscription.TenantId,
            PlanId = newSubscription.PlanId,
            PlanName = newPlan.Name,
            PlanDisplayName = newPlan.DisplayName,
            StartDate = newSubscription.StartDate,
            EndDate = newSubscription.EndDate,
            Status = newSubscription.Status.ToString(),
            BillingCycle = newSubscription.BillingCycle.ToString(),
            Amount = newSubscription.Amount,
            Currency = newSubscription.Currency,
            AutoRenew = newSubscription.AutoRenew,
            DaysRemaining = (int)(newSubscription.EndDate - DateTime.UtcNow).TotalDays
        };
    }
}
