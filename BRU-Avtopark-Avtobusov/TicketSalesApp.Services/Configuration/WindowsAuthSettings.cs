namespace TicketSalesApp.Services.Configuration
{
    public class WindowsAuthSettings
    {
        public bool Enabled { get; set; } = false;
        public bool AutoProvisionUsers { get; set; } = false;
        public int DefaultRole { get; set; } = 0;
        public string[] AllowedDomains { get; set; } = [];
    }
}
