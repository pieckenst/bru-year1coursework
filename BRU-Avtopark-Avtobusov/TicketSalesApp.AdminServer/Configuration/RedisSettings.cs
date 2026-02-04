namespace TicketSalesApp.AdminServer.Configuration;

public class RedisSettings
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "TicketSalesApp";
    public int DefaultDatabase { get; set; } = 0;
    public int CacheDatabase { get; set; } = 1;
    public int SessionDatabase { get; set; } = 2;
    public int SignalRDatabase { get; set; } = 3;
}