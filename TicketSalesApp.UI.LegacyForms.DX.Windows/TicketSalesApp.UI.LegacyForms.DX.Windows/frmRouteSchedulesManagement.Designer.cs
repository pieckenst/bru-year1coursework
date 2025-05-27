namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmRouteSchedulesManagement
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
            this.gridControlSchedules = new DevExpress.XtraGrid.GridControl();
            this.routeScheduleBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewSchedules = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colRouteScheduleId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteStartPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteEndPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteStopsDisplay = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDepartureTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colArrivalTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colAvailableSeats = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colIsActive = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemCheckEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.lueRouteFilter = new DevExpress.XtraEditors.LookUpEdit();
            this.routeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dateFilter = new DevExpress.XtraEditors.DateEdit();
            this.btnAdd = new DevExpress.XtraEditors.SimpleButton();
            this.btnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlGroupFilters = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemRouteFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemAdd = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemRefresh = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemEdit = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDelete = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemButtons = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeScheduleBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedules)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupFilters)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemAdd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemButtons)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.gridControlSchedules);
            this.layoutControl1.Controls.Add(this.lueRouteFilter);
            this.layoutControl1.Controls.Add(this.dateFilter);
            this.layoutControl1.Controls.Add(this.btnAdd);
            this.layoutControl1.Controls.Add(this.btnEdit);
            this.layoutControl1.Controls.Add(this.btnDelete);
            this.layoutControl1.Controls.Add(this.btnRefresh);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.Root;
            this.layoutControl1.Size = new System.Drawing.Size(984, 561);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // gridControlSchedules
            // 
            this.gridControlSchedules.DataSource = this.routeScheduleBindingSource;
            this.gridControlSchedules.Location = new System.Drawing.Point(12, 64);
            this.gridControlSchedules.MainView = this.gridViewSchedules;
            this.gridControlSchedules.Name = "gridControlSchedules";
            this.gridControlSchedules.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemCheckEdit1});
            this.gridControlSchedules.Size = new System.Drawing.Size(960, 459);
            this.gridControlSchedules.TabIndex = 4;
            this.gridControlSchedules.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSchedules});
            // 
            // routeScheduleBindingSource
            // 
            this.routeScheduleBindingSource.DataSource = typeof(TicketSalesApp.Core.Models.RouteSchedules);
            // 
            // gridViewSchedules
            // 
            this.gridViewSchedules.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colRouteScheduleId,
            this.colRouteStartPoint,
            this.colRouteEndPoint,
            this.colRouteStopsDisplay,
            this.colDepartureTime,
            this.colArrivalTime,
            this.colPrice,
            this.colAvailableSeats,
            this.colIsActive});
            this.gridViewSchedules.GridControl = this.gridControlSchedules;
            this.gridViewSchedules.Name = "gridViewSchedules";
            this.gridViewSchedules.OptionsBehavior.Editable = false;
            this.gridViewSchedules.OptionsView.ShowGroupPanel = false;
            this.gridViewSchedules.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gridViewSchedules_CustomUnboundColumnData);
            this.gridViewSchedules.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gridViewSchedules_FocusedRowChanged);
            // 
            // colRouteScheduleId
            // 
            this.colRouteScheduleId.FieldName = "RouteScheduleId";
            this.colRouteScheduleId.Name = "colRouteScheduleId";
            this.colRouteScheduleId.Visible = true;
            this.colRouteScheduleId.VisibleIndex = 0;
            this.colRouteScheduleId.Width = 40;
            this.colRouteScheduleId.Caption = "ID";
            // 
            // colRouteStartPoint
            // 
            this.colRouteStartPoint.FieldName = "Marshut.StartPoint"; // Unbound
            this.colRouteStartPoint.Name = "colRouteStartPoint";
            this.colRouteStartPoint.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colRouteStartPoint.Visible = true;
            this.colRouteStartPoint.VisibleIndex = 1;
            this.colRouteStartPoint.Width = 150;
            this.colRouteStartPoint.Caption = "Маршрут (Начало)";
            this.colRouteStartPoint.OptionsColumn.AllowEdit = false;
            // 
            // colRouteEndPoint
            // 
            this.colRouteEndPoint.FieldName = "Marshut.EndPoint"; // Unbound
            this.colRouteEndPoint.Name = "colRouteEndPoint";
            this.colRouteEndPoint.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colRouteEndPoint.Visible = true;
            this.colRouteEndPoint.VisibleIndex = 2;
            this.colRouteEndPoint.Width = 150;
            this.colRouteEndPoint.Caption = "Маршрут (Конец)";
            this.colRouteEndPoint.OptionsColumn.AllowEdit = false;
            // 
            // colRouteStopsDisplay
            // 
            this.colRouteStopsDisplay.Caption = "Остановки";
            this.colRouteStopsDisplay.FieldName = "RouteStopsDisplayString";
            this.colRouteStopsDisplay.Name = "colRouteStopsDisplay";
            this.colRouteStopsDisplay.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colRouteStopsDisplay.Visible = true;
            this.colRouteStopsDisplay.VisibleIndex = 3;
            this.colRouteStopsDisplay.Width = 250;
            this.colRouteStopsDisplay.OptionsColumn.AllowEdit = false;
            // 
            // colDepartureTime
            // 
            this.colDepartureTime.FieldName = "DepartureTime";
            this.colDepartureTime.Name = "colDepartureTime";
            this.colDepartureTime.Visible = true;
            this.colDepartureTime.VisibleIndex = 4;
            this.colDepartureTime.Width = 120;
            this.colDepartureTime.Caption = "Отправление";
            this.colDepartureTime.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            this.colDepartureTime.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            // 
            // colArrivalTime
            // 
            this.colArrivalTime.FieldName = "ArrivalTime";
            this.colArrivalTime.Name = "colArrivalTime";
            this.colArrivalTime.Visible = true;
            this.colArrivalTime.VisibleIndex = 5;
            this.colArrivalTime.Width = 120;
            this.colArrivalTime.Caption = "Прибытие";
            this.colArrivalTime.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            this.colArrivalTime.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            // 
            // colPrice
            // 
            this.colPrice.FieldName = "Price";
            this.colPrice.Name = "colPrice";
            this.colPrice.Visible = true;
            this.colPrice.VisibleIndex = 6;
            this.colPrice.Width = 80;
            this.colPrice.Caption = "Цена";
            this.colPrice.DisplayFormat.FormatString = "C2";
            this.colPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            // 
            // colAvailableSeats
            // 
            this.colAvailableSeats.FieldName = "AvailableSeats";
            this.colAvailableSeats.Name = "colAvailableSeats";
            this.colAvailableSeats.Visible = true;
            this.colAvailableSeats.VisibleIndex = 7;
            this.colAvailableSeats.Width = 80;
            this.colAvailableSeats.Caption = "Места";
            // 
            // colIsActive
            // 
            this.colIsActive.FieldName = "IsActive";
            this.colIsActive.Name = "colIsActive";
            this.colIsActive.Visible = true;
            this.colIsActive.VisibleIndex = 8;
            this.colIsActive.Width = 60;
            this.colIsActive.Caption = "Активен";
            this.colIsActive.ColumnEdit = this.repositoryItemCheckEdit1;
            // 
            // repositoryItemCheckEdit1
            // 
            this.repositoryItemCheckEdit1.AutoHeight = false;
            this.repositoryItemCheckEdit1.Name = "repositoryItemCheckEdit1";
            // 
            // lueRouteFilter
            // 
            this.lueRouteFilter.Location = new System.Drawing.Point(24, 28);
            this.lueRouteFilter.Name = "lueRouteFilter";
            this.lueRouteFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lueRouteFilter.Properties.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] {
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("RouteId", "ID", 40, DevExpress.Utils.FormatType.None, "", true, DevExpress.Utils.HorzAlignment.Default, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.Default),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("StartPoint", "Начало", 150),
            new DevExpress.XtraEditors.Controls.LookUpColumnInfo("EndPoint", "Конец", 150)});
            this.lueRouteFilter.Properties.DataSource = this.routeBindingSource;
            this.lueRouteFilter.Properties.DisplayMember = "StartPoint";
            this.lueRouteFilter.Properties.NullText = "[Все маршруты]";
            this.lueRouteFilter.Properties.ValueMember = "RouteId";
            this.lueRouteFilter.Size = new System.Drawing.Size(363, 20);
            this.lueRouteFilter.StyleController = this.layoutControl1;
            this.lueRouteFilter.TabIndex = 5;
           
            // 
            // routeBindingSource
            // 
            this.routeBindingSource.DataSource = typeof(TicketSalesApp.Core.Models.Marshut);
            // 
            // dateFilter
            // 
            this.dateFilter.EditValue = null;
            this.dateFilter.Location = new System.Drawing.Point(391, 28);
            this.dateFilter.Name = "dateFilter";
            this.dateFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.dateFilter.Properties.NullText = "[Все даты]";
            this.dateFilter.Size = new System.Drawing.Size(277, 20);
            this.dateFilter.StyleController = this.layoutControl1;
            this.dateFilter.TabIndex = 6;
            
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(672, 28);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(114, 22);
            this.btnAdd.StyleController = this.layoutControl1;
            this.btnAdd.TabIndex = 7;
            this.btnAdd.Text = "Добавить расписание";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(763, 527);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(102, 22);
            this.btnEdit.StyleController = this.layoutControl1;
            this.btnEdit.TabIndex = 8;
            this.btnEdit.Text = "Редактировать";
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(869, 527);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(103, 22);
            this.btnDelete.StyleController = this.layoutControl1;
            this.btnDelete.TabIndex = 9;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(790, 28);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(182, 22);
            this.btnRefresh.StyleController = this.layoutControl1;
            this.btnRefresh.TabIndex = 10;
            this.btnRefresh.Text = "Обновить";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemGrid,
            this.layoutControlGroupFilters,
            this.layoutControlItemEdit,
            this.layoutControlItemDelete,
            this.emptySpaceItemButtons});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(984, 561);
            this.Root.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            this.layoutControlItemGrid.Control = this.gridControlSchedules;
            this.layoutControlItemGrid.Location = new System.Drawing.Point(0, 52);
            this.layoutControlItemGrid.Name = "layoutControlItemGrid";
            this.layoutControlItemGrid.Size = new System.Drawing.Size(964, 463);
            this.layoutControlItemGrid.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemGrid.TextVisible = false;
            // 
            // layoutControlGroupFilters
            // 
            this.layoutControlGroupFilters.GroupBordersVisible = false;
            this.layoutControlGroupFilters.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemRouteFilter,
            this.layoutControlItemDateFilter,
            this.layoutControlItemAdd,
            this.layoutControlItemRefresh});
            this.layoutControlGroupFilters.Location = new System.Drawing.Point(0, 0);
            this.layoutControlGroupFilters.Name = "layoutControlGroupFilters";
            this.layoutControlGroupFilters.Size = new System.Drawing.Size(964, 52);
            this.layoutControlGroupFilters.Spacing = new DevExpress.XtraLayout.Utils.Padding(12, 12, 16, 10);
            this.layoutControlGroupFilters.Text = "Фильтры";
            // 
            // layoutControlItemRouteFilter
            // 
            this.layoutControlItemRouteFilter.Control = this.lueRouteFilter;
            this.layoutControlItemRouteFilter.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItemRouteFilter.Name = "layoutControlItemRouteFilter";
            this.layoutControlItemRouteFilter.Size = new System.Drawing.Size(367, 26);
            this.layoutControlItemRouteFilter.Text = "Маршрут:";
            this.layoutControlItemRouteFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemRouteFilter.TextVisible = false;
            // 
            // layoutControlItemDateFilter
            // 
            this.layoutControlItemDateFilter.Control = this.dateFilter;
            this.layoutControlItemDateFilter.Location = new System.Drawing.Point(367, 0);
            this.layoutControlItemDateFilter.Name = "layoutControlItemDateFilter";
            this.layoutControlItemDateFilter.Size = new System.Drawing.Size(281, 26);
            this.layoutControlItemDateFilter.Text = "Дата:";
            this.layoutControlItemDateFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemDateFilter.TextVisible = false;
            // 
            // layoutControlItemAdd
            // 
            this.layoutControlItemAdd.Control = this.btnAdd;
            this.layoutControlItemAdd.Location = new System.Drawing.Point(648, 0);
            this.layoutControlItemAdd.Name = "layoutControlItemAdd";
            this.layoutControlItemAdd.Size = new System.Drawing.Size(118, 26);
            this.layoutControlItemAdd.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemAdd.TextVisible = false;
            // 
            // layoutControlItemRefresh
            // 
            this.layoutControlItemRefresh.Control = this.btnRefresh;
            this.layoutControlItemRefresh.Location = new System.Drawing.Point(766, 0);
            this.layoutControlItemRefresh.Name = "layoutControlItemRefresh";
            this.layoutControlItemRefresh.Size = new System.Drawing.Size(186, 26);
            this.layoutControlItemRefresh.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemRefresh.TextVisible = false;
            // 
            // layoutControlItemEdit
            // 
            this.layoutControlItemEdit.Control = this.btnEdit;
            this.layoutControlItemEdit.Location = new System.Drawing.Point(751, 515);
            this.layoutControlItemEdit.MaxSize = new System.Drawing.Size(106, 26);
            this.layoutControlItemEdit.MinSize = new System.Drawing.Size(106, 26);
            this.layoutControlItemEdit.Name = "layoutControlItemEdit";
            this.layoutControlItemEdit.Size = new System.Drawing.Size(106, 26);
            this.layoutControlItemEdit.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemEdit.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemEdit.TextVisible = false;
            // 
            // layoutControlItemDelete
            // 
            this.layoutControlItemDelete.Control = this.btnDelete;
            this.layoutControlItemDelete.Location = new System.Drawing.Point(857, 515);
            this.layoutControlItemDelete.MaxSize = new System.Drawing.Size(107, 26);
            this.layoutControlItemDelete.MinSize = new System.Drawing.Size(107, 26);
            this.layoutControlItemDelete.Name = "layoutControlItemDelete";
            this.layoutControlItemDelete.Size = new System.Drawing.Size(107, 26);
            this.layoutControlItemDelete.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDelete.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemDelete.TextVisible = false;
            // 
            // emptySpaceItemButtons
            // 
            this.emptySpaceItemButtons.AllowHotTrack = false;
            this.emptySpaceItemButtons.Location = new System.Drawing.Point(0, 515);
            this.emptySpaceItemButtons.Name = "emptySpaceItemButtons";
            this.emptySpaceItemButtons.Size = new System.Drawing.Size(751, 26);
            this.emptySpaceItemButtons.TextSize = new System.Drawing.Size(0, 0);
            // 
            // frmRouteSchedulesManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.layoutControl1);
            this.Name = "frmRouteSchedulesManagement";
            this.Text = "Управление расписанием маршрутов";
            this.Load += new System.EventHandler(this.frmRouteSchedulesManagement_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSchedules)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeScheduleBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSchedules)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupFilters)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemAdd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemButtons)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraGrid.GridControl gridControlSchedules;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSchedules;
        private DevExpress.XtraEditors.LookUpEdit lueRouteFilter;
        private DevExpress.XtraEditors.DateEdit dateFilter;
        private DevExpress.XtraEditors.SimpleButton btnAdd;
        private DevExpress.XtraEditors.SimpleButton btnEdit;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.SimpleButton btnRefresh;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemGrid;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroupFilters;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRouteFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemAdd;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRefresh;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemEdit;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDelete;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemButtons;
        private System.Windows.Forms.BindingSource routeScheduleBindingSource;
        private System.Windows.Forms.BindingSource routeBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteScheduleId;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteStartPoint;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteEndPoint;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteStopsDisplay;
        private DevExpress.XtraGrid.Columns.GridColumn colDepartureTime;
        private DevExpress.XtraGrid.Columns.GridColumn colArrivalTime;
        private DevExpress.XtraGrid.Columns.GridColumn colPrice;
        private DevExpress.XtraGrid.Columns.GridColumn colAvailableSeats;
        private DevExpress.XtraGrid.Columns.GridColumn colIsActive;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemCheckEdit1;
    }
} 