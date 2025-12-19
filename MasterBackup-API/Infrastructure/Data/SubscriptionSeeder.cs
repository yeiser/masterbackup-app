using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Infrastructure.Data;

public static class SubscriptionSeeder
{
    public static async Task SeedFreeSubscriptionsAsync(MasterDbContext context)
    {
        // Obtener el plan FREE
        var freePlan = await context.Plans
            .Where(p => p.Name == "free")
            .FirstOrDefaultAsync();

        if (freePlan == null)
        {
            throw new InvalidOperationException("Free plan not found. Please run PlanSeeder first.");
        }

        // Obtener todos los tenants activos que no tienen suscripción activa
        var tenantsWithoutSubscription = await context.Tenants
            .Where(t => t.IsActive)
            .Where(t => !context.Subscriptions.Any(s => 
                s.TenantId == t.Id && 
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing) &&
                s.EndDate > DateTime.UtcNow))
            .ToListAsync();

        if (!tenantsWithoutSubscription.Any())
        {
            return; // Todos los tenants ya tienen suscripción
        }

        // Crear suscripciones FREE para cada tenant
        var subscriptions = new List<Subscription>();
        foreach (var tenant in tenantsWithoutSubscription)
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                PlanId = freePlan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(10), // Prácticamente sin límite
                Status = SubscriptionStatus.Active,
                BillingCycle = BillingCycle.Monthly,
                Amount = 0,
                Currency = "USD",
                AutoRenew = true,
                IsTrialUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            subscriptions.Add(subscription);
        }

        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();

        Console.WriteLine($"✓ Created {subscriptions.Count} FREE subscriptions for existing tenants");
    }
}
