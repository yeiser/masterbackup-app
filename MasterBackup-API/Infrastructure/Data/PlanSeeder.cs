using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Infrastructure.Data;

public static class PlanSeeder
{
    public static async Task SeedPlansAsync(MasterDbContext context)
    {
        // Verificar si ya existen planes
        if (await context.Plans.AnyAsync())
        {
            return; // Ya hay datos
        }

        var plans = new List<Plan>
        {
            // Plan Free
            new Plan
            {
                Id = Guid.NewGuid(),
                Name = "free",
                DisplayName = "Free",
                Description = "Perfect for testing and small projects",
                MonthlyPrice = 0,
                YearlyPrice = 0,
                Currency = "USD",
                MaxDatabases = 1,
                MaxUsers = 1,
                MaxStorageGB = 5,
                BackupRetentionDays = 7,
                CloudStorageEnabled = false,
                ScheduledBackupsEnabled = false,
                ApiAccessEnabled = false,
                PrioritySupport = false,
                CustomBrandingEnabled = false,
                DisplayOrder = 1,
                IsActive = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow
            },
            
            // Plan Basic
            new Plan
            {
                Id = Guid.NewGuid(),
                Name = "basic",
                DisplayName = "Basic",
                Description = "For small teams getting started",
                MonthlyPrice = 29,
                YearlyPrice = 290,
                Currency = "USD",
                MaxDatabases = 5,
                MaxUsers = 3,
                MaxStorageGB = 50,
                BackupRetentionDays = 30,
                CloudStorageEnabled = true,
                ScheduledBackupsEnabled = true,
                ApiAccessEnabled = false,
                PrioritySupport = false,
                CustomBrandingEnabled = false,
                DisplayOrder = 2,
                IsActive = true,
                IsFeatured = true,
                BadgeText = "Most Popular",
                BadgeColor = "primary",
                CreatedAt = DateTime.UtcNow
            },
            
            // Plan Professional
            new Plan
            {
                Id = Guid.NewGuid(),
                Name = "pro",
                DisplayName = "Professional",
                Description = "For growing businesses with advanced needs",
                MonthlyPrice = 79,
                YearlyPrice = 790,
                Currency = "USD",
                MaxDatabases = 20,
                MaxUsers = 10,
                MaxStorageGB = 200,
                BackupRetentionDays = 90,
                CloudStorageEnabled = true,
                ScheduledBackupsEnabled = true,
                ApiAccessEnabled = true,
                PrioritySupport = true,
                CustomBrandingEnabled = true,
                DisplayOrder = 3,
                IsActive = true,
                IsFeatured = true,
                BadgeText = "Best Value",
                BadgeColor = "success",
                CreatedAt = DateTime.UtcNow
            },
            
            // Plan Enterprise
            new Plan
            {
                Id = Guid.NewGuid(),
                Name = "enterprise",
                DisplayName = "Enterprise",
                Description = "For large organizations with custom requirements",
                MonthlyPrice = 199,
                YearlyPrice = 1990,
                Currency = "USD",
                MaxDatabases = -1, // Unlimited
                MaxUsers = -1, // Unlimited
                MaxStorageGB = 1000,
                BackupRetentionDays = 365,
                CloudStorageEnabled = true,
                ScheduledBackupsEnabled = true,
                ApiAccessEnabled = true,
                PrioritySupport = true,
                CustomBrandingEnabled = true,
                DisplayOrder = 4,
                IsActive = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Plans.AddRange(plans);
        await context.SaveChangesAsync();

        // Agregar características para cada plan
        var planFeatures = new List<PlanFeature>();
        int featureOrder = 1;

        // Características del plan Free
        var freePlan = plans[0];
        planFeatures.AddRange(new[]
        {
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "1 Database", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "1 User", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "5 GB Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "7 Days Retention", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "Manual Backups", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = freePlan.Id, Name = "Email Support", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
        });

        // Características del plan Basic
        featureOrder = 1;
        var basicPlan = plans[1];
        planFeatures.AddRange(new[]
        {
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "5 Databases", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "3 Users", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "50 GB Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "30 Days Retention", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "Scheduled Backups", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "Cloud Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = basicPlan.Id, Name = "Priority Support", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
        });

        // Características del plan Professional
        featureOrder = 1;
        var proPlan = plans[2];
        planFeatures.AddRange(new[]
        {
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "20 Databases", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "10 Users", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "200 GB Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "90 Days Retention", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "Scheduled Backups", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "Cloud Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "API Access", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "Custom Branding", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = proPlan.Id, Name = "24/7 Support", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
        });

        // Características del plan Enterprise
        featureOrder = 1;
        var enterprisePlan = plans[3];
        planFeatures.AddRange(new[]
        {
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "Unlimited Databases", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "Unlimited Users", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "1 TB Storage", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "365 Days Retention", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "All Pro Features", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "Dedicated Support", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "SLA Guarantee", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "Custom Integrations", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
            new PlanFeature { Id = Guid.NewGuid(), PlanId = enterprisePlan.Id, Name = "On-Premise Option", IsIncluded = true, DisplayOrder = featureOrder++, CreatedAt = DateTime.UtcNow },
        });

        context.PlanFeatures.AddRange(planFeatures);
        await context.SaveChangesAsync();
    }
}
