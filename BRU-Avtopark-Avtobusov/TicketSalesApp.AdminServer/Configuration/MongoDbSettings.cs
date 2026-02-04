namespace TicketSalesApp.AdminServer.Configuration;

public class MongoDbSettings
{
    public const string SectionName = "MongoDB";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "ticketsales";
    public CollectionSettings Collections { get; set; } = new();
}

public class CollectionSettings
{
    public string Logs { get; set; } = "logs";
    public string Analytics { get; set; } = "analytics";
    public string Exports { get; set; } = "exports";
    public string Notifications { get; set; } = "notifications";
}