namespace TicketSalesApp.AdminServer.Configuration;

public class CachingSettings
{
    public const string SectionName = "Caching";
    
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, TimeSpan> Policies { get; set; } = new();
}