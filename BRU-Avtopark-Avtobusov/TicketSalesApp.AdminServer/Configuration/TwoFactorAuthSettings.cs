namespace TicketSalesApp.AdminServer.Configuration;

public class TwoFactorAuthSettings
{
    public const string SectionName = "TwoFactorAuth";
    
    public string Issuer { get; set; } = "TicketSales";
    public int QRCodeSize { get; set; } = 200;
    public int RecoveryCodesCount { get; set; } = 10;
    public int TimeStepTolerance { get; set; } = 1;
}