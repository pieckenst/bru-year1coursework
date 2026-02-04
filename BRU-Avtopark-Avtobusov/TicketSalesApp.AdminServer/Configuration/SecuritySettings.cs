namespace TicketSalesApp.AdminServer.Configuration;

public class SecuritySettings
{
    public const string SectionName = "Security";
    
    public bool RequireHttps { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000;
    public AccountLockoutSettings AccountLockout { get; set; } = new();
}

public class AccountLockoutSettings
{
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
}