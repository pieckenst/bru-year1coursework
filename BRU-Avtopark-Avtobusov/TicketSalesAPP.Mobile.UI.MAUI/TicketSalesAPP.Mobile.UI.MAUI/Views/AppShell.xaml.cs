using System.IdentityModel.Tokens.Jwt;
using Microsoft.Maui.Controls;
using TicketSalesAPP.Mobile.UI.MAUI.ViewModels;

namespace TicketSalesAPP.Mobile.UI.MAUI.Views
{
    public partial class AppShell : Shell
    {
        private bool _isAdmin;
        public bool IsAdmin 
        { 
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged();
                IsUser = !value;
            }
        }

        private bool _isUser;
        public bool IsUser 
        { 
            get => _isUser;
            set
            {
                _isUser = value;
                OnPropertyChanged();
            }
        }

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
            RegisterRoutes();
            CheckUserRole();
        }

        private void RegisterRoutes()
        {
            // Admin routes
            Routing.RegisterRoute("usermanagement", typeof(UserManagementPage));
            Routing.RegisterRoute("busmanagement", typeof(BusManagementPage));
            Routing.RegisterRoute("routemanagement", typeof(RouteManagementPage));
            Routing.RegisterRoute("routeschedules", typeof(RouteSchedulesPage));
            Routing.RegisterRoute("maintenance", typeof(MaintenancePage));
            Routing.RegisterRoute("employees", typeof(EmployeePage));
            Routing.RegisterRoute("jobs", typeof(JobPage));
            Routing.RegisterRoute("sales", typeof(SalesPage));
            Routing.RegisterRoute("statistics", typeof(StatisticsPage));

            // User routes
            Routing.RegisterRoute("ticketsearch", typeof(TicketSearchPage));
            Routing.RegisterRoute("schedule", typeof(SchedulePage));
            Routing.RegisterRoute("mytickets", typeof(MyTicketsPage));
            Routing.RegisterRoute("profile", typeof(ProfilePage));
        }

        private void CheckUserRole()
        {
            var token = SecureStorage.GetAsync("auth_token").Result;
            if (string.IsNullOrEmpty(token))
            {
                IsAdmin = false;
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
            IsAdmin = roleClaim?.Value == "1";
        }
    }
}
