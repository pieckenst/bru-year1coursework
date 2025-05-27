using System.ComponentModel;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmIncomeReport
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
            this.btnExport = new DevExpress.XtraEditors.SimpleButton();
            this.btnApplyFilter = new DevExpress.XtraEditors.SimpleButton();
            this.lueRouteFilter = new DevExpress.XtraEditors.LookUpEdit();
            this.dateToFilter = new DevExpress.XtraEditors.DateEdit();
            this.dateFromFilter = new DevExpress.XtraEditors.DateEdit();
            this.gridControlReport = new DevExpress.XtraGrid.GridControl();
            this.saleViewModelBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewReport = new DevExpress.XtraGrid.Views.Grid.GridView();
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
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemGrid = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemRouteFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemApplyFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItemExport = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.colPassengerName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPassengerPhone = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlReport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleViewModelBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewReport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemExport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.btnExport);
            this.layoutControl1.Controls.Add(this.btnApplyFilter);
            this.layoutControl1.Controls.Add(this.lueRouteFilter);
            this.layoutControl1.Controls.Add(this.dateToFilter);
            this.layoutControl1.Controls.Add(this.dateFromFilter);
            this.layoutControl1.Controls.Add(this.gridControlReport);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.Root;
            this.layoutControl1.Size = new System.Drawing.Size(884, 561);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(781, 527);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(91, 22);
            this.btnExport.StyleController = this.layoutControl1;
            this.btnExport.TabIndex = 5;
            this.btnExport.Text = "Экспорт";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnApplyFilter
            // 
            this.btnApplyFilter.Location = new System.Drawing.Point(591, 12);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(98, 22);
            this.btnApplyFilter.StyleController = this.layoutControl1;
            this.btnApplyFilter.TabIndex = 3;
            this.btnApplyFilter.Text = "Применить";
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
            // 
            // lueRouteFilter
            // 
            this.lueRouteFilter.Location = new System.Drawing.Point(397, 12);
            this.lueRouteFilter.Name = "lueRouteFilter";
            this.lueRouteFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.lueRouteFilter.Properties.NullText = "[Все маршруты]";
            this.lueRouteFilter.Size = new System.Drawing.Size(190, 20);
            this.lueRouteFilter.StyleController = this.layoutControl1;
            this.lueRouteFilter.TabIndex = 2;
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
            // gridControlReport
            // 
            this.gridControlReport.DataSource = this.saleViewModelBindingSource;
            this.gridControlReport.Location = new System.Drawing.Point(12, 38);
            this.gridControlReport.MainView = this.gridViewReport;
            this.gridControlReport.Name = "gridControlReport";
            this.gridControlReport.Size = new System.Drawing.Size(860, 485);
            this.gridControlReport.TabIndex = 4;
            this.gridControlReport.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewReport});
            // 
            // saleViewModelBindingSource
            // 
            this.saleViewModelBindingSource.DataSource = typeof(TicketSalesApp.UI.LegacyForms.DX.Windows.IncomeReport_SaleViewModel);
            // 
            // gridViewReport
            // 
            this.gridViewReport.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colSaleId,
            this.colSaleDate,
            this.colRouteScheduleId,
            this.colRouteDescription,
            this.colDepartureTime,
            this.colArrivalTime,
            this.colSeatNumber,
            this.colTotalAmount,
            this.colPaymentMethod,
            this.colPassengerName,
            this.colPassengerPhone,
            this.colStatus});
            this.gridViewReport.GridControl = this.gridControlReport;
            this.gridViewReport.Name = "gridViewReport";
            this.gridViewReport.OptionsView.ShowFooter = true;
            // 
            // colSaleId
            // 
            this.colSaleId.FieldName = "SaleId";
            this.colSaleId.Name = "colSaleId";
            this.colSaleId.Visible = true;
            this.colSaleId.VisibleIndex = 0;
            this.colSaleId.Width = 60;
            // 
            // colSaleDate
            // 
            this.colSaleDate.Caption = "Дата Продажи";
            this.colSaleDate.FieldName = "SaleDate";
            this.colSaleDate.Name = "colSaleDate";
            this.colSaleDate.Visible = true;
            this.colSaleDate.VisibleIndex = 1;
            this.colSaleDate.Width = 120;
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
            this.colRouteDescription.Width = 200;
            // 
            // colDepartureTime
            // 
            this.colDepartureTime.Caption = "Отправление";
            this.colDepartureTime.FieldName = "DepartureTime";
            this.colDepartureTime.Name = "colDepartureTime";
            this.colDepartureTime.Visible = true;
            this.colDepartureTime.VisibleIndex = 3;
            this.colDepartureTime.Width = 100;
            // 
            // colArrivalTime
            // 
            this.colArrivalTime.Caption = "Прибытие";
            this.colArrivalTime.FieldName = "ArrivalTime";
            this.colArrivalTime.Name = "colArrivalTime";
            this.colArrivalTime.Visible = true;
            this.colArrivalTime.VisibleIndex = 4;
            this.colArrivalTime.Width = 100;
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
            this.colTotalAmount.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] {
            new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "TotalAmount", "SUM={0:0.##}")});
            this.colTotalAmount.Visible = true;
            this.colTotalAmount.VisibleIndex = 6;
            this.colTotalAmount.Width = 90;
            // 
            // colPaymentMethod
            // 
            this.colPaymentMethod.Caption = "Метод Оплаты";
            this.colPaymentMethod.FieldName = "PaymentMethod";
            this.colPaymentMethod.Name = "colPaymentMethod";
            this.colPaymentMethod.Visible = true;
            this.colPaymentMethod.VisibleIndex = 7;
            this.colPaymentMethod.Width = 90;
            // 
            // colStatus
            // 
            this.colStatus.Caption = "Статус";
            this.colStatus.FieldName = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.Visible = true;
            this.colStatus.VisibleIndex = 9;
            this.colStatus.Width = 100;
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemGrid,
            this.layoutControlItemDateFrom,
            this.layoutControlItemDateTo,
            this.layoutControlItemRouteFilter,
            this.layoutControlItemApplyFilter,
            this.emptySpaceItem1,
            this.layoutControlItemExport,
            this.emptySpaceItem2});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(884, 561);
            this.Root.TextVisible = false;
            // 
            // layoutControlItemGrid
            // 
            this.layoutControlItemGrid.Control = this.gridControlReport;
            this.layoutControlItemGrid.Location = new System.Drawing.Point(0, 26);
            this.layoutControlItemGrid.Name = "layoutControlItemGrid";
            this.layoutControlItemGrid.Size = new System.Drawing.Size(864, 489);
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
            // layoutControlItemRouteFilter
            // 
            this.layoutControlItemRouteFilter.Control = this.lueRouteFilter;
            this.layoutControlItemRouteFilter.Location = new System.Drawing.Point(333, 0);
            this.layoutControlItemRouteFilter.MaxSize = new System.Drawing.Size(246, 26);
            this.layoutControlItemRouteFilter.MinSize = new System.Drawing.Size(100, 26);
            this.layoutControlItemRouteFilter.Name = "layoutControlItemRouteFilter";
            this.layoutControlItemRouteFilter.Size = new System.Drawing.Size(246, 26);
            this.layoutControlItemRouteFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemRouteFilter.Text = "Маршрут:";
            this.layoutControlItemRouteFilter.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItemRouteFilter.TextSize = new System.Drawing.Size(47, 13);
            this.layoutControlItemRouteFilter.TextToControlDistance = 5;
            // 
            // layoutControlItemApplyFilter
            // 
            this.layoutControlItemApplyFilter.Control = this.btnApplyFilter;
            this.layoutControlItemApplyFilter.Location = new System.Drawing.Point(579, 0);
            this.layoutControlItemApplyFilter.MaxSize = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.MinSize = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.Name = "layoutControlItemApplyFilter";
            this.layoutControlItemApplyFilter.Size = new System.Drawing.Size(102, 26);
            this.layoutControlItemApplyFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemApplyFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemApplyFilter.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(681, 0);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(183, 26);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItemExport
            // 
            this.layoutControlItemExport.Control = this.btnExport;
            this.layoutControlItemExport.Location = new System.Drawing.Point(769, 515);
            this.layoutControlItemExport.MaxSize = new System.Drawing.Size(95, 26);
            this.layoutControlItemExport.MinSize = new System.Drawing.Size(95, 26);
            this.layoutControlItemExport.Name = "layoutControlItemExport";
            this.layoutControlItemExport.Size = new System.Drawing.Size(95, 26);
            this.layoutControlItemExport.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemExport.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemExport.TextVisible = false;
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 515);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(769, 26);
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // colPassengerName
            // 
            this.colPassengerName.Caption = "Пассажир";
            this.colPassengerName.FieldName = "PassengerName";
            this.colPassengerName.Name = "colPassengerName";
            this.colPassengerName.Visible = true;
            this.colPassengerName.VisibleIndex = 7;
            this.colPassengerName.Width = 150;
            // 
            // colPassengerPhone
            // 
            this.colPassengerPhone.Caption = "Телефон Пассажира";
            this.colPassengerPhone.FieldName = "PassengerPhone";
            this.colPassengerPhone.Name = "colPassengerPhone";
            this.colPassengerPhone.Visible = true;
            this.colPassengerPhone.VisibleIndex = 8;
            this.colPassengerPhone.Width = 120;
            // 
            // frmIncomeReport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(884, 561);
            this.Controls.Add(this.layoutControl1);
            this.Name = "frmIncomeReport";
            this.Text = "Отчет о Доходах";
            this.Load += new System.EventHandler(this.frmIncomeReport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.lueRouteFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlReport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.saleViewModelBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewReport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemRouteFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemExport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraGrid.GridControl gridControlReport;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewReport;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemGrid;
        private DevExpress.XtraEditors.SimpleButton btnApplyFilter;
        private DevExpress.XtraEditors.LookUpEdit lueRouteFilter;
        private DevExpress.XtraEditors.DateEdit dateToFilter;
        private DevExpress.XtraEditors.DateEdit dateFromFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateFrom;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateTo;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemRouteFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemApplyFilter;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private System.Windows.Forms.BindingSource saleViewModelBindingSource;
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
        private DevExpress.XtraEditors.SimpleButton btnExport;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemExport;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraGrid.Columns.GridColumn colPassengerName;
        private DevExpress.XtraGrid.Columns.GridColumn colPassengerPhone;
    }
} 