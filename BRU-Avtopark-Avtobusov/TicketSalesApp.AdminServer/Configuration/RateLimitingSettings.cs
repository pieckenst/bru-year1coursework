namespace TicketSalesApp.AdminServer.Configuration;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";
    
    public int GlobalLimit { get; set; } = 1000;
    public int PerUserLimit { get; set; } = 100;
    public int WindowSizeInMinutes { get; set; } = 1;
    public Dictionary<string, int> Endpoints { get; set; } = new();
}