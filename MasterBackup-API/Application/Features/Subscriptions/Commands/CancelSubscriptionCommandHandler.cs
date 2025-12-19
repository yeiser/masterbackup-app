using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Unit>
{
    private readonly MasterDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CancelSubscriptionCommandHandler(MasterDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new UnauthorizedAccessException("Tenant not found");

        // Obtener la suscripción actual
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && 
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            throw new InvalidOperationException("No active subscription found");
        }

        // Cancelar la suscripción
        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.CancellationReason = request.Reason;
        subscription.AutoRenew = false;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
