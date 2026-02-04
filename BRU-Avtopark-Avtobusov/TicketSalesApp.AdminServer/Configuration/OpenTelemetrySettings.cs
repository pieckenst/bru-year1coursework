namespace TicketSalesApp.AdminServer.Configuration;

public class OpenTelemetrySettings
{
    public const string SectionName = "OpenTelemetry";
    
    public string ServiceName { get; set; } = "TicketSales.AdminServer";
    public string ServiceVersion { get; set; } = "1.0.0";
    public JaegerSettings Jaeger { get; set; } = new();
    public ConsoleSettings Console { get; set; } = new();
    public SamplingSettings Sampling { get; set; } = new();
}

public class JaegerSettings
{
    public string AgentHost { get; set; } = "localhost";
    public int AgentPort { get; set; } = 6831;
    public string Endpoint { get; set; } = "http://localhost:14268/api/traces";
}

public class ConsoleSettings
{
    public bool Enabled { get; set; } = true;
}

public class SamplingSettings
{
    public double Ratio { get; set; } = 1.0;
}