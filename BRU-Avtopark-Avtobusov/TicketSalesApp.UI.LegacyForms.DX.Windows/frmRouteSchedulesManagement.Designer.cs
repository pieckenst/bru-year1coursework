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
            components = new System.ComponentModel.Container();
            layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            gridControlSchedules = new DevExpress.XtraGrid.GridControl();
            routeScheduleBindingSource = new System.Windows.Forms.BindingSource(components);
            gridViewSchedules = new DevExpress.XtraGrid.Views.Grid.GridView();
            colRouteScheduleId = new DevExpress.XtraGrid.Columns.GridColumn();
            colRouteStartPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            colRouteEndPoint = new DevExpress.XtraGrid.Columns.GridColumn();
            colDepartureTime = new DevExpress.XtraGrid.Columns.GridColumn();
            colArrivalTime = new DevExpress.XtraGrid.Columns.GridColumn();
            colPrice = new DevExpress.XtraGrid.Columns.GridColumn();
            colAvailableSeats = new DevExpress.XtraGrid.Columns.GridColumn();
            colIsActive = new DevExpress.XtraGrid.Columns.GridColumn();
            repositoryItemCheckEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            lueRouteFilter = new DevExpress.XtraEditors.LookUpEdit();
            routeBindingSource = new System.Windows.Forms.BindingSource(components);
            dateFilter = new DevExpress.XtraEditors.DateEdit();
            btnAdd = new DevExpress.XtraEditors.SimpleButton();
            btnEdit = new DevExpress.XtraEditors.SimpleButton();
            btnDelete = new DevExpress.XtraEditors.SimpleButton();
            btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            Root = new DevExpress.XtraLayout.LayoutControlGroup();
            layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlGroupFilters = new DevExpress.XtraLayout.LayoutControlGroup();
            layoutControlItemRouteFilter = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemDateFilter = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemAdd = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemRefresh = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemEdit = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItemDelete = new DevExpress.XtraLayout.LayoutControlItem();
            emptySpaceItemButtons = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)layoutControl1).BeginInit();
            layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControlSchedules).BeginInit();
            ((System.ComponentModel.ISupportInitialize)routeScheduleBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridViewSchedules).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemCheckEdit1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lueRouteFilter.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)routeBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateFilter.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dateFilter.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)Root).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroupFilters).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemRouteFilter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemDateFilter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemAdd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemRefresh).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemDelete).BeginInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceItemButtons).BeginInit();
            SuspendLayout();
            // 
            // layoutControl1
            // 
            layoutControl1.Controls.Add(gridControlSchedules);
            layoutControl1.Controls.Add(lueRouteFilter);
            layoutControl1.Controls.Add(dateFilter);
            layoutControl1.Controls.Add(btnAdd);
            layoutControl1.Controls.Add(btnEdit);
            layoutControl1.Controls.Add(btnDelete);
            layoutControl1.Controls.Add(btnRefresh);
            layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            layoutControl1.Location = new System.Drawing.Point(0, 0);
            layoutControl1.Name = "layoutControl1";
            layoutControl1.Root = Root;
            layoutControl1.Size = new System.Drawing.Size(864, 561);
            layoutControl1.TabIndex = 0;
            layoutControl1.Text = "layoutControl1";
            // 
            // gridControlSchedules
            // 
            gridControlSchedules.DataSource = routeScheduleBindingSource;
            gridControlSchedules.Location = new System.Drawing.Point(12, 38);
            gridControlSchedules.MainView = gridViewSchedules;
            gridControlSchedules.Name = "gridControlSchedules";
            gridControlSchedules.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemCheckEdit1 });
            gridControlSchedules.Size = new System.Drawing.Size(840, 485);
            gridControlSchedules.TabIndex = 4;
            gridControlSchedules.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridViewSchedules });
            // 
            // gridViewSchedules
            // 
            gridViewSchedules.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { colRouteScheduleId, colRouteStartPoint, colRouteEndPoint, colDepartureTime, colArrivalTime, colPrice, colAvailableSeats, colIsActive });
            gridViewSchedules.GridControl = gridControlSchedules;
            gridViewSchedules.Name = "gridViewSchedules";
            gridViewSchedules.OptionsBehavior.Editable = false;
            gridViewSchedules.OptionsView.ShowGroupPanel = false;
            gridViewSchedules.FocusedRowChanged += gridViewSchedules_FocusedRowChanged;
            gridViewSchedules.CustomUnboundColumnData += gridViewSchedules_CustomUnboundColumnData;
            // 
            // colRouteScheduleId
            // 
            colRouteScheduleId.Caption = "ID";
            colRouteScheduleId.FieldName = "RouteScheduleId";
            colRouteScheduleId.Name = "colRouteScheduleId";
            colRouteScheduleId.Visible = true;
            colRouteScheduleId.VisibleIndex = 0;
            colRouteScheduleId.Width = 40;
            // 
            // colRouteStartPoint
            // 
            colRouteStartPoint.Caption = "Маршрут (Начало)";
            colRouteStartPoint.FieldName = "Marshut.StartPoint";
            colRouteStartPoint.Name = "colRouteStartPoint";
            colRouteStartPoint.OptionsColumn.AllowEdit = false;
            colRouteStartPoint.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colRouteStartPoint.Visible = true;
            colRouteStartPoint.VisibleIndex = 1;
            colRouteStartPoint.Width = 150;
            // 
            // colRouteEndPoint
            // 
            colRouteEndPoint.Caption = "Маршрут (Конец)";
            colRouteEndPoint.FieldName = "Marshut.EndPoint";
            colRouteEndPoint.Name = "colRouteEndPoint";
            colRouteEndPoint.OptionsColumn.AllowEdit = false;
            colRouteEndPoint.UnboundType = DevExpress.Data.UnboundColumnType.String;
            colRouteEndPoint.Visible = true;
            colRouteEndPoint.VisibleIndex = 2;
            colRouteEndPoint.Width = 150;
            // 
            // colDepartureTime
            // 
            colDepartureTime.Caption = "Отправление";
            colDepartureTime.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colDepartureTime.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colDepartureTime.FieldName = "DepartureTime";
            colDepartureTime.Name = "colDepartureTime";
            colDepartureTime.Visible = true;
            colDepartureTime.VisibleIndex = 3;
            colDepartureTime.Width = 120;
            // 
            // colArrivalTime
            // 
            colArrivalTime.Caption = "Прибытие";
            colArrivalTime.DisplayFormat.FormatString = "dd.MM.yyyy HH:mm";
            colArrivalTime.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            colArrivalTime.FieldName = "ArrivalTime";
            colArrivalTime.Name = "colArrivalTime";
            colArrivalTime.Visible = true;
            colArrivalTime.VisibleIndex = 4;
            colArrivalTime.Width = 120;
            // 
            // colPrice
            // 
            colPrice.Caption = "Цена";
            colPrice.DisplayFormat.FormatString = "C2";
            colPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            colPrice.FieldName = "Price";
            colPrice.Name = "colPrice";
            colPrice.Visible = true;
            colPrice.VisibleIndex = 5;
            colPrice.Width = 80;
            // 
            // colAvailableSeats
            // 
            colAvailableSeats.Caption = "Места";
            colAvailableSeats.FieldName = "AvailableSeats";
            colAvailableSeats.Name = "colAvailableSeats";
            colAvailableSeats.Visible = true;
            colAvailableSeats.VisibleIndex = 6;
            colAvailableSeats.Width = 80;
            // 
            // colIsActive
            // 
            colIsActive.Caption = "Активен";
            colIsActive.ColumnEdit = repositoryItemCheckEdit1;
            colIsActive.FieldName = "IsActive";
            colIsActive.Name = "colIsActive";
            colIsActive.Visible = true;
            colIsActive.VisibleIndex = 7;
            colIsActive.Width = 60;
            // 
            // repositoryItemCheckEdit1
            // 
            repositoryItemCheckEdit1.AutoHeight = false;
            repositoryItemCheckEdit1.Name = "repositoryItemCheckEdit1";
            // 
            // lueRouteFilter
            // 
            lueRouteFilter.Location = new System.Drawing.Point(12, 12);
            lueRouteFilter.Name = "lueRouteFilter";
            lueRouteFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            lueRouteFilter.Properties.Columns.AddRange(new DevExpress.XtraEditors.Controls.LookUpColumnInfo[] { new DevExpress.XtraEditors.Controls.LookUpColumnInfo("RouteId", "ID", 40, DevExpress.Utils.FormatType.None, "", true, DevExpress.Utils.HorzAlignment.Default, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.Default), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("StartPoint", "Начало", 150, DevExpress.Utils.FormatType.None, "", true, DevExpress.Utils.HorzAlignment.Default, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.Default), new DevExpress.XtraEditors.Controls.LookUpColumnInfo("EndPoint", "Конец", 150, DevExpress.Utils.FormatType.None, "", true, DevExpress.Utils.HorzAlignment.Default, DevExpress.Data.ColumnSortOrder.None, DevExpress.Utils.DefaultBoolean.Default) });
            lueRouteFilter.Properties.DataSource = routeBindingSource;
            lueRouteFilter.Properties.DisplayMember = "StartPoint";
            lueRouteFilter.Properties.NullText = "[Все маршруты]";
            lueRouteFilter.Properties.ValueMember = "RouteId";
            lueRouteFilter.Size = new System.Drawing.Size(323, 20);
            lueRouteFilter.StyleController = layoutControl1;
            lueRouteFilter.TabIndex = 5;
            // 
            // dateFilter
            // 
            dateFilter.EditValue = null;
            dateFilter.Location = new System.Drawing.Point(339, 12);
            dateFilter.Name = "dateFilter";
            dateFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateFilter.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            dateFilter.Properties.NullText = "[Все даты]";
            dateFilter.Size = new System.Drawing.Size(250, 20);
            dateFilter.StyleController = layoutControl1;
            dateFilter.TabIndex = 6;
            // 
            // btnAdd
            // 
            btnAdd.Location = new System.Drawing.Point(593, 12);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(120, 22);
            btnAdd.StyleController = layoutControl1;
            btnAdd.TabIndex = 7;
            btnAdd.Text = "Добавить расписание";
            btnAdd.Click += btnAdd_Click;
            // 
            // btnEdit
            // 
            btnEdit.Location = new System.Drawing.Point(643, 527);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new System.Drawing.Size(102, 22);
            btnEdit.StyleController = layoutControl1;
            btnEdit.TabIndex = 8;
            btnEdit.Text = "Редактировать";
            btnEdit.Click += btnEdit_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(749, 527);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(103, 22);
            btnDelete.StyleController = layoutControl1;
            btnDelete.TabIndex = 9;
            btnDelete.Text = "Удалить";
            btnDelete.Click += btnDelete_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(717, 12);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(135, 22);
            btnRefresh.StyleController = layoutControl1;
            btnRefresh.TabIndex = 10;
            btnRefresh.Text = "Обновить";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // Root
            // 
            Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            Root.GroupBordersVisible = false;
            Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] { layoutControlItemGrid, layoutControlGroupFilters, layoutControlItemEdit, layoutControlItemDelete, emptySpaceItemButtons });
            Root.Name = "Root";
            Root.Size = new System.Drawing.Size(864, 561);
            Root.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            layoutControlItemGrid.Control = gridControlSchedules;
            layoutControlItemGrid.Location = new System.Drawing.Point(0, 26);
            layoutControlItemGrid.Name = "layoutControlItemGrid";
            layoutControlItemGrid.Size = new System.Drawing.Size(844, 489);
            layoutControlItemGrid.TextVisible = false;
            // 
            // layoutControlGroupFilters
            // 
            layoutControlGroupFilters.GroupBordersVisible = false;
            layoutControlGroupFilters.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] { layoutControlItemRouteFilter, layoutControlItemDateFilter, layoutControlItemAdd, layoutControlItemRefresh });
            layoutControlGroupFilters.Location = new System.Drawing.Point(0, 0);
            layoutControlGroupFilters.Name = "layoutControlGroupFilters";
            layoutControlGroupFilters.Size = new System.Drawing.Size(844, 26);
            layoutControlGroupFilters.Spacing = new DevExpress.XtraLayout.Utils.Padding(12, 12, 16, 10);
            layoutControlGroupFilters.Text = "Фильтры";
            // 
            // layoutControlItemRouteFilter
            // 
            layoutControlItemRouteFilter.Control = lueRouteFilter;
            layoutControlItemRouteFilter.Location = new System.Drawing.Point(0, 0);
            layoutControlItemRouteFilter.Name = "layoutControlItemRouteFilter";
            layoutControlItemRouteFilter.Size = new System.Drawing.Size(327, 26);
            layoutControlItemRouteFilter.Text = "Маршрут:";
            layoutControlItemRouteFilter.TextVisible = false;
            // 
            // layoutControlItemDateFilter
            // 
            layoutControlItemDateFilter.Control = dateFilter;
            layoutControlItemDateFilter.Location = new System.Drawing.Point(327, 0);
            layoutControlItemDateFilter.Name = "layoutControlItemDateFilter";
            layoutControlItemDateFilter.Size = new System.Drawing.Size(254, 26);
            layoutControlItemDateFilter.Text = "Дата:";
            layoutControlItemDateFilter.TextVisible = false;
            // 
            // layoutControlItemAdd
            // 
            layoutControlItemAdd.Control = btnAdd;
            layoutControlItemAdd.Location = new System.Drawing.Point(581, 0);
            layoutControlItemAdd.Name = "layoutControlItemAdd";
            layoutControlItemAdd.Size = new System.Drawing.Size(124, 26);
            layoutControlItemAdd.TextVisible = false;
            // 
            // layoutControlItemRefresh
            // 
            layoutControlItemRefresh.Control = btnRefresh;
            layoutControlItemRefresh.Location = new System.Drawing.Point(705, 0);
            layoutControlItemRefresh.Name = "layoutControlItemRefresh";
            layoutControlItemRefresh.Size = new System.Drawing.Size(139, 26);
            layoutControlItemRefresh.TextVisible = false;
            // 
            // layoutControlItemEdit
            // 
            layoutControlItemEdit.Control = btnEdit;
            layoutControlItemEdit.Location = new System.Drawing.Point(631, 515);
            layoutControlItemEdit.MaxSize = new System.Drawing.Size(106, 26);
            layoutControlItemEdit.MinSize = new System.Drawing.Size(106, 26);
            layoutControlItemEdit.Name = "layoutControlItemEdit";
            layoutControlItemEdit.Size = new System.Drawing.Size(106, 26);
            layoutControlItemEdit.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemEdit.TextVisible = false;
            // 
            // layoutControlItemDelete
            // 
            layoutControlItemDelete.Control = btnDelete;
            layoutControlItemDelete.Location = new System.Drawing.Point(737, 515);
            layoutControlItemDelete.MaxSize = new System.Drawing.Size(107, 26);
            layoutControlItemDelete.MinSize = new System.Drawing.Size(107, 26);
            layoutControlItemDelete.Name = "layoutControlItemDelete";
            layoutControlItemDelete.Size = new System.Drawing.Size(107, 26);
            layoutControlItemDelete.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            layoutControlItemDelete.TextVisible = false;
            // 
            // emptySpaceItemButtons
            // 
            emptySpaceItemButtons.Location = new System.Drawing.Point(0, 515);
            emptySpaceItemButtons.Name = "emptySpaceItemButtons";
            emptySpaceItemButtons.Size = new System.Drawing.Size(631, 26);
            // 
            // frmRouteSchedulesManagement
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(864, 561);
            Controls.Add(layoutControl1);
            Name = "frmRouteSchedulesManagement";
            Text = "Управление расписанием маршрутов";
            Load += frmRouteSchedulesManagement_Load;
            ((System.ComponentModel.ISupportInitialize)layoutControl1).EndInit();
            layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControlSchedules).EndInit();
            ((System.ComponentModel.ISupportInitialize)routeScheduleBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridViewSchedules).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemCheckEdit1).EndInit();
            ((System.ComponentModel.ISupportInitialize)lueRouteFilter.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)routeBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateFilter.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)dateFilter.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)Root).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroupFilters).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemRouteFilter).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemDateFilter).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemAdd).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemRefresh).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItemDelete).EndInit();
            ((System.ComponentModel.ISupportInitialize)emptySpaceItemButtons).EndInit();
            ResumeLayout(false);

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
        private DevExpress.XtraGrid.Columns.GridColumn colDepartureTime;
        private DevExpress.XtraGrid.Columns.GridColumn colArrivalTime;
        private DevExpress.XtraGrid.Columns.GridColumn colPrice;
        private DevExpress.XtraGrid.Columns.GridColumn colAvailableSeats;
        private DevExpress.XtraGrid.Columns.GridColumn colIsActive;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemCheckEdit1;
    }
} 