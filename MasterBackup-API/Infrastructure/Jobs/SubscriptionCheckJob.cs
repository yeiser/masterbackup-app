using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MasterBackup_API.Infrastructure.Jobs;

/// <summary>
/// Job que verifica y actualiza el estado de las suscripciones
/// Se ejecuta diariamente a las 3:00 AM
/// </summary>
[DisallowConcurrentExecution]
public class SubscriptionCheckJob : IJob
{
    private readonly ILogger<SubscriptionCheckJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SubscriptionCheckJob(ILogger<SubscriptionCheckJob> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting subscription check job at {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var masterContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        try
        {
            var now = DateTime.UtcNow;
            
            // 1. Marcar suscripciones expiradas
            var expiredSubscriptions = await masterContext.Subscriptions
                .Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing) &&
                           s.EndDate <= now)
                .ToListAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                _logger.LogInformation(
                    "Marking subscription {SubscriptionId} as expired for tenant {TenantId}",
                    subscription.Id, subscription.TenantId);

                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = now;
            }

            // 2. Finalizar trials y convertir a suscripciones regulares o expirarlas
            var trialSubscriptions = await masterContext.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Trialing &&
                           s.TrialEndDate.HasValue &&
                           s.TrialEndDate.Value <= now)
                .ToListAsync();

            foreach (var subscription in trialSubscriptions)
            {
                _logger.LogInformation(
                    "Trial ended for subscription {SubscriptionId} of tenant {TenantId}",
                    subscription.Id, subscription.TenantId);

                // Si AutoRenew está activado, convertir a Active
                if (subscription.AutoRenew)
                {
                    subscription.Status = SubscriptionStatus.Active;
                    _logger.LogInformation("Converting trial to active subscription {SubscriptionId}", subscription.Id);
                }
                else
                {
                    subscription.Status = SubscriptionStatus.Expired;
                    _logger.LogInformation("Expiring trial subscription {SubscriptionId}", subscription.Id);
                }
                
                subscription.UpdatedAt = now;
            }

            // 3. Identificar suscripciones próximas a vencer (7 días antes)
            var expiringDate = now.AddDays(7);
            var expiringSubscriptions = await masterContext.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active &&
                           s.EndDate > now &&
                           s.EndDate <= expiringDate &&
                           s.AutoRenew)
                .ToListAsync();

            foreach (var subscription in expiringSubscriptions)
            {
                var daysRemaining = (int)(subscription.EndDate - now).TotalDays;
                _logger.LogInformation(
                    "Subscription {SubscriptionId} for tenant {TenantId} expires in {Days} days",
                    subscription.Id, subscription.TenantId, daysRemaining);

                // TODO: Aquí se podría enviar una notificación al tenant
                // sobre la próxima renovación
            }

            // 4. Guardar cambios
            if (expiredSubscriptions.Any() || trialSubscriptions.Any())
            {
                await masterContext.SaveChangesAsync();
                _logger.LogInformation(
                    "Subscription check completed. Expired: {Expired}, Trial ended: {Trial}",
                    expiredSubscriptions.Count, trialSubscriptions.Count);
            }
            else
            {
                _logger.LogInformation("No subscriptions require updates");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during subscription check job execution");
            throw;
        }
    }
}
