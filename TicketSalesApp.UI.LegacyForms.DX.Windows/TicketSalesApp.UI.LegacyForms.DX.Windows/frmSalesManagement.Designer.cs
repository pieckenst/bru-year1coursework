using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmSalesManagement
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
           
            this.layoutControl = new DevExpress.XtraLayout.LayoutControl();
            this.btnExport = new DevExpress.XtraEditors.SimpleButton();
            this.btnApplyFilter = new DevExpress.XtraEditors.SimpleButton();
            this.dateToFilter = new DevExpress.XtraEditors.DateEdit();
            this.dateFromFilter = new DevExpress.XtraEditors.DateEdit();
            this.btnRefresh = new DevExpress.XtraEditors.SimpleButton();
            this.btnDelete = new DevExpress.XtraEditors.SimpleButton();
            this.btnEdit = new DevExpress.XtraEditors.SimpleButton();
            this.btnAdd = new DevExpress.XtraEditors.SimpleButton();
            this.gridControlSales = new DevExpress.XtraGrid.GridControl();
            this.salesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewSales = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colSaleId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSaleDate = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteScheduleId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRouteDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDepartureTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colArrivalTime = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colSeatNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colTotalAmount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPaymentMethod = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.layoutControlGroup = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemAdd = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemEdit = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDelete = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemRefresh = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemActions = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItemDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemApplyFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemExport = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemFilters = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).BeginInit();
            this.layoutControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSales)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSales)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemAdd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDelete)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemActions)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemExport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilters)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl
            // 
            this.layoutControl.Controls.Add(this.btnExport);
            this.layoutControl.Controls.Add(this.btnApplyFilter);
            this.layoutControl.Controls.Add(this.dateToFilter);
            this.layoutControl.Controls.Add(this.dateFromFilter);
            this.layoutControl.Controls.Add(this.btnRefresh);
            this.layoutControl.Controls.Add(this.btnDelete);
            this.layoutControl.Controls.Add(this.btnEdit);
            this.layoutControl.Controls.Add(this.btnAdd);
            this.layoutControl.Controls.Add(this.gridControlSales);
            this.layoutControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl.Location = new System.Drawing.Point(0, 0);
            this.layoutControl.Name = "layoutControl";
            this.layoutControl.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(1107, 211, 650, 400);
            this.layoutControl.Root = this.layoutControlGroup;
            this.layoutControl.Size = new System.Drawing.Size(984, 661);
            this.layoutControl.TabIndex = 0;
            this.layoutControl.Text = "layoutControl1";
            // 
            // btnExport
            // 
            
            this.btnExport.Location = new System.Drawing.Point(792, 627);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(88, 22);
            this.btnExport.StyleController = this.layoutControl;
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Экспорт";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnApplyFilter
            // 
            
            this.btnApplyFilter.Location = new System.Drawing.Point(426, 12);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(86, 22);
            this.btnApplyFilter.StyleController = this.layoutControl;
            this.btnApplyFilter.TabIndex = 3;
            this.btnApplyFilter.Text = "Применить";
            // 
            // dateToFilter
            // 
            this.dateToFilter.EditValue = null;
            this.dateToFilter.Location = new System.Drawing.Point(294, 12);
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
            this.dateToFilter.Size = new System.Drawing.Size(128, 20);
            this.dateToFilter.StyleController = this.layoutControl;
            this.dateToFilter.TabIndex = 2;
            // 
            // dateFromFilter
            // 
            this.dateFromFilter.EditValue = null;
            this.dateFromFilter.Location = new System.Drawing.Point(73, 12);
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
            this.dateFromFilter.Size = new System.Drawing.Size(156, 20);
            this.dateFromFilter.StyleController = this.layoutControl;
            this.dateFromFilter.TabIndex = 1;
            // 
            // btnRefresh
            // 
        
            this.btnRefresh.Location = new System.Drawing.Point(700, 627);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(88, 22);
            this.btnRefresh.StyleController = this.layoutControl;
            this.btnRefresh.TabIndex = 7;
            this.btnRefresh.Text = "Обновить";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnDelete
            // 
            
            this.btnDelete.Location = new System.Drawing.Point(608, 627);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(88, 22);
            this.btnDelete.StyleController = this.layoutControl;
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnEdit
            // 
          
            this.btnEdit.Location = new System.Drawing.Point(516, 627);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(88, 22);
            this.btnEdit.StyleController = this.layoutControl;
            this.btnEdit.TabIndex = 5;
            this.btnEdit.Text = "Изменить";
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnAdd
            // 
         
            this.btnAdd.Location = new System.Drawing.Point(424, 627);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(88, 22);
            this.btnAdd.StyleController = this.layoutControl;
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // gridControlSales
            // 
            this.gridControlSales.DataSource = this.salesBindingSource;
            this.gridControlSales.Location = new System.Drawing.Point(12, 38);
            this.gridControlSales.MainView = this.gridViewSales;
            this.gridControlSales.Name = "gridControlSales";
            this.gridControlSales.Size = new System.Drawing.Size(960, 585);
            this.gridControlSales.TabIndex = 0;
            this.gridControlSales.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewSales});
            // 
            // salesBindingSource
            // 
            this.salesBindingSource.DataSource = typeof(TicketSalesApp.UI.LegacyForms.DX.Windows.SaleViewModel);
            // 
            // gridViewSales
            // 
            this.gridViewSales.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colSaleId,
            this.colSaleDate,
            this.colRouteScheduleId,
            this.colRouteDescription,
            this.colDepartureTime,
            this.colArrivalTime,
            this.colSeatNumber,
            this.colTotalAmount,
            this.colPaymentMethod,
            this.colStatus});
            this.gridViewSales.GridControl = this.gridControlSales;
            this.gridViewSales.Name = "gridViewSales";
            this.gridViewSales.OptionsDetail.EnableMasterViewMode = false;
            this.gridViewSales.OptionsFind.AlwaysVisible = true;
            this.gridViewSales.OptionsView.ShowGroupPanel = false;
            // 
            // colSaleId
            // 
            this.colSaleId.Caption = "ID";
            this.colSaleId.FieldName = "SaleId";
            this.colSaleId.Name = "colSaleId";
            this.colSaleId.Visible = true;
            this.colSaleId.VisibleIndex = 0;
            this.colSaleId.Width = 50;
            // 
            // colSaleDate
            // 
            this.colSaleDate.Caption = "Дата Продажи";
            this.colSaleDate.FieldName = "SaleDate";
            this.colSaleDate.Name = "colSaleDate";
            this.colSaleDate.Visible = true;
            this.colSaleDate.VisibleIndex = 1;
            this.colSaleDate.Width = 110;
            // 
            // colRouteScheduleId
            // 
            this.colRouteScheduleId.FieldName = "RouteScheduleId";
            this.colRouteScheduleId.Name = "colRouteScheduleId";
            // 
            // colRouteDescription
            // 
            this.colRouteDescription.Caption = "Маршрут";
            this.colRouteDescription.FieldName = "RouteDescription";
            this.colRouteDescription.Name = "colRouteDescription";
            this.colRouteDescription.Visible = true;
            this.colRouteDescription.VisibleIndex = 2;
            this.colRouteDescription.Width = 180;
            // 
            // colDepartureTime
            // 
            this.colDepartureTime.Caption = "Отправление";
            this.colDepartureTime.FieldName = "DepartureTime";
            this.colDepartureTime.Name = "colDepartureTime";
            this.colDepartureTime.Visible = true;
            this.colDepartureTime.VisibleIndex = 3;
            this.colDepartureTime.Width = 90;
            // 
            // colArrivalTime
            // 
            this.colArrivalTime.Caption = "Прибытие";
            this.colArrivalTime.FieldName = "ArrivalTime";
            this.colArrivalTime.Name = "colArrivalTime";
            this.colArrivalTime.Visible = true;
            this.colArrivalTime.VisibleIndex = 4;
            this.colArrivalTime.Width = 90;
            // 
            // colSeatNumber
            // 
            this.colSeatNumber.Caption = "Место";
            this.colSeatNumber.FieldName = "SeatNumber";
            this.colSeatNumber.Name = "colSeatNumber";
            this.colSeatNumber.Visible = true;
            this.colSeatNumber.VisibleIndex = 5;
            this.colSeatNumber.Width = 50;
            // 
            // colTotalAmount
            // 
            this.colTotalAmount.Caption = "Сумма";
            this.colTotalAmount.FieldName = "TotalAmount";
            this.colTotalAmount.Name = "colTotalAmount";
            this.colTotalAmount.Visible = true;
            this.colTotalAmount.VisibleIndex = 6;
            this.colTotalAmount.Width = 80;
            // 
            // colPaymentMethod
            // 
            this.colPaymentMethod.Caption = "Оплата";
            this.colPaymentMethod.FieldName = "PaymentMethod";
            this.colPaymentMethod.Name = "colPaymentMethod";
            this.colPaymentMethod.Visible = true;
            this.colPaymentMethod.VisibleIndex = 7;
            this.colPaymentMethod.Width = 80;
            // 
            // colStatus
            // 
            this.colStatus.Caption = "Статус";
            this.colStatus.FieldName = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.Visible = true;
            this.colStatus.VisibleIndex = 8;
            this.colStatus.Width = 80;
            // 
            // layoutControlGroup
            // 
            this.layoutControlGroup.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup.GroupBordersVisible = false;
            this.layoutControlGroup.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemGrid,
            this.layoutControlItemAdd,
            this.layoutControlItemEdit,
            this.layoutControlItemDelete,
            this.layoutControlItemRefresh,
            this.emptySpaceItemActions,
            this.layoutControlItemDateFrom,
            this.layoutControlItemDateTo,
            this.layoutControlItemApplyFilter,
            this.layoutControlItemExport,
            this.emptySpaceItemFilters});
            this.layoutControlGroup.Name = "Root";
            this.layoutControlGroup.Size = new System.Drawing.Size(984, 661);
            this.layoutControlGroup.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            this.layoutControlItemGrid.Control = this.gridControlSales;
            this.layoutControlItemGrid.Location = new System.Drawing.Point(0, 26);
            this.layoutControlItemGrid.Name = "layoutControlItemGrid";
            this.layoutControlItemGrid.Size = new System.Drawing.Size(964, 589);
            this.layoutControlItemGrid.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemGrid.TextVisible = false;
            // 
            // layoutControlItemAdd
            // 
            this.layoutControlItemAdd.Control = this.btnAdd;
            this.layoutControlItemAdd.Location = new System.Drawing.Point(412, 615);
            this.layoutControlItemAdd.MaxSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemAdd.MinSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemAdd.Name = "layoutControlItemAdd";
            this.layoutControlItemAdd.Size = new System.Drawing.Size(92, 26);
            this.layoutControlItemAdd.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemAdd.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemAdd.TextVisible = false;
            // 
            // layoutControlItemEdit
            // 
            this.layoutControlItemEdit.Control = this.btnEdit;
            this.layoutControlItemEdit.Location = new System.Drawing.Point(504, 615);
            this.layoutControlItemEdit.MaxSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemEdit.MinSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemEdit.Name = "layoutControlItemEdit";
            this.layoutControlItemEdit.Size = new System.Drawing.Size(92, 26);
            this.layoutControlItemEdit.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemEdit.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemEdit.TextVisible = false;
            // 
            // layoutControlItemDelete
            // 
            this.layoutControlItemDelete.Control = this.btnDelete;
            this.layoutControlItemDelete.Location = new System.Drawing.Point(596, 615);
            this.layoutControlItemDelete.MaxSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemDelete.MinSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemDelete.Name = "layoutControlItemDelete";
            this.layoutControlItemDelete.Size = new System.Drawing.Size(92, 26);
            this.layoutControlItemDelete.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDelete.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemDelete.TextVisible = false;
            // 
            // layoutControlItemRefresh
            // 
            this.layoutControlItemRefresh.Control = this.btnRefresh;
            this.layoutControlItemRefresh.Location = new System.Drawing.Point(688, 615);
            this.layoutControlItemRefresh.MaxSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemRefresh.MinSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemRefresh.Name = "layoutControlItemRefresh";
            this.layoutControlItemRefresh.Size = new System.Drawing.Size(92, 26);
            this.layoutControlItemRefresh.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemRefresh.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemRefresh.TextVisible = false;
            // 
            // emptySpaceItemActions
            // 
            this.emptySpaceItemActions.AllowHotTrack = false;
            this.emptySpaceItemActions.Location = new System.Drawing.Point(0, 615);
            this.emptySpaceItemActions.Name = "emptySpaceItemActions";
            this.emptySpaceItemActions.Size = new System.Drawing.Size(412, 26);
            this.emptySpaceItemActions.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItemDateFrom
            // 
            this.layoutControlItemDateFrom.Control = this.dateFromFilter;
            this.layoutControlItemDateFrom.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItemDateFrom.MaxSize = new System.Drawing.Size(221, 26);
            this.layoutControlItemDateFrom.MinSize = new System.Drawing.Size(221, 26);
            this.layoutControlItemDateFrom.Name = "layoutControlItemDateFrom";
            this.layoutControlItemDateFrom.Size = new System.Drawing.Size(221, 26);
            this.layoutControlItemDateFrom.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDateFrom.Text = "Дата С:";
            this.layoutControlItemDateFrom.TextSize = new System.Drawing.Size(58, 13);
            // 
            // layoutControlItemDateTo
            // 
            this.layoutControlItemDateTo.Control = this.dateToFilter;
            this.layoutControlItemDateTo.Location = new System.Drawing.Point(221, 0);
            this.layoutControlItemDateTo.MaxSize = new System.Drawing.Size(193, 26);
            this.layoutControlItemDateTo.MinSize = new System.Drawing.Size(193, 26);
            this.layoutControlItemDateTo.Name = "layoutControlItemDateTo";
            this.layoutControlItemDateTo.Size = new System.Drawing.Size(193, 26);
            this.layoutControlItemDateTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDateTo.Text = "По:";
            this.layoutControlItemDateTo.TextSize = new System.Drawing.Size(58, 13);
            // 
            // layoutControlItemApplyFilter
            // 
            this.layoutControlItemApplyFilter.Control = this.btnApplyFilter;
            this.layoutControlItemApplyFilter.Location = new System.Drawing.Point(414, 0);
            this.layoutControlItemApplyFilter.MaxSize = new System.Drawing.Size(90, 26);
            this.layoutControlItemApplyFilter.MinSize = new System.Drawing.Size(90, 26);
            this.layoutControlItemApplyFilter.Name = "layoutControlItemApplyFilter";
            this.layoutControlItemApplyFilter.Size = new System.Drawing.Size(90, 26);
            this.layoutControlItemApplyFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemApplyFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemApplyFilter.TextVisible = false;
            // 
            // layoutControlItemExport
            // 
            this.layoutControlItemExport.Control = this.btnExport;
            this.layoutControlItemExport.Location = new System.Drawing.Point(780, 615);
            this.layoutControlItemExport.MaxSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemExport.MinSize = new System.Drawing.Size(92, 26);
            this.layoutControlItemExport.Name = "layoutControlItemExport";
            this.layoutControlItemExport.Size = new System.Drawing.Size(184, 26);
            this.layoutControlItemExport.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemExport.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemExport.TextVisible = false;
            // 
            // emptySpaceItemFilters
            // 
            this.emptySpaceItemFilters.AllowHotTrack = false;
            this.emptySpaceItemFilters.Location = new System.Drawing.Point(504, 0);
            this.emptySpaceItemFilters.Name = "emptySpaceItemFilters";
            this.emptySpaceItemFilters.Size = new System.Drawing.Size(460, 26);
            this.emptySpaceItemFilters.TextSize = new System.Drawing.Size(0, 0);
            // 
            // frmSalesManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.layoutControl);
        
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "frmSalesManagement";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Управление Продажами";
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl)).EndInit();
            this.layoutControl.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlSales)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.salesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewSales)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemAdd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDelete)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRefresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemActions)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemExport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilters)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup;
        private DevExpress.XtraGrid.GridControl gridControlSales;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewSales;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemGrid;
        private DevExpress.XtraEditors.SimpleButton btnAdd;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemAdd;
        private DevExpress.XtraEditors.SimpleButton btnRefresh;
        private DevExpress.XtraEditors.SimpleButton btnDelete;
        private DevExpress.XtraEditors.SimpleButton btnEdit;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemEdit;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDelete;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRefresh;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemActions;
        private System.Windows.Forms.BindingSource salesBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colSaleId;
        private DevExpress.XtraGrid.Columns.GridColumn colSaleDate;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteScheduleId;
        private DevExpress.XtraGrid.Columns.GridColumn colRouteDescription;
        private DevExpress.XtraGrid.Columns.GridColumn colDepartureTime;
        private DevExpress.XtraGrid.Columns.GridColumn colArrivalTime;
        private DevExpress.XtraGrid.Columns.GridColumn colSeatNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colTotalAmount;
        private DevExpress.XtraGrid.Columns.GridColumn colPaymentMethod;
        private DevExpress.XtraGrid.Columns.GridColumn colStatus;
        private DevExpress.XtraEditors.DateEdit dateToFilter;
        private DevExpress.XtraEditors.DateEdit dateFromFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateFrom;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateTo;
        private DevExpress.XtraEditors.SimpleButton btnExport;
        private DevExpress.XtraEditors.SimpleButton btnApplyFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemApplyFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemExport;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemFilters;
    }
} 