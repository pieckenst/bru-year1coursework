#if MODERN
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketSalesApp.Core.Data
{
    /// <summary>
    /// Database provider abstraction interface
    /// </summary>
    public interface IDatabaseProvider
    {
        string ProviderName { get; }
        string ConnectionString { get; }
        
        DbContext CreateContext();
        Task MigrateAsync();
        Task<bool> TestConnectionAsync();
        Task<string> GetVersionAsync();
        Task<Dictionary<string, object>> GetHealthInfoAsync();
    }
    
    /// <summary>
    /// Database provider factory for creating appropriate providers
    /// </summary>
    public interface IDatabaseProviderFactory
    {
        IDatabaseProvider CreateProvider(string providerName, string connectionString);
        IEnumerable<string> GetSupportedProviders();
    }
}
#endif