using Microsoft.AspNetCore.Authorization;
using TicketSalesApp.AdminServer.Authorization;

namespace TicketSalesApp.AdminServer.Configuration
{
    /// <summary>
    /// Configuration for authorization policies
    /// </summary>
    public static class AuthorizationPolicies
    {
        // Policy names
        public const string AdminOnly = "AdminOnly";
        public const string CanManageBuses = "CanManageBuses";
        public const string CanManageRoutes = "CanManageRoutes";
        public const string CanManageTickets = "CanManageTickets";
        public const string CanManageUsers = "CanManageUsers";
        public const string CanManageEmployees = "CanManageEmployees";
        public const string CanManageJobs = "CanManageJobs";
        public const string CanManageMaintenance = "CanManageMaintenance";
        public const string CanViewReports = "CanViewReports";
        public const string CanManageReports = "CanManageReports";
        public const string CanManageRoles = "CanManageRoles";
        public const string CanManagePermissions = "CanManagePermissions";
        public const string CanViewSales = "CanViewSales";
        public const string CanManageSales = "CanManageSales";
        public const string CanExportData = "CanExportData";
        public const string CanViewDashboard = "CanViewDashboard";

        // Permission names (matching the seeded permissions)
        public static class Permissions
        {
            // User Management
            public const string ViewUsers = "View Users";
            public const string CreateUsers = "Create Users";
            public const string EditUsers = "Edit Users";
            public const string DeleteUsers = "Delete Users";

            // Bus Management
            public const string ViewBuses = "View Buses";
            public const string CreateBuses = "Create Buses";
            public const string EditBuses = "Edit Buses";
            public const string DeleteBuses = "Delete Buses";

            // Route Management
            public const string ViewRoutes = "View Routes";
            public const string CreateRoutes = "Create Routes";
            public const string EditRoutes = "Edit Routes";
            public const string DeleteRoutes = "Delete Routes";

            // Ticket Management
            public const string ViewTickets = "View Tickets";
            public const string CreateTickets = "Create Tickets";
            public const string EditTickets = "Edit Tickets";
            public const string DeleteTickets = "Delete Tickets";

            // Sales Management
            public const string ViewSales = "View Sales";
            public const string CreateSales = "Create Sales";
            public const string EditSales = "Edit Sales";
            public const string DeleteSales = "Delete Sales";

            // Maintenance Management
            public const string ViewMaintenance = "View Maintenance";
            public const string CreateMaintenance = "Create Maintenance";
            public const string EditMaintenance = "Edit Maintenance";
            public const string DeleteMaintenance = "Delete Maintenance";

            // Reports
            public const string ViewReports = "View Reports";
            public const string CreateReports = "Create Reports";
            public const string EditReports = "Edit Reports";
            public const string DeleteReports = "Delete Reports";

            // Employee Management
            public const string ViewEmployees = "View Employees";
            public const string CreateEmployees = "Create Employees";
            public const string EditEmployees = "Edit Employees";
            public const string DeleteEmployees = "Delete Employees";

            // Role Management
            public const string ViewRoles = "View Roles";
            public const string CreateRoles = "Create Roles";
            public const string EditRoles = "Edit Roles";
            public const string DeleteRoles = "Delete Roles";
        }

        /// <summary>
        /// Configure authorization policies
        /// </summary>
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Admin-only policy (legacy compatibility)
            options.AddPolicy(AdminOnly, policy =>
                policy.Requirements.Add(new AdminOnlyRequirement()));

            // Bus management policies
            options.AddPolicy(CanManageBuses, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateBuses)));

            // Route management policies
            options.AddPolicy(CanManageRoutes, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateRoutes)));

            // Ticket management policies
            options.AddPolicy(CanManageTickets, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateTickets)));

            // User management policies
            options.AddPolicy(CanManageUsers, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateUsers)));

            // Employee management policies
            options.AddPolicy(CanManageEmployees, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateEmployees)));

            // Job management policies (using employee permissions for now)
            options.AddPolicy(CanManageJobs, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateEmployees)));

            // Maintenance management policies
            options.AddPolicy(CanManageMaintenance, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateMaintenance)));

            // Report viewing policies
            options.AddPolicy(CanViewReports, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ViewReports)));

            // Report management policies
            options.AddPolicy(CanManageReports, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateReports)));

            // Role management policies
            options.AddPolicy(CanManageRoles, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateRoles)));

            // Permission management policies (admin only for now)
            options.AddPolicy(CanManagePermissions, policy =>
                policy.Requirements.Add(new AdminOnlyRequirement()));

            // Sales viewing policies
            options.AddPolicy(CanViewSales, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ViewSales)));

            // Sales management policies
            options.AddPolicy(CanManageSales, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.CreateSales)));

            // Data export policies (admin or users with report permissions)
            options.AddPolicy(CanExportData, policy =>
                policy.Requirements.Add(new PermissionRequirement(Permissions.ViewReports)));

            // Dashboard viewing (any authenticated user with view permissions)
            options.AddPolicy(CanViewDashboard, policy =>
                policy.RequireAuthenticatedUser());
        }

        /// <summary>
        /// Get all policy names for documentation/testing
        /// </summary>
        public static IEnumerable<string> GetAllPolicyNames()
        {
            return new[]
            {
                AdminOnly,
                CanManageBuses,
                CanManageRoutes,
                CanManageTickets,
                CanManageUsers,
                CanManageEmployees,
                CanManageJobs,
                CanManageMaintenance,
                CanViewReports,
                CanManageReports,
                CanManageRoles,
                CanManagePermissions,
                CanViewSales,
                CanManageSales,
                CanExportData,
                CanViewDashboard
            };
        }

        /// <summary>
        /// Get all permission names for documentation/testing
        /// </summary>
        public static IEnumerable<string> GetAllPermissionNames()
        {
            return typeof(Permissions)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => f.GetValue(null) as string)
                .Where(v => !string.IsNullOrEmpty(v))
                .Cast<string>();
        }
    }
}