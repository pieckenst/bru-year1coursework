namespace TicketSalesApp.AdminServer.Configuration;

public class SignalRSettings
{
    public const string SectionName = "SignalR";
    
    public bool UseRedisBackplane { get; set; } = true;
    public string RedisConnectionString { get; set; } = string.Empty;
    public string HubPath { get; set; } = "/hubs/notifications";
    public bool EnableDetailedErrors { get; set; } = false;
    public TimeSpan ClientTimeoutInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);
}