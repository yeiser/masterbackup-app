using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, SubscriptionDto>
{
    private readonly MasterDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateSubscriptionCommandHandler(MasterDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<SubscriptionDto> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        // Verificar que el plan existe
        var plan = await _context.Plans.FindAsync(new object[] { request.PlanId }, cancellationToken);
        if (plan == null)
        {
            throw new KeyNotFoundException("Plan not found");
        }

        // Verificar si ya tiene una suscripción activa
        var existingSubscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && 
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSubscription != null)
        {
            throw new InvalidOperationException("You already have an active subscription");
        }

        // Verificar si ya usó el trial
        var trialUsed = await _context.Subscriptions
            .AnyAsync(s => s.TenantId == tenantId && s.IsTrialUsed, cancellationToken);

        if (request.UseTrial && trialUsed)
        {
            throw new InvalidOperationException("Trial period already used");
        }

        // Determinar el ciclo de facturación
        var billingCycle = request.BillingCycle.ToLower() == "yearly" 
            ? BillingCycle.Yearly 
            : BillingCycle.Monthly;

        // Calcular precio y fechas
        var amount = billingCycle == BillingCycle.Yearly ? plan.YearlyPrice : plan.MonthlyPrice;
        var startDate = DateTime.UtcNow;
        var endDate = billingCycle == BillingCycle.Yearly 
            ? startDate.AddYears(1) 
            : startDate.AddMonths(1);

        DateTime? trialEndDate = null;
        var status = SubscriptionStatus.Active;

        if (request.UseTrial && !trialUsed)
        {
            trialEndDate = startDate.AddDays(14); // 14 días de trial
            endDate = trialEndDate.Value.AddMonths(billingCycle == BillingCycle.Yearly ? 12 : 1);
            status = SubscriptionStatus.Trialing;
        }

        // Crear la suscripción
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = request.PlanId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            BillingCycle = billingCycle,
            Amount = amount,
            Currency = plan.Currency,
            AutoRenew = true,
            TrialEndDate = trialEndDate,
            IsTrialUsed = request.UseTrial,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            PlanId = subscription.PlanId,
            PlanName = plan.Name,
            PlanDisplayName = plan.DisplayName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status.ToString(),
            BillingCycle = subscription.BillingCycle.ToString(),
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            AutoRenew = subscription.AutoRenew,
            TrialEndDate = subscription.TrialEndDate,
            DaysRemaining = (int)(subscription.EndDate - DateTime.UtcNow).TotalDays
        };
    }
}
