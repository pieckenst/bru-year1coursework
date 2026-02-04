using Hangfire.Dashboard;

namespace TicketSalesApp.AdminServer.Configuration
{
    /// <summary>
    /// Authorization filter for Hangfire dashboard
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // In development, allow access
            if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return true;
            }
            
            // In production, require authentication and admin role
            return httpContext.User.Identity?.IsAuthenticated == true &&
                   (httpContext.User.IsInRole("Admin") || 
                    httpContext.User.HasClaim("role", "Admin"));
        }
    }
}