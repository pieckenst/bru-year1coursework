namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmRouteManagement
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
            this.gridControlRoutes = new DevExpress.XtraGrid.GridControl();
            this.routeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewRoutes = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colRouteId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colStartPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colEndPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colTravelTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colBusModel = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDriverSurname = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colTicketsCount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.txtSearch = new DevExpress.XtraEditors.TextEdit();
            this.btnAdd = new DevExpress.XtraEditors.SimpleButton();
            this.btnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlRoutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewRoutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.gridControlRoutes);
            this.layoutControl1.Controls.Add(this.txtSearch);
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
            // gridControlRoutes
            // 
            this.gridControlRoutes.DataSource = this.routeBindingSource;
            this.gridControlRoutes.Location = new System.Drawing.Point(12, 38);
            this.gridControlRoutes.MainView = this.gridViewRoutes;
            this.gridControlRoutes.Name = "gridControlRoutes";
            this.gridControlRoutes.Size = new System.Drawing.Size(960, 485);
            this.gridControlRoutes.TabIndex = 4;
            this.gridControlRoutes.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewRoutes});
            // 
            // routeBindingSource
            // 
            this.routeBindingSource.DataSource = typeof(TicketSalesApp.Core.Models.Marshut);
            // 
            // gridViewRoutes
            // 
            this.gridViewRoutes.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colRouteId,
            this.colStartPoint,
            this.colEndPoint,
            this.colTravelTime,
            this.colBusModel,
            this.colDriverSurname,
            this.colTicketsCount});
            this.gridViewRoutes.GridControl = this.gridControlRoutes;
            this.gridViewRoutes.Name = "gridViewRoutes";
            this.gridViewRoutes.OptionsBehavior.Editable = false;
            this.gridViewRoutes.OptionsView.ShowGroupPanel = false;
            this.gridViewRoutes.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gridViewRoutes_CustomUnboundColumnData);
            this.gridViewRoutes.FocusedRowChanged += new DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventHandler(this.gridViewRoutes_FocusedRowChanged);
            // 
            // colRouteId
            // 
            this.colRouteId.FieldName = "RouteId";
            this.colRouteId.Name = "colRouteId";
            this.colRouteId.Visible = true;
            this.colRouteId.VisibleIndex = 0;
            this.colRouteId.Width = 50;
            this.colRouteId.Caption = "ID";
            // 
            // colStartPoint
            // 
            this.colStartPoint.FieldName = "StartPoint";
            this.colStartPoint.Name = "colStartPoint";
            this.colStartPoint.Visible = true;
            this.colStartPoint.VisibleIndex = 1;
            this.colStartPoint.Width = 150;
            this.colStartPoint.Caption = "Начальная точка";
            // 
            // colEndPoint
            // 
            this.colEndPoint.FieldName = "EndPoint";
            this.colEndPoint.Name = "colEndPoint";
            this.colEndPoint.Visible = true;
            this.colEndPoint.VisibleIndex = 2;
            this.colEndPoint.Width = 150;
            this.colEndPoint.Caption = "Конечная точка";
            // 
            // colTravelTime
            // 
            this.colTravelTime.FieldName = "TravelTime";
            this.colTravelTime.Name = "colTravelTime";
            this.colTravelTime.Visible = true;
            this.colTravelTime.VisibleIndex = 3;
            this.colTravelTime.Width = 100;
            this.colTravelTime.Caption = "Время в пути";
            // 
            // colBusModel
            // 
            this.colBusModel.FieldName = "Avtobus.Model"; // Unbound
            this.colBusModel.Name = "colBusModel";
            this.colBusModel.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colBusModel.Visible = true;
            this.colBusModel.VisibleIndex = 4;
            this.colBusModel.Width = 150;
            this.colBusModel.Caption = "Автобус";
            this.colBusModel.OptionsColumn.AllowEdit = false;
            // 
            // colDriverSurname
            // 
            this.colDriverSurname.FieldName = "Employee.Surname"; // Unbound
            this.colDriverSurname.Name = "colDriverSurname";
            this.colDriverSurname.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colDriverSurname.Visible = true;
            this.colDriverSurname.VisibleIndex = 5;
            this.colDriverSurname.Width = 150;
            this.colDriverSurname.Caption = "Водитель";
            this.colDriverSurname.OptionsColumn.AllowEdit = false;
            // 
            // colTicketsCount
            // 
            this.colTicketsCount.FieldName = "Tickets.Count"; // Unbound
            this.colTicketsCount.Name = "colTicketsCount";
            this.colTicketsCount.UnboundType = DevExpress.Data.UnboundColumnType.Integer;
            this.colTicketsCount.Visible = true;
            this.colTicketsCount.VisibleIndex = 6;
            this.colTicketsCount.Width = 120;
            this.colTicketsCount.Caption = "Кол-во билетов";
            this.colTicketsCount.OptionsColumn.AllowEdit = false;
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(12, 12);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Properties.NullValuePrompt = "Поиск по начальной/конечной точке, модели автобуса, водителю...";
            this.txtSearch.Size = new System.Drawing.Size(960, 20);
            this.txtSearch.StyleController = this.layoutControl1;
            this.txtSearch.TabIndex = 5;
            this.txtSearch.EditValueChanged += new System.EventHandler(this.txtSearch_EditValueChanged);
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(654, 527);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(74, 22);
            this.btnAdd.StyleController = this.layoutControl1;
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.Location = new System.Drawing.Point(732, 527);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(79, 22);
            this.btnEdit.StyleController = this.layoutControl1;
            this.btnEdit.TabIndex = 7;
            this.btnEdit.Text = "Редактировать";
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(815, 527);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(74, 22);
            this.btnDelete.StyleController = this.layoutControl1;
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(893, 527);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(79, 22);
            this.btnRefresh.StyleController = this.layoutControl1;
            this.btnRefresh.TabIndex = 9;
            this.btnRefresh.Text = "Обновить";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.emptySpaceItem1,
            this.layoutControlItem3,
            this.layoutControlItem4,
            this.layoutControlItem5,
            this.layoutControlItem6});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(984, 561);
            this.Root.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.gridControlRoutes;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 26);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(964, 489);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.txtSearch;
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(964, 26);
            this.layoutControlItem2.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem2.TextVisible = false;
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.btnAdd;
            this.layoutControlItem3.Location = new System.Drawing.Point(642, 515);
            this.layoutControlItem3.MaxSize = new System.Drawing.Size(78, 26);
            this.layoutControlItem3.MinSize = new System.Drawing.Size(78, 26);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Size = new System.Drawing.Size(78, 26);
            this.layoutControlItem3.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextVisible = false;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.btnEdit;
            this.layoutControlItem4.Location = new System.Drawing.Point(720, 515);
            this.layoutControlItem4.MaxSize = new System.Drawing.Size(83, 26);
            this.layoutControlItem4.MinSize = new System.Drawing.Size(83, 26);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Size = new System.Drawing.Size(83, 26);
            this.layoutControlItem4.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem4.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem4.TextVisible = false;
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.btnDelete;
            this.layoutControlItem5.Location = new System.Drawing.Point(803, 515);
            this.layoutControlItem5.MaxSize = new System.Drawing.Size(78, 26);
            this.layoutControlItem5.MinSize = new System.Drawing.Size(78, 26);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Size = new System.Drawing.Size(78, 26);
            this.layoutControlItem5.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem5.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem5.TextVisible = false;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.btnRefresh;
            this.layoutControlItem6.Location = new System.Drawing.Point(881, 515);
            this.layoutControlItem6.MaxSize = new System.Drawing.Size(83, 26);
            this.layoutControlItem6.MinSize = new System.Drawing.Size(83, 26);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Size = new System.Drawing.Size(83, 26);
            this.layoutControlItem6.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 515);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(642, 26);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // frmRouteManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 561);
            this.Controls.Add(this.layoutControl1);
            this.Name = "frmRouteManagement";
            this.Text = "Управление маршрутами";
            this.Load += new System.EventHandler(this.frmRouteManagement_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlRoutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.routeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewRoutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraGrid.GridControl gridControlRoutes;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewRoutes;
        private DevExpress.XtraEditors.TextEdit txtSearch;
        private DevExpress.XtraEditors.SimpleButton btnAdd;
        private DevExpress.XtraEditors.SimpleButton btnEdit;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.SimpleButton btnRefresh;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
        private System.Windows.Forms.BindingSource routeBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteId;
        private DevExpress.XtraGrid.Columns.GridColumn colStartPoint;
        private DevExpress.XtraGrid.Columns.GridColumn colEndPoint;
        private DevExpress.XtraGrid.Columns.GridColumn colTravelTime;
        private DevExpress.XtraGrid.Columns.GridColumn colBusModel;
        private DevExpress.XtraGrid.Columns.GridColumn colDriverSurname;
        private DevExpress.XtraGrid.Columns.GridColumn colTicketsCount;
    }
} 