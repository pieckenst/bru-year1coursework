namespace TicketSalesApp.AdminServer.Configuration;

public class WebAuthnSettings
{
    public const string SectionName = "WebAuthn";
    
    public string ServerDomain { get; set; } = "localhost";
    public string ServerName { get; set; } = "TicketSales Admin Server";
    public string[] Origins { get; set; } = { "https://localhost:5001", "http://localhost:5000" };
    public int TimestampDriftTolerance { get; set; } = 300000;
    public int ChallengeSize { get; set; } = 32;
}