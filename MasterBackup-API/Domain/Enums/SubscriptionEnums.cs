namespace MasterBackup_API.Domain.Enums;

public enum SubscriptionStatus
{
    Active = 1,
    Trialing = 2,
    PastDue = 3,
    Canceled = 4,
    Expired = 5
}

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}
