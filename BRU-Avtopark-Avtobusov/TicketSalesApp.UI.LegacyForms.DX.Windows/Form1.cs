using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraBars; // Required for BarItem, ItemClickEventArgs
using DevExpress.XtraBars.Ribbon; // Required for RibbonForm
using NLog;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // Inherit from RibbonForm instead of XtraForm
    public partial class Form1 : RibbonForm 
    {
        // Add static logger field
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Form1(bool loginresult)
        {
            InitializeComponent();

            if (!loginresult)
            {
                // Optional: Handle unsuccessful login, maybe close the app or show a message
                // For now, just preventing menu handlers from being attached if login failed.
                 this.Load += (s, e) => Close(); 
                return; 
            }

            // Wire up menu item clicks programmatically
            WireUpEventHandlers();

            // Set status bar info from ApiClientService
            bsiCompanyName.Caption = "Название компании: Your Company";

            // Apply role-based permissions
            ApplyPermissions();

            // Set user info in status bar
            UpdateStatusBarUserInfo();
        }

        private void WireUpEventHandlers()
        {
             // --- Wire up Ribbon BarButtonItems ---
             if (this.bbiBuses != null) 
                 this.bbiBuses.ItemClick += BaseItem1_Click; // Buses
             if (this.bbiRoutes != null)
                 this.bbiRoutes.ItemClick += BaseItem4_Click; // Routes
             if (this.bbiEmployees != null)
                 this.bbiEmployees.ItemClick += BaseItem5_Click; // Employees
             if (this.bbiJobs != null)
                 this.bbiJobs.ItemClick += BaseItem2_Click; // Jobs
             if (this.bbiMaintenance != null)
                 this.bbiMaintenance.ItemClick += BaseItem3_Click; // Maintenance
             if (this.bbiRouteSchedules != null) 
                 this.bbiRouteSchedules.ItemClick += BaseItem6_Click; // Route Schedules
             if (this.bbiTickets != null)
                 this.bbiTickets.ItemClick += MenuItemTicketManagement_Click; // Tickets
             if (this.bbiSales != null)
                 this.bbiSales.ItemClick += MenuItemSalesManagement_Click; // Sales
             if (this.bbiIncomeReport != null)
                 this.bbiIncomeReport.ItemClick += MenuItemIncomeReport_Click; // Income Report
             if (this.bbiSalesStatistics != null)
                 this.bbiSalesStatistics.ItemClick += MenuItemSalesStatistics_Click; // Sales Statistics

             // --- Wire up Application Menu Items ---
             if (this.bbiAbout != null)
                 this.bbiAbout.ItemClick += оПрограммеToolStripMenuItem_Click; // About
             if (this.bbiExit != null)
                 this.bbiExit.ItemClick += ExitApplication_Click; // Exit

            // --- Wire up NavBar Items (Optional but good for consistency) ---
            // Note: These often mirror Ribbon items. Ensure names match designer.
            // Example: If navBarItem_BusManagement exists and should open Bus Management:
            // if (this.navBarItem_BusManagement != null)
            //    this.navBarItem_BusManagement.LinkClicked += (s, e) => ShowMdiChildForm(new frmBusManagement()); 
            // Add similar handlers for other relevant NavBarItems if you keep the NavBarControl fully functional.


            // --- Existing Inventory/System Admin (Map if needed) ---
            // Example: Map User Management if a corresponding BarButtonItem 'bbiUserManagement' exists
            // if (this.bbiUserManagement != null)
            //     this.bbiUserManagement.ItemClick += TsbUserManage_Click; 

            // Add handlers for Inventory Items if corresponding BarButtonItems exist
            // if (this.bbiStockIn != null) this.bbiStockIn.ItemClick += YourStockInHandler;
            // if (this.bbiStockOut != null) this.bbiStockOut.ItemClick += YourStockOutHandler;
            // ... etc ...
        }

        // --- Event Handler Methods (Reused from original) ---

        private void BaseItem1_Click(object sender, ItemClickEventArgs e) // Buses (Triggered by bbiBuses)
        {
            ShowMdiChildForm(new frmBusManagement());
        }

        private void BaseItem4_Click(object sender, ItemClickEventArgs e) // Routes (Triggered by bbiRoutes)
        {
            ShowMdiChildForm(new frmRouteManagement());
        }

        private void BaseItem5_Click(object sender, ItemClickEventArgs e) // Employees (Triggered by bbiEmployees)
        {
            ShowMdiChildForm(new frmEmployeeManagement());
        }

        private void BaseItem2_Click(object sender, ItemClickEventArgs e) // Jobs (Triggered by bbiJobs)
        {
            ShowMdiChildForm(new frmJobManagement());
        }

        private void BaseItem3_Click(object sender, ItemClickEventArgs e) // Maintenance (Triggered by bbiMaintenance)
        {
            ShowMdiChildForm(new frmMaintenanceManagement());
        }

        private void BaseItem6_Click(object sender, ItemClickEventArgs e) // Route Schedules (Triggered by bbiRouteSchedules)
        {
            ShowMdiChildForm(new frmRouteSchedulesManagement());
        }

        private void MenuItemTicketManagement_Click(object sender, ItemClickEventArgs e) // Ticket Management (Triggered by bbiTickets)
        {
            ShowMdiChildForm(new frmTicketManagement());
        }
        
        private void MenuItemSalesManagement_Click(object sender, ItemClickEventArgs e) // Sales (Triggered by bbiSales)
        {
             ShowMdiChildForm(new frmSalesManagement());
        }

        private void MenuItemSalesStatistics_Click(object sender, ItemClickEventArgs e) // Sales Statistics (Triggered by bbiSalesStatistics)
        {
            ShowMdiChildForm(new frmSalesStatistics());
        }

        private void MenuItemIncomeReport_Click(object sender, ItemClickEventArgs e) // Income Report (Triggered by bbiIncomeReport)
        {
            ShowMdiChildForm(new frmIncomeReport());
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, ItemClickEventArgs e) // About (Triggered by bbiAbout)
        {
            ShowMdiChildForm(new AboutWindow());
        }

        private void ExitApplication_Click(object sender, ItemClickEventArgs e) // Exit (Triggered by bbiExit)
        {
             Application.Exit();
        }

        // Placeholder for User Management click (if implemented)
        private void TsbUserManage_Click(object sender, ItemClickEventArgs e) 
        {
             // Example: ShowMdiChildForm(new frmUserManagement());
             MessageBox.Show("User Management Clicked (Not Implemented)");
        }

        // Placeholder for Inventory clicks (if implemented)
        private void 库存业务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // This handler might become obsolete if using Ribbon items directly
             MessageBox.Show("Inventory Business Clicked (Not Implemented via Menu)");
        }
        

        // --- Helper Method to Show MDI Child Forms (Prevents Duplicates) ---
        
        private void ShowMdiChildForm(Form childForm)
        {
            // Check if a form of the same type is already open
            foreach (Form openForm in this.MdiChildren)
            {
                if (openForm.GetType() == childForm.GetType())
                {
                    openForm.MdiParent = this; // Ensure it's still parented correctly
                    openForm.Activate(); // Bring existing form to front
                    childForm.Dispose(); // Dispose the new instance we were about to show
                    return;
                }
            }

            // If not open, set MdiParent and show
            childForm.MdiParent = this;
            childForm.Show();
        }

        // --- Apply Permissions Based on Role ---
        private void ApplyPermissions()
        {
            var role = ApiClientService.Instance.UserRole;
            Log.Debug("Applying permissions for role: {0}", role.HasValue ? role.Value.ToString() : "None");

            // Role: 0 = regular user (non-employee, ticket buying/selling only)
            // Role: 1 = admin (employee, full access)
            bool isAdmin = role.HasValue && role.Value == 1;
            bool isEmployee = isAdmin; // In this system, admins = employees

            Log.Info("Permission check - IsAdmin: {0}, IsEmployee: {1}, Role value: {2}", 
                isAdmin, isEmployee, role.HasValue ? role.Value.ToString() : "NULL");

            // === EMPLOYEE/ADMIN FUNCTIONS (Role 1) ===
            // These buttons are for managing company operations - employees only
            if (bbiBuses != null) {
                bbiBuses.Enabled = isEmployee;
                Log.Debug("bbiBuses enabled: {0}", isEmployee);
            }
            if (bbiRoutes != null) {
                bbiRoutes.Enabled = isEmployee;
                Log.Debug("bbiRoutes enabled: {0}", isEmployee);
            }
            if (bbiEmployees != null) {
                bbiEmployees.Enabled = isEmployee;
                Log.Debug("bbiEmployees enabled: {0}", isEmployee);
            }
            if (bbiJobs != null) {
                bbiJobs.Enabled = isEmployee;
                Log.Debug("bbiJobs enabled: {0}", isEmployee);
            }
            if (bbiMaintenance != null) {
                bbiMaintenance.Enabled = isEmployee;
                Log.Debug("bbiMaintenance enabled: {0}", isEmployee);
            }
            if (bbiRouteSchedules != null) {
                bbiRouteSchedules.Enabled = isEmployee;
                Log.Debug("bbiRouteSchedules enabled: {0}", isEmployee);
            }
            if (bbiSalesStatistics != null) {
                bbiSalesStatistics.Enabled = isEmployee;
                Log.Debug("bbiSalesStatistics enabled: {0}", isEmployee);
            }

            // === SYSTEM ADMIN FUNCTIONS (Role 1 only) ===
            // User management and permissions are admin-only
            if (bbiUserManagement != null) {
                bbiUserManagement.Enabled = isAdmin;
                Log.Debug("bbiUserManagement enabled: {0}", isAdmin);
            }
            if (bbiPermissions != null) {
                bbiPermissions.Enabled = isAdmin;
                Log.Debug("bbiPermissions enabled: {0}", isAdmin);
            }

            // === CUSTOMER FUNCTIONS (All users) ===
            // Ticket buying and schedule viewing - available to everyone
            if (bbiTickets != null) {
                bbiTickets.Enabled = true; // Everyone can manage tickets
                Log.Debug("bbiTickets enabled: true (public)");
            }
            if (bbiSales != null) {
                bbiSales.Enabled = true; // Everyone can view/make sales
                Log.Debug("bbiSales enabled: true (public)");
            }
            if (bbiIncomeReport != null) {
                bbiIncomeReport.Enabled = isEmployee; // Financial reports for employees only
                Log.Debug("bbiIncomeReport enabled: {0}", isEmployee);
            }

            // --- Hide Entire Ribbon Pages for non-employees ---
            // System Admin Page (ribbonPage4) - admin only
            if (ribbonPage4 != null) {
                ribbonPage4.Visible = isAdmin;
                Log.Debug("ribbonPage4 (System Admin) visible: {0}", isAdmin);
            }

            // Inventory Management Page (ribbonPage3) - employees only
            if (ribbonPage3 != null) {
                ribbonPage3.Visible = isEmployee;
                Log.Debug("ribbonPage3 (Inventory) visible: {0}", isEmployee);
            }

            Log.Info("Permissions applied successfully.");
        }

        // --- Update Status Bar --- 
        private void UpdateStatusBarUserInfo()
        {
           string userName = ApiClientService.Instance.UserName;
           string roleDescription = "Пользователь"; // Default
           if (ApiClientService.Instance.UserRole.HasValue)
           {
               if (ApiClientService.Instance.UserRole.Value == 1) {
                   roleDescription = "Администратор";
               } // Add other roles if needed
           }

           bsiUserID.Caption = string.Format("Пользователь: {0} ({1})", 
               string.IsNullOrEmpty(userName) ? "[Неизвестно]" : userName, 
               roleDescription);
        }

    }
}