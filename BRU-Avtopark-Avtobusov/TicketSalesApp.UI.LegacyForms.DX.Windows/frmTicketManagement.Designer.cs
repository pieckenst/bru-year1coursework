using System.ComponentModel;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmTicketManagement
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
            layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            btnCancelTicket = new DevExpress.XtraEditors.SimpleButton();
            btnViewDetails = new DevExpress.XtraEditors.SimpleButton();
            btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            btnApplyFilter = new DevExpress.XtraEditors.SimpleButton();
            cboStatusFilter = new DevExpress.XtraEditors.ComboBoxEdit();
            lueRouteScheduleFilter = new DevExpress.XtraEditors.LookUpEdit();
            dateToFilter = new DevExpress.XtraEditors.DateEdit();
            dateFromFilter = new DevExpress.XtraEditors.DateEdit();
            gridControlTickets = new DevExpress.XtraGrid.GridControl();
            gridViewTickets = new GridView();
            colTicketId = new GridColumn();
            colRouteId = new GridColumn();
            colPassengerName = new GridColumn();
            colTicketPrice = new GridColumn();
            colPurchaseDate = new GridColumn();
            colIsSold = new GridColumn();
            colRouteDisplay = new GridColumn();
            Root = new DevExpress.XtraLayout.LayoutControlGroup();
            layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemRouteSchedule = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemStatus = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemApplyFilter = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceItemFilters = new DevExpress.XtraLayout.EmptySpaceItem();
            layoutControlItemRefresh = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemViewDetails = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemCancelTicket = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceItemActions = new DevExpress.XtraLayout.EmptySpaceItem();
            ticketViewModelBindingSource = new System.Windows.Forms.BindingSource(components);
            ((ISupportInitialize)layoutControl1).BeginInit();
            layoutControl1.SuspendLayout();
            ((ISupportInitialize)cboStatusFilter.Properties).BeginInit();
            ((ISupportInitialize)lueRouteScheduleFilter.Properties).BeginInit();
            ((ISupportInitialize)dateToFilter.Properties).BeginInit();
            ((ISupportInitialize)dateToFilter.Properties.CalendarTimeProperties).BeginInit();
            ((ISupportInitialize)dateFromFilter.Properties).BeginInit();
            ((ISupportInitialize)dateFromFilter.Properties.CalendarTimeProperties).BeginInit();
            ((ISupportInitialize)gridControlTickets).BeginInit();
            ((ISupportInitialize)gridViewTickets).BeginInit();
            ((ISupportInitialize)Root).BeginInit();
            ((ISupportInitialize)layoutControlItemGrid).BeginInit();
            ((ISupportInitialize)layoutControlItemDateFrom).BeginInit();
            ((ISupportInitialize)layoutControlItemDateTo).BeginInit();
            ((ISupportInitialize)layoutControlItemRouteSchedule).BeginInit();
            ((ISupportInitialize)layoutControlItemStatus).BeginInit();
            ((ISupportInitialize)layoutControlItemApplyFilter).BeginInit();
            ((ISupportInitialize)emptySpaceItemFilters).BeginInit();
            ((ISupportInitialize)layoutControlItemRefresh).BeginInit();
            ((ISupportInitialize)layoutControlItemViewDetails).BeginInit();
            ((ISupportInitialize)layoutControlItemCancelTicket).BeginInit();
            ((ISupportInitialize)emptySpaceItemActions).BeginInit();
            ((ISupportInitialize)ticketViewModelBindingSource).BeginInit();
            SuspendLayout();
            // 
            // layoutControl1
            // 
            layoutControl1.Controls.Add(btnCancelTicket);
            layoutControl1.Controls.Add(btnViewDetails);
            layoutControl1.Controls.Add(btnRefresh);
            layoutControl1.Controls.Add(btnApplyFilter);
            layoutControl1.Controls.Add(cboStatusFilter);
            layoutControl1.Controls.Add(lueRouteScheduleFilter);
            layoutControl1.Controls.Add(dateToFilter);
            layoutControl1.Controls.Add(dateFromFilter);
            layoutControl1.Controls.Add(gridControlTickets);
            layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            layoutControl1.Location = new System.Drawing.Point(0, 0);
            layoutControl1.Margin = new System.Windows.Forms.Padding(4);
            layoutControl1.Name = "layoutControl1";
            layoutControl1.Root = Root;
            layoutControl1.Size = new System.Drawing.Size(1132, 700);
            layoutControl1.TabIndex = 0;
            layoutControl1.Text = "layoutControl1";
            // 
            // btnCancelTicket
            // 
            btnCancelTicket.Location = new System.Drawing.Point(880, 660);
            btnCancelTicket.Margin = new System.Windows.Forms.Padding(4);
            btnCancelTicket.Name = "btnCancelTicket";
            btnCancelTicket.Size = new System.Drawing.Size(129, 28);
            btnCancelTicket.StyleController = layoutControl1;
            btnCancelTicket.TabIndex = 8;
            btnCancelTicket.Text = "Отменить Билет";
            btnCancelTicket.Click += btnCancelTicket_Click;
            // 
            // btnViewDetails
            // 
            btnViewDetails.Location = new System.Drawing.Point(753, 660);
            btnViewDetails.Margin = new System.Windows.Forms.Padding(4);
            btnViewDetails.Name = "btnViewDetails";
            btnViewDetails.Size = new System.Drawing.Size(123, 28);
            btnViewDetails.StyleController = layoutControl1;
            btnViewDetails.TabIndex = 7;
            btnViewDetails.Text = "Детали";
            btnViewDetails.Click += btnViewDetails_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(1013, 660);
            btnRefresh.Margin = new System.Windows.Forms.Padding(4);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(107, 28);
            btnRefresh.StyleController = layoutControl1;
            btnRefresh.TabIndex = 9;
            btnRefresh.Text = "Обновить";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnApplyFilter
            // 
            btnApplyFilter.Location = new System.Drawing.Point(808, 12);
            btnApplyFilter.Margin = new System.Windows.Forms.Padding(4);
            btnApplyFilter.Name = "btnApplyFilter";
            btnApplyFilter.Size = new System.Drawing.Size(115, 28);
            btnApplyFilter.StyleController = layoutControl1;
            btnApplyFilter.TabIndex = 4;
            btnApplyFilter.Text = "Применить";
            btnApplyFilter.Click += btnApplyFilter_Click;
            // 
            // cboStatusFilter
            // 
            cboStatusFilter.Location = new System.Drawing.Point(650, 12);
            cboStatusFilter.Margin = new System.Windows.Forms.Padding(4);
            cboStatusFilter.Name = "cboStatusFilter";
            cboStatusFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cboStatusFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cboStatusFilter.Size = new System.Drawing.Size(154, 22);
            cboStatusFilter.StyleController = layoutControl1;
            cboStatusFilter.TabIndex = 3;
            // 
            // lueRouteScheduleFilter
            // 
            lueRouteScheduleFilter.Location = new System.Drawing.Point(453, 12);
            lueRouteScheduleFilter.Margin = new System.Windows.Forms.Padding(4);
            lueRouteScheduleFilter.Name = "lueRouteScheduleFilter";
            lueRouteScheduleFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lueRouteScheduleFilter.Properties.NullText = "[Все рейсы]";
            lueRouteScheduleFilter.Size = new System.Drawing.Size(148, 22);
            lueRouteScheduleFilter.StyleController = layoutControl1;
            lueRouteScheduleFilter.TabIndex = 2;
            // 
            // dateToFilter
            // 
            dateToFilter.EditValue = null;
            dateToFilter.Location = new System.Drawing.Point(276, 12);
            dateToFilter.Margin = new System.Windows.Forms.Padding(4);
            dateToFilter.Name = "dateToFilter";
            dateToFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateToFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateToFilter.Properties.DisplayFormat.FormatString = "dd.MM.yyyy";
            dateToFilter.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateToFilter.Properties.EditFormat.FormatString = "dd.MM.yyyy";
            dateToFilter.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateToFilter.Properties.Mask.EditMask = "dd.MM.yyyy";
            dateToFilter.Size = new System.Drawing.Size(115, 22);
            dateToFilter.StyleController = layoutControl1;
            dateToFilter.TabIndex = 1;
            // 
            // dateFromFilter
            // 
            dateFromFilter.EditValue = null;
            dateFromFilter.Location = new System.Drawing.Point(69, 12);
            dateFromFilter.Margin = new System.Windows.Forms.Padding(4);
            dateFromFilter.Name = "dateFromFilter";
            dateFromFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateFromFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateFromFilter.Properties.DisplayFormat.FormatString = "dd.MM.yyyy";
            dateFromFilter.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateFromFilter.Properties.EditFormat.FormatString = "dd.MM.yyyy";
            dateFromFilter.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateFromFilter.Properties.Mask.EditMask = "dd.MM.yyyy";
            dateFromFilter.Size = new System.Drawing.Size(146, 22);
            dateFromFilter.StyleController = layoutControl1;
            dateFromFilter.TabIndex = 0;
            // 
            // gridControlTickets
            // 
            gridControlTickets.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(5);
            gridControlTickets.Location = new System.Drawing.Point(12, 44);
            gridControlTickets.MainView = gridViewTickets;
            gridControlTickets.Margin = new System.Windows.Forms.Padding(4);
            gridControlTickets.Name = "gridControlTickets";
            gridControlTickets.Size = new System.Drawing.Size(1108, 612);
            gridControlTickets.TabIndex = 5;
            gridControlTickets.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridViewTickets });
            // 
            // gridViewTickets
            // 
            gridViewTickets.Columns.AddRange(new GridColumn[] { colTicketId, colRouteId, colPassengerName, colTicketPrice, colPurchaseDate, colIsSold, colRouteDisplay });
            gridViewTickets.DetailHeight = 431;
            gridViewTickets.GridControl = gridControlTickets;
            gridViewTickets.Name = "gridViewTickets";
            gridViewTickets.OptionsEditForm.PopupEditFormWidth = 933;
            gridViewTickets.OptionsFind.AlwaysVisible = true;
            // 
            // colTicketId
            // 
            colTicketId.Caption = "Ticket ID";
            colTicketId.FieldName = "TicketId";
            colTicketId.MinWidth = 23;
            colTicketId.Name = "colTicketId";
            colTicketId.OptionsColumn.AllowEdit = false;
            colTicketId.Visible = true;
            colTicketId.VisibleIndex = 0;
            colTicketId.Width = 87;
            // 
            // colRouteId
            // 
            colRouteId.Caption = "Route ID";
            colRouteId.FieldName = "RouteId";
            colRouteId.MinWidth = 23;
            colRouteId.Name = "colRouteId";
            colRouteId.OptionsColumn.AllowEdit = false;
            colRouteId.Visible = true;
            colRouteId.VisibleIndex = 1;
            colRouteId.Width = 87;
            // 
            // colPassengerName
            // 
            colPassengerName.Caption = "Passenger";
            colPassengerName.FieldName = "PassengerName";
            colPassengerName.MinWidth = 23;
            colPassengerName.Name = "colPassengerName";
            colPassengerName.OptionsColumn.AllowEdit = false;
            colPassengerName.Visible = true;
            colPassengerName.VisibleIndex = 2;
            colPassengerName.Width = 87;
            // 
            // colTicketPrice
            // 
            colTicketPrice.Caption = "Price";
            colTicketPrice.DisplayFormat.FormatString = "C2";
            colTicketPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colTicketPrice.FieldName = "TicketPrice";
            colTicketPrice.MinWidth = 23;
            colTicketPrice.Name = "colTicketPrice";
            colTicketPrice.OptionsColumn.AllowEdit = false;
            colTicketPrice.Visible = true;
            colTicketPrice.VisibleIndex = 3;
            colTicketPrice.Width = 87;
            // 
            // colPurchaseDate
            // 
            colPurchaseDate.Caption = "Purchase Date";
            colPurchaseDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colPurchaseDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colPurchaseDate.FieldName = "PurchaseDate";
            colPurchaseDate.MinWidth = 23;
            colPurchaseDate.Name = "colPurchaseDate";
            colPurchaseDate.OptionsColumn.AllowEdit = false;
            colPurchaseDate.Visible = true;
            colPurchaseDate.VisibleIndex = 4;
            colPurchaseDate.Width = 87;
            // 
            // colIsSold
            // 
            colIsSold.Caption = "Продан";
            colIsSold.FieldName = "IsSold";
            colIsSold.MinWidth = 23;
            colIsSold.Name = "colIsSold";
            colIsSold.OptionsColumn.AllowEdit = false;
            colIsSold.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colIsSold.Visible = true;
            colIsSold.VisibleIndex = 5;
            colIsSold.Width = 87;
            // 
            // colRouteDisplay
            // 
            colRouteDisplay.Caption = "Route";
            colRouteDisplay.FieldName = "RouteDisplayString";
            colRouteDisplay.MinWidth = 23;
            colRouteDisplay.Name = "colRouteDisplay";
            colRouteDisplay.OptionsColumn.AllowEdit = false;
            colRouteDisplay.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colRouteDisplay.Visible = true;
            colRouteDisplay.VisibleIndex = 6;
            colRouteDisplay.Width = 87;
            // 
            // Root
            // 
            Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            Root.GroupBordersVisible = false;
            Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] { layoutControlItemGrid, layoutControlItemDateFrom, layoutControlItemDateTo, layoutControlItemRouteSchedule, layoutControlItemStatus, layoutControlItemApplyFilter, emptySpaceItemFilters, layoutControlItemRefresh, layoutControlItemViewDetails, layoutControlItemCancelTicket, emptySpaceItemActions });
            Root.Name = "Root";
            Root.Size = new System.Drawing.Size(1132, 700);
            Root.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            layoutControlItemGrid.Control = gridControlTickets;
            layoutControlItemGrid.Location = new System.Drawing.Point(0, 32);
            layoutControlItemGrid.Name = "layoutControlItemGrid";
            layoutControlItemGrid.Size = new System.Drawing.Size(1112, 616);
            layoutControlItemGrid.TextVisible = false;
            // 
            // layoutControlItemDateFrom
            // 
            layoutControlItemDateFrom.Control = dateFromFilter;
            layoutControlItemDateFrom.Location = new System.Drawing.Point(0, 0);
            layoutControlItemDateFrom.MaxSize = new System.Drawing.Size(210, 32);
            layoutControlItemDateFrom.MinSize = new System.Drawing.Size(128, 32);
            layoutControlItemDateFrom.Name = "layoutControlItemDateFrom";
            layoutControlItemDateFrom.Size = new System.Drawing.Size(207, 32);
            layoutControlItemDateFrom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemDateFrom.Text = "Дата С:";
            layoutControlItemDateFrom.TextSize = new System.Drawing.Size(53, 16);
            // 
            // layoutControlItemDateTo
            // 
            layoutControlItemDateTo.Control = dateToFilter;
            layoutControlItemDateTo.Location = new System.Drawing.Point(207, 0);
            layoutControlItemDateTo.MaxSize = new System.Drawing.Size(178, 32);
            layoutControlItemDateTo.MinSize = new System.Drawing.Size(128, 32);
            layoutControlItemDateTo.Name = "layoutControlItemDateTo";
            layoutControlItemDateTo.Size = new System.Drawing.Size(176, 32);
            layoutControlItemDateTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemDateTo.Text = "Дата По:";
            layoutControlItemDateTo.TextSize = new System.Drawing.Size(53, 16);
            // 
            // layoutControlItemRouteSchedule
            // 
            layoutControlItemRouteSchedule.Control = lueRouteScheduleFilter;
            layoutControlItemRouteSchedule.Location = new System.Drawing.Point(383, 0);
            layoutControlItemRouteSchedule.MaxSize = new System.Drawing.Size(212, 32);
            layoutControlItemRouteSchedule.MinSize = new System.Drawing.Size(117, 32);
            layoutControlItemRouteSchedule.Name = "layoutControlItemRouteSchedule";
            layoutControlItemRouteSchedule.Size = new System.Drawing.Size(210, 32);
            layoutControlItemRouteSchedule.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemRouteSchedule.Text = "Рейс:";
            layoutControlItemRouteSchedule.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            layoutControlItemRouteSchedule.TextSize = new System.Drawing.Size(32, 16);
            layoutControlItemRouteSchedule.TextToControlDistance = 26;
            // 
            // layoutControlItemStatus
            // 
            layoutControlItemStatus.Control = cboStatusFilter;
            layoutControlItemStatus.Location = new System.Drawing.Point(593, 0);
            layoutControlItemStatus.MaxSize = new System.Drawing.Size(204, 32);
            layoutControlItemStatus.MinSize = new System.Drawing.Size(117, 32);
            layoutControlItemStatus.Name = "layoutControlItemStatus";
            layoutControlItemStatus.Size = new System.Drawing.Size(203, 32);
            layoutControlItemStatus.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemStatus.Text = "Статус:";
            layoutControlItemStatus.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            layoutControlItemStatus.TextSize = new System.Drawing.Size(44, 16);
            layoutControlItemStatus.TextToControlDistance = 1;
            // 
            // layoutControlItemApplyFilter
            // 
            layoutControlItemApplyFilter.Control = btnApplyFilter;
            layoutControlItemApplyFilter.Location = new System.Drawing.Point(796, 0);
            layoutControlItemApplyFilter.MaxSize = new System.Drawing.Size(119, 32);
            layoutControlItemApplyFilter.MinSize = new System.Drawing.Size(119, 32);
            layoutControlItemApplyFilter.Name = "layoutControlItemApplyFilter";
            layoutControlItemApplyFilter.Size = new System.Drawing.Size(119, 32);
            layoutControlItemApplyFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemApplyFilter.TextVisible = false;
            // 
            // emptySpaceItemFilters
            // 
            emptySpaceItemFilters.Location = new System.Drawing.Point(915, 0);
            emptySpaceItemFilters.Name = "emptySpaceItemFilters";
            emptySpaceItemFilters.Size = new System.Drawing.Size(197, 32);
            // 
            // layoutControlItemRefresh
            // 
            layoutControlItemRefresh.Control = btnRefresh;
            layoutControlItemRefresh.Location = new System.Drawing.Point(1001, 648);
            layoutControlItemRefresh.MaxSize = new System.Drawing.Size(111, 32);
            layoutControlItemRefresh.MinSize = new System.Drawing.Size(111, 32);
            layoutControlItemRefresh.Name = "layoutControlItemRefresh";
            layoutControlItemRefresh.Size = new System.Drawing.Size(111, 32);
            layoutControlItemRefresh.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemRefresh.TextVisible = false;
            // 
            // layoutControlItemViewDetails
            // 
            layoutControlItemViewDetails.Control = btnViewDetails;
            layoutControlItemViewDetails.Location = new System.Drawing.Point(741, 648);
            layoutControlItemViewDetails.MaxSize = new System.Drawing.Size(127, 32);
            layoutControlItemViewDetails.MinSize = new System.Drawing.Size(127, 32);
            layoutControlItemViewDetails.Name = "layoutControlItemViewDetails";
            layoutControlItemViewDetails.Size = new System.Drawing.Size(127, 32);
            layoutControlItemViewDetails.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemViewDetails.TextVisible = false;
            // 
            // layoutControlItemCancelTicket
            // 
            layoutControlItemCancelTicket.Control = btnCancelTicket;
            layoutControlItemCancelTicket.Location = new System.Drawing.Point(868, 648);
            layoutControlItemCancelTicket.MaxSize = new System.Drawing.Size(133, 32);
            layoutControlItemCancelTicket.MinSize = new System.Drawing.Size(133, 32);
            layoutControlItemCancelTicket.Name = "layoutControlItemCancelTicket";
            layoutControlItemCancelTicket.Size = new System.Drawing.Size(133, 32);
            layoutControlItemCancelTicket.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemCancelTicket.TextVisible = false;
            // 
            // emptySpaceItemActions
            // 
            emptySpaceItemActions.Location = new System.Drawing.Point(0, 648);
            emptySpaceItemActions.Name = "emptySpaceItemActions";
            emptySpaceItemActions.Size = new System.Drawing.Size(741, 32);
            // 
            // ticketViewModelBindingSource
            // 
            ticketViewModelBindingSource.DataSource = typeof(Core.Models.Bilet);
            // 
            // frmTicketManagement
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1132, 700);
            Controls.Add(layoutControl1);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "frmTicketManagement";
            Text = "Управление Билетами";
            Load += frmTicketManagement_Load;
            ((ISupportInitialize)layoutControl1).EndInit();
            layoutControl1.ResumeLayout(false);
            ((ISupportInitialize)cboStatusFilter.Properties).EndInit();
            ((ISupportInitialize)lueRouteScheduleFilter.Properties).EndInit();
            ((ISupportInitialize)dateToFilter.Properties.CalendarTimeProperties).EndInit();
            ((ISupportInitialize)dateToFilter.Properties).EndInit();
            ((ISupportInitialize)dateFromFilter.Properties.CalendarTimeProperties).EndInit();
            ((ISupportInitialize)dateFromFilter.Properties).EndInit();
            ((ISupportInitialize)gridControlTickets).EndInit();
            ((ISupportInitialize)gridViewTickets).EndInit();
            ((ISupportInitialize)Root).EndInit();
            ((ISupportInitialize)layoutControlItemGrid).EndInit();
            ((ISupportInitialize)layoutControlItemDateFrom).EndInit();
            ((ISupportInitialize)layoutControlItemDateTo).EndInit();
            ((ISupportInitialize)layoutControlItemRouteSchedule).EndInit();
            ((ISupportInitialize)layoutControlItemStatus).EndInit();
            ((ISupportInitialize)layoutControlItemApplyFilter).EndInit();
            ((ISupportInitialize)emptySpaceItemFilters).EndInit();
            ((ISupportInitialize)layoutControlItemRefresh).EndInit();
            ((ISupportInitialize)layoutControlItemViewDetails).EndInit();
            ((ISupportInitialize)layoutControlItemCancelTicket).EndInit();
            ((ISupportInitialize)emptySpaceItemActions).EndInit();
            ((ISupportInitialize)ticketViewModelBindingSource).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraGrid.GridControl gridControlTickets;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewTickets;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemGrid;
        private DevExpress.XtraEditors.SimpleButton btnApplyFilter;
        private DevExpress.XtraEditors.ComboBoxEdit cboStatusFilter;
        private DevExpress.XtraEditors.LookUpEdit lueRouteScheduleFilter;
        private DevExpress.XtraEditors.DateEdit dateToFilter;
        private DevExpress.XtraEditors.DateEdit dateFromFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateFrom;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateTo;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRouteSchedule;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemStatus;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemApplyFilter;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemFilters;
        private DevExpress.XtraEditors.SimpleButton btnCancelTicket;
        private DevExpress.XtraEditors.SimpleButton btnViewDetails;
        private DevExpress.XtraEditors.SimpleButton btnRefresh;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRefresh;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemViewDetails;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemCancelTicket;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemActions;
        private System.Windows.Forms.BindingSource ticketViewModelBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colTicketId;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteId;
        private DevExpress.XtraGrid.Columns.GridColumn colPassengerName;
        private DevExpress.XtraGrid.Columns.GridColumn colTicketPrice;
        private DevExpress.XtraGrid.Columns.GridColumn colPurchaseDate;
        private DevExpress.XtraGrid.Columns.GridColumn colIsSold;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteDisplay;
    }
} 