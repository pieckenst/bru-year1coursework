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

            bool isAdmin = role.HasValue && role.Value == 1;

            // --- Ribbon Button Visibility/Enabled State --- 

            // Assumed Admin-Only functions
            if (bbiUserManagement != null) bbiUserManagement.Enabled = isAdmin;
            if (bbiPermissions != null) bbiPermissions.Enabled = isAdmin;
            if (bbiJobs != null) bbiJobs.Enabled = isAdmin; // Example: Only admins manage job titles
            // Add other admin-only buttons here...
            // Example: Potentially Maintenance, Sales Statistics etc might be admin only
            if (bbiMaintenance != null) bbiMaintenance.Enabled = isAdmin;
            if (bbiSalesStatistics != null) bbiSalesStatistics.Enabled = isAdmin;

            // --- Potentially Hide Entire Ribbon Pages/Groups for non-admins ---
            // Example: Hide System Admin Page for non-admins
            if (ribbonPage4 != null) ribbonPage4.Visible = isAdmin;

            // Example: Hide Inventory Management Page (if permissions require)
            if (ribbonPage3 != null) ribbonPage3.Visible = isAdmin; // Or based on specific role

            // Example: Maybe regular users can only view tickets/schedules but not manage them
            // You might need finer control than just enabling/disabling top-level buttons.
            // This might involve passing the role to the child forms themselves.

            // --- NavBar Control Item Visibility/Enabled State (Example) ---
            /*
            if (navBarControl2 != null)
            {
               // Example: Hide System Admin group
               // Find NavBarGroup by Caption or Name
               var adminGroup = navBarControl2.Groups.FirstOrDefault(g => g.Caption == "System Administration"); 
               if (adminGroup != null) adminGroup.Visible = isAdmin;

               // Example: Disable specific items
               var jobItem = navBarControl2.Items.FirstOrDefault(i => i.Name == "navBarItem_Jobs"); // Assuming Name property is set in designer
               if (jobItem != null) jobItem.Enabled = isAdmin; 
            }
            */

            Log.Debug("Permissions applied.");
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