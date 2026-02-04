namespace TicketSalesApp.AdminServer.Configuration;

public class HangfireSettings
{
    public const string SectionName = "Hangfire";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string DashboardPath { get; set; } = "/hangfire";
    public int WorkerCount { get; set; } = 5;
    public string[] Queues { get; set; } = { "default", "exports", "notifications", "maintenance" };
}