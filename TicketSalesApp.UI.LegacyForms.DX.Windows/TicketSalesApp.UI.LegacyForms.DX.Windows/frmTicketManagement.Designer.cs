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
            this.components = new System.ComponentModel.Container();
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.btnCancelTicket = new DevExpress.XtraEditors.SimpleButton();
            this.btnViewDetails = new DevExpress.XtraEditors.SimpleButton();
            this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.btnApplyFilter = new DevExpress.XtraEditors.SimpleButton();
            this.cboStatusFilter = new DevExpress.XtraEditors.ComboBoxEdit();
            this.lueRouteScheduleFilter = new DevExpress.XtraEditors.LookUpEdit();
            this.dateToFilter = new DevExpress.XtraEditors.DateEdit();
            this.dateFromFilter = new DevExpress.XtraEditors.DateEdit();
            this.gridControlTickets = new DevExpress.XtraGrid.GridControl();
            this.ticketViewModelBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewTickets = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colTicketId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPassengerName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colTicketPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPurchaseDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colIsSold = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteDisplay = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemRouteSchedule = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemStatus = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemApplyFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemFilters = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItemRefresh = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemViewDetails = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemCancelTicket = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemActions = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cboStatusFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteScheduleFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlTickets)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ticketViewModelBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewTickets)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteSchedule)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemViewDetails)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemCancelTicket)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemActions)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.btnCancelTicket);
            this.layoutControl1.Controls.Add(this.btnViewDetails);
            this.layoutControl1.Controls.Add(this.btnRefresh);
            this.layoutControl1.Controls.Add(this.btnApplyFilter);
            this.layoutControl1.Controls.Add(this.cboStatusFilter);
            this.layoutControl1.Controls.Add(this.lueRouteScheduleFilter);
            this.layoutControl1.Controls.Add(this.dateToFilter);
            this.layoutControl1.Controls.Add(this.dateFromFilter);
            this.layoutControl1.Controls.Add(this.gridControlTickets);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.Root;
            this.layoutControl1.Size = new System.Drawing.Size(984, 561);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // btnCancelTicket
            // 
            this.btnCancelTicket.Location = new System.Drawing.Point(767, 527);
            this.btnCancelTicket.Name = "btnCancelTicket";
            this.btnCancelTicket.Size = new System.Drawing.Size(110, 22);
            this.btnCancelTicket.StyleController = this.layoutControl1;
            this.btnCancelTicket.TabIndex = 8;
            this.btnCancelTicket.Text = "Отменить Билет";
            this.btnCancelTicket.Click += new System.EventHandler(this.btnCancelTicket_Click);
            // 
            // btnViewDetails
            // 
            this.btnViewDetails.Location = new System.Drawing.Point(658, 527);
            this.btnViewDetails.Name = "btnViewDetails";
            this.btnViewDetails.Size = new System.Drawing.Size(105, 22);
            this.btnViewDetails.StyleController = this.layoutControl1;
            this.btnViewDetails.TabIndex = 7;
            this.btnViewDetails.Text = "Детали";
            this.btnViewDetails.Click += new System.EventHandler(this.btnViewDetails_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(881, 527);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(91, 22);
            this.btnRefresh.StyleController = this.layoutControl1;
            this.btnRefresh.TabIndex = 9;
            this.btnRefresh.Text = "Обновить";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnApplyFilter
            // 
            this.btnApplyFilter.Location = new System.Drawing.Point(702, 12);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(98, 22);
            this.btnApplyFilter.StyleController = this.layoutControl1;
            this.btnApplyFilter.TabIndex = 4;
            this.btnApplyFilter.Text = "Применить";
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
            // 
            // cboStatusFilter
            // 
            this.cboStatusFilter.Location = new System.Drawing.Point(564, 12);
            this.cboStatusFilter.Name = "cboStatusFilter";
            this.cboStatusFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cboStatusFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cboStatusFilter.Size = new System.Drawing.Size(134, 20);
            this.cboStatusFilter.StyleController = this.layoutControl1;
            this.cboStatusFilter.TabIndex = 3;
            // 
            // lueRouteScheduleFilter
            // 
            this.lueRouteScheduleFilter.Location = new System.Drawing.Point(397, 12);
            this.lueRouteScheduleFilter.Name = "lueRouteScheduleFilter";
            this.lueRouteScheduleFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lueRouteScheduleFilter.Properties.NullText = "[Все рейсы]";
            this.lueRouteScheduleFilter.Size = new System.Drawing.Size(122, 20);
            this.lueRouteScheduleFilter.StyleController = this.layoutControl1;
            this.lueRouteScheduleFilter.TabIndex = 2;
            // 
            // dateToFilter
            // 
            this.dateToFilter.EditValue = null;
            this.dateToFilter.Location = new System.Drawing.Point(248, 12);
            this.dateToFilter.Name = "dateToFilter";
            this.dateToFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateToFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateToFilter.Properties.DisplayFormat.FormatString = "dd.MM.yyyy";
            this.dateToFilter.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dateToFilter.Properties.EditFormat.FormatString = "dd.MM.yyyy";
            this.dateToFilter.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dateToFilter.Properties.Mask.EditMask = "dd.MM.yyyy";
            this.dateToFilter.Size = new System.Drawing.Size(93, 20);
            this.dateToFilter.StyleController = this.layoutControl1;
            this.dateToFilter.TabIndex = 1;
            // 
            // dateFromFilter
            // 
            this.dateFromFilter.EditValue = null;
            this.dateFromFilter.Location = new System.Drawing.Point(68, 12);
            this.dateFromFilter.Name = "dateFromFilter";
            this.dateFromFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFromFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFromFilter.Properties.DisplayFormat.FormatString = "dd.MM.yyyy";
            this.dateFromFilter.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dateFromFilter.Properties.EditFormat.FormatString = "dd.MM.yyyy";
            this.dateFromFilter.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.dateFromFilter.Properties.Mask.EditMask = "dd.MM.yyyy";
            this.dateFromFilter.Size = new System.Drawing.Size(120, 20);
            this.dateFromFilter.StyleController = this.layoutControl1;
            this.dateFromFilter.TabIndex = 0;
            // 
            // gridControlTickets
            // 
            this.gridControlTickets.DataSource = null;
            this.gridControlTickets.Location = new System.Drawing.Point(12, 38);
            this.gridControlTickets.MainView = this.gridViewTickets;
            this.gridControlTickets.Name = "gridControlTickets";
            this.gridControlTickets.Size = new System.Drawing.Size(960, 485);
            this.gridControlTickets.TabIndex = 5;
            this.gridControlTickets.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewTickets});
            // 
            // ticketViewModelBindingSource
            // 
            this.ticketViewModelBindingSource.DataSource = typeof(TicketSalesApp.Core.Models.Bilet);
            // 
            // gridViewTickets
            // 
            this.gridViewTickets.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colTicketId,
            this.colRouteId,
            this.colPassengerName,
            this.colTicketPrice,
            this.colPurchaseDate,
            this.colIsSold,
            this.colRouteDisplay});
            this.gridViewTickets.GridControl = this.gridControlTickets;
            this.gridViewTickets.Name = "gridViewTickets";
            this.gridViewTickets.OptionsFind.AlwaysVisible = true;
            // 
            // colTicketId
            // 
            this.colTicketId.Caption = "Ticket ID";
            this.colTicketId.FieldName = "TicketId";
            this.colTicketId.Name = "colTicketId";
            this.colTicketId.Visible = true;
            this.colTicketId.VisibleIndex = 0;
            this.colTicketId.OptionsColumn.AllowEdit = false;
            // 
            // colRouteId
            // 
            this.colRouteId.Caption = "Route ID";
            this.colRouteId.FieldName = "RouteId";
            this.colRouteId.Name = "colRouteId";
            this.colRouteId.Visible = true;
            this.colRouteId.VisibleIndex = 1;
            this.colRouteId.OptionsColumn.AllowEdit = false;
            // 
            // colPassengerName
            // 
            this.colPassengerName.Caption = "Passenger";
            this.colPassengerName.FieldName = "PassengerName";
            this.colPassengerName.Name = "colPassengerName";
            this.colPassengerName.Visible = true;
            this.colPassengerName.VisibleIndex = 2;
            this.colPassengerName.OptionsColumn.AllowEdit = false;
            // 
            // colTicketPrice
            // 
            this.colTicketPrice.Caption = "Price";
            this.colTicketPrice.FieldName = "TicketPrice";
            this.colTicketPrice.Name = "colTicketPrice";
            this.colTicketPrice.Visible = true;
            this.colTicketPrice.VisibleIndex = 3;
            this.colTicketPrice.OptionsColumn.AllowEdit = false;
            this.colTicketPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.colTicketPrice.DisplayFormat.FormatString = "C2";
            // 
            // colPurchaseDate
            // 
            this.colPurchaseDate.Caption = "Purchase Date";
            this.colPurchaseDate.FieldName = "PurchaseDate";
            this.colPurchaseDate.Name = "colPurchaseDate";
            this.colPurchaseDate.Visible = true;
            this.colPurchaseDate.VisibleIndex = 4;
            this.colPurchaseDate.OptionsColumn.AllowEdit = false;
            this.colPurchaseDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.colPurchaseDate.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            // 
            // colIsSold
            // 
            this.colIsSold.Caption = "Продан";
            this.colIsSold.FieldName = "IsSold";
            this.colIsSold.Name = "colIsSold";
            this.colIsSold.Visible = true;
            this.colIsSold.VisibleIndex = 5;
            this.colIsSold.OptionsColumn.AllowEdit = false;
            this.colIsSold.UnboundType = DevExpress.Data.UnboundColumnType.String;
            // 
            // colRouteDisplay
            // 
            this.colRouteDisplay.Caption = "Route";
            this.colRouteDisplay.FieldName = "RouteDisplayString";
            this.colRouteDisplay.Name = "colRouteDisplay";
            this.colRouteDisplay.Visible = true;
            this.colRouteDisplay.VisibleIndex = 6;
            this.colRouteDisplay.OptionsColumn.AllowEdit = false;
            this.colRouteDisplay.UnboundType = DevExpress.Data.UnboundColumnType.String;
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemGrid,
            this.layoutControlItemDateFrom,
            this.layoutControlItemDateTo,
            this.layoutControlItemRouteSchedule,
            this.layoutControlItemStatus,
            this.layoutControlItemApplyFilter,
            this.emptySpaceItemFilters,
            this.layoutControlItemRefresh,
            this.layoutControlItemViewDetails,
            this.layoutControlItemCancelTicket,
            this.emptySpaceItemActions});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(984, 561);
            this.Root.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            this.layoutControlItemGrid.Control = this.gridControlTickets;
            this.layoutControlItemGrid.Location = new System.Drawing.Point(0, 26);
            this.layoutControlItemGrid.Name = "layoutControlItemGrid";
            this.layoutControlItemGrid.Size = new System.Drawing.Size(964, 489);
            this.layoutControlItemGrid.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemGrid.TextVisible = false;
            // 
            // layoutControlItemDateFrom
            // 
            this.layoutControlItemDateFrom.Control = this.dateFromFilter;
            this.layoutControlItemDateFrom.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItemDateFrom.MaxSize = new System.Drawing.Size(180, 26);
            this.layoutControlItemDateFrom.MinSize = new System.Drawing.Size(110, 26);
            this.layoutControlItemDateFrom.Name = "layoutControlItemDateFrom";
            this.layoutControlItemDateFrom.Size = new System.Drawing.Size(180, 26);
            this.layoutControlItemDateFrom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDateFrom.Text = "Дата С:";
            this.layoutControlItemDateFrom.TextSize = new System.Drawing.Size(44, 13);
            // 
            // layoutControlItemDateTo
            // 
            this.layoutControlItemDateTo.Control = this.dateToFilter;
            this.layoutControlItemDateTo.Location = new System.Drawing.Point(180, 0);
            this.layoutControlItemDateTo.MaxSize = new System.Drawing.Size(153, 26);
            this.layoutControlItemDateTo.MinSize = new System.Drawing.Size(110, 26);
            this.layoutControlItemDateTo.Name = "layoutControlItemDateTo";
            this.layoutControlItemDateTo.Size = new System.Drawing.Size(153, 26);
            this.layoutControlItemDateTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDateTo.Text = "Дата По:";
            this.layoutControlItemDateTo.TextSize = new System.Drawing.Size(44, 13);
            // 
            // layoutControlItemRouteSchedule
            // 
            this.layoutControlItemRouteSchedule.Control = this.lueRouteScheduleFilter;
            this.layoutControlItemRouteSchedule.Location = new System.Drawing.Point(333, 0);
            this.layoutControlItemRouteSchedule.MaxSize = new System.Drawing.Size(182, 26);
            this.layoutControlItemRouteSchedule.MinSize = new System.Drawing.Size(100, 26);
            this.layoutControlItemRouteSchedule.Name = "layoutControlItemRouteSchedule";
            this.layoutControlItemRouteSchedule.Size = new System.Drawing.Size(182, 26);
            this.layoutControlItemRouteSchedule.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemRouteSchedule.Text = "Рейс:";
            this.layoutControlItemRouteSchedule.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItemRouteSchedule.TextSize = new System.Drawing.Size(30, 13);
            this.layoutControlItemRouteSchedule.TextToControlDistance = 26;
            // 
            // layoutControlItemStatus
            // 
            this.layoutControlItemStatus.Control = this.cboStatusFilter;
            this.layoutControlItemStatus.Location = new System.Drawing.Point(515, 0);
            this.layoutControlItemStatus.MaxSize = new System.Drawing.Size(175, 26);
            this.layoutControlItemStatus.MinSize = new System.Drawing.Size(100, 26);
            this.layoutControlItemStatus.Name = "layoutControlItemStatus";
            this.layoutControlItemStatus.Size = new System.Drawing.Size(175, 26);
            this.layoutControlItemStatus.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemStatus.Text = "Статус:";
            this.layoutControlItemStatus.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItemStatus.TextSize = new System.Drawing.Size(36, 13);
            this.layoutControlItemStatus.TextToControlDistance = 1;
            // 
            // layoutControlItemApplyFilter
            // 
            this.layoutControlItemApplyFilter.Control = this.btnApplyFilter;
            this.layoutControlItemApplyFilter.Location = new System.Drawing.Point(690, 0);
            this.layoutControlItemApplyFilter.MaxSize = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.MinSize = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.Name = "layoutControlItemApplyFilter";
            this.layoutControlItemApplyFilter.Size = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemApplyFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemApplyFilter.TextVisible = false;
            // 
            // emptySpaceItemFilters
            // 
            this.emptySpaceItemFilters.AllowHotTrack = false;
            this.emptySpaceItemFilters.Location = new System.Drawing.Point(792, 0);
            this.emptySpaceItemFilters.Name = "emptySpaceItemFilters";
            this.emptySpaceItemFilters.Size = new System.Drawing.Size(172, 26);
            this.emptySpaceItemFilters.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItemRefresh
            // 
            this.layoutControlItemRefresh.Control = this.btnRefresh;
            this.layoutControlItemRefresh.Location = new System.Drawing.Point(869, 515);
            this.layoutControlItemRefresh.MaxSize = new System.Drawing.Size(95, 26);
            this.layoutControlItemRefresh.MinSize = new System.Drawing.Size(95, 26);
            this.layoutControlItemRefresh.Name = "layoutControlItemRefresh";
            this.layoutControlItemRefresh.Size = new System.Drawing.Size(95, 26);
            this.layoutControlItemRefresh.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemRefresh.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemRefresh.TextVisible = false;
            // 
            // layoutControlItemViewDetails
            // 
            this.layoutControlItemViewDetails.Control = this.btnViewDetails;
            this.layoutControlItemViewDetails.Location = new System.Drawing.Point(646, 515);
            this.layoutControlItemViewDetails.MaxSize = new System.Drawing.Size(109, 26);
            this.layoutControlItemViewDetails.MinSize = new System.Drawing.Size(109, 26);
            this.layoutControlItemViewDetails.Name = "layoutControlItemViewDetails";
            this.layoutControlItemViewDetails.Size = new System.Drawing.Size(109, 26);
            this.layoutControlItemViewDetails.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemViewDetails.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemViewDetails.TextVisible = false;
            // 
            // layoutControlItemCancelTicket
            // 
            this.layoutControlItemCancelTicket.Control = this.btnCancelTicket;
            this.layoutControlItemCancelTicket.Location = new System.Drawing.Point(755, 515);
            this.layoutControlItemCancelTicket.MaxSize = new System.Drawing.Size(114, 26);
            this.layoutControlItemCancelTicket.MinSize = new System.Drawing.Size(114, 26);
            this.layoutControlItemCancelTicket.Name = "layoutControlItemCancelTicket";
            this.layoutControlItemCancelTicket.Size = new System.Drawing.Size(114, 26);
            this.layoutControlItemCancelTicket.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemCancelTicket.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemCancelTicket.TextVisible = false;
            // 
            // emptySpaceItemActions
            // 
            this.emptySpaceItemActions.AllowHotTrack = false;
            this.emptySpaceItemActions.Location = new System.Drawing.Point(0, 515);
            this.emptySpaceItemActions.Name = "emptySpaceItemActions";
            this.emptySpaceItemActions.Size = new System.Drawing.Size(646, 26);
            this.emptySpaceItemActions.TextSize = new System.Drawing.Size(0, 0);
            // 
            // frmTicketManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.layoutControl1);
            this.Name = "frmTicketManagement";
            this.Text = "Управление Билетами";
            this.Load += new System.EventHandler(this.frmTicketManagement_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cboStatusFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteScheduleFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlTickets)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ticketViewModelBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewTickets)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteSchedule)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemViewDetails)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemCancelTicket)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemActions)).EndInit();
            this.ResumeLayout(false);

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