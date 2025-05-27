using System.ComponentModel;
using DevExpress.XtraCharts;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmSalesStatistics
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
            
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.btnApplyFilter = new DevExpress.XtraEditors.SimpleButton();
            this.dateToFilter = new DevExpress.XtraEditors.DateEdit();
            this.dateFromFilter = new DevExpress.XtraEditors.DateEdit();
            this.chartControlSalesByRoute = new DevExpress.XtraCharts.ChartControl();
            this.lblTotalIncomeValue = new DevExpress.XtraEditors.LabelControl();
            this.lblTotalSalesValue = new DevExpress.XtraEditors.LabelControl();
            this.Root = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemDateFrom = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemDateTo = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemApplyFilter = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemFilter = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlGroupTotals = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemTotalSales = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItemTotalIncome = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItemTotals = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlGroupChart = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItemChart = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartControlSalesByRoute)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupTotals)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemTotalSales)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemTotalIncome)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemTotals)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemChart)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.btnApplyFilter);
            this.layoutControl1.Controls.Add(this.dateToFilter);
            this.layoutControl1.Controls.Add(this.dateFromFilter);
            this.layoutControl1.Controls.Add(this.chartControlSalesByRoute);
            this.layoutControl1.Controls.Add(this.lblTotalIncomeValue);
            this.layoutControl1.Controls.Add(this.lblTotalSalesValue);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(1010, 245, 650, 400);
            this.layoutControl1.Root = this.Root;
            this.layoutControl1.Size = new System.Drawing.Size(784, 561);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // btnApplyFilter
            // 
           
            this.btnApplyFilter.Location = new System.Drawing.Point(397, 12);
            this.btnApplyFilter.Name = "btnApplyFilter";
            this.btnApplyFilter.Size = new System.Drawing.Size(96, 22);
            this.btnApplyFilter.StyleController = this.layoutControl1;
            this.btnApplyFilter.TabIndex = 2;
            this.btnApplyFilter.Text = "Применить";
            this.btnApplyFilter.Click += new System.EventHandler(this.btnApplyFilter_Click);
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
            this.dateToFilter.Size = new System.Drawing.Size(145, 20);
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
            // chartControlSalesByRoute
            // 
            this.chartControlSalesByRoute.Location = new System.Drawing.Point(24, 131);
            this.chartControlSalesByRoute.Name = "chartControlSalesByRoute";
            this.chartControlSalesByRoute.SeriesSerializable = new DevExpress.XtraCharts.Series[0];
            this.chartControlSalesByRoute.Size = new System.Drawing.Size(736, 406);
            this.chartControlSalesByRoute.TabIndex = 4;
            // 
            // lblTotalIncomeValue
            // 
            this.lblTotalIncomeValue.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.lblTotalIncomeValue.Appearance.Options.UseFont = true;
            this.lblTotalIncomeValue.Location = new System.Drawing.Point(174, 68);
            this.lblTotalIncomeValue.Name = "lblTotalIncomeValue";
            this.lblTotalIncomeValue.Size = new System.Drawing.Size(9, 19);
            this.lblTotalIncomeValue.StyleController = this.layoutControl1;
            this.lblTotalIncomeValue.TabIndex = 3;
            this.lblTotalIncomeValue.Text = "-";
            // 
            // lblTotalSalesValue
            // 
            this.lblTotalSalesValue.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.lblTotalSalesValue.Appearance.Options.UseFont = true;
            this.lblTotalSalesValue.Location = new System.Drawing.Point(174, 45);
            this.lblTotalSalesValue.Name = "lblTotalSalesValue";
            this.lblTotalSalesValue.Size = new System.Drawing.Size(9, 19);
            this.lblTotalSalesValue.StyleController = this.layoutControl1;
            this.lblTotalSalesValue.TabIndex = 2;
            this.lblTotalSalesValue.Text = "-";
            // 
            // Root
            // 
            this.Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.Root.GroupBordersVisible = false;
            this.Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemDateFrom,
            this.layoutControlItemDateTo,
            this.layoutControlItemApplyFilter,
            this.emptySpaceItemFilter,
            this.layoutControlGroupTotals,
            this.layoutControlGroupChart});
            this.Root.Name = "Root";
            this.Root.Size = new System.Drawing.Size(784, 561);
            this.Root.TextVisible = false;
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
            this.layoutControlItemDateTo.MaxSize = new System.Drawing.Size(205, 26);
            this.layoutControlItemDateTo.MinSize = new System.Drawing.Size(110, 26);
            this.layoutControlItemDateTo.Name = "layoutControlItemDateTo";
            this.layoutControlItemDateTo.Size = new System.Drawing.Size(205, 26);
            this.layoutControlItemDateTo.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemDateTo.Text = "Дата По:";
            this.layoutControlItemDateTo.TextSize = new System.Drawing.Size(44, 13);
            // 
            // layoutControlItemApplyFilter
            // 
            this.layoutControlItemApplyFilter.Control = this.btnApplyFilter;
            this.layoutControlItemApplyFilter.Location = new System.Drawing.Point(385, 0);
            this.layoutControlItemApplyFilter.MaxSize = new System.Drawing.Size(100, 26);
            this.layoutControlItemApplyFilter.MinSize = new System.Drawing.Size(100, 26);
            this.layoutControlItemApplyFilter.Name = "layoutControlItemApplyFilter";
            this.layoutControlItemApplyFilter.Size = new System.Drawing.Size(100, 26);
            this.layoutControlItemApplyFilter.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItemApplyFilter.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemApplyFilter.TextVisible = false;
            // 
            // emptySpaceItemFilter
            // 
            this.emptySpaceItemFilter.AllowHotTrack = false;
            this.emptySpaceItemFilter.Location = new System.Drawing.Point(485, 0);
            this.emptySpaceItemFilter.Name = "emptySpaceItemFilter";
            this.emptySpaceItemFilter.Size = new System.Drawing.Size(279, 26);
            this.emptySpaceItemFilter.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlGroupTotals
            // 
            this.layoutControlGroupTotals.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemTotalSales,
            this.layoutControlItemTotalIncome,
            this.emptySpaceItemTotals});
            this.layoutControlGroupTotals.Location = new System.Drawing.Point(0, 26);
            this.layoutControlGroupTotals.Name = "layoutControlGroupTotals";
            this.layoutControlGroupTotals.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlGroupTotals.Size = new System.Drawing.Size(764, 60);
            this.layoutControlGroupTotals.Text = "Общие Итоги";
            this.layoutControlGroupTotals.TextVisible = false;
            // 
            // layoutControlItemTotalSales
            // 
            this.layoutControlItemTotalSales.AppearanceItemCaption.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.layoutControlItemTotalSales.AppearanceItemCaption.Options.UseFont = true;
            this.layoutControlItemTotalSales.Control = this.lblTotalSalesValue;
            this.layoutControlItemTotalSales.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItemTotalSales.Name = "layoutControlItemTotalSales";
            this.layoutControlItemTotalSales.Size = new System.Drawing.Size(163, 23);
            this.layoutControlItemTotalSales.Text = "Всего Продаж:";
            this.layoutControlItemTotalSales.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItemTotalSales.TextSize = new System.Drawing.Size(92, 16);
            this.layoutControlItemTotalSales.TextToControlDistance = 58;
            // 
            // layoutControlItemTotalIncome
            // 
            this.layoutControlItemTotalIncome.AppearanceItemCaption.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.layoutControlItemTotalIncome.AppearanceItemCaption.Options.UseFont = true;
            this.layoutControlItemTotalIncome.Control = this.lblTotalIncomeValue;
            this.layoutControlItemTotalIncome.Location = new System.Drawing.Point(0, 23);
            this.layoutControlItemTotalIncome.Name = "layoutControlItemTotalIncome";
            this.layoutControlItemTotalIncome.Size = new System.Drawing.Size(163, 23);
            this.layoutControlItemTotalIncome.Text = "Общий Доход:";
            this.layoutControlItemTotalIncome.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItemTotalIncome.TextSize = new System.Drawing.Size(85, 16);
            this.layoutControlItemTotalIncome.TextToControlDistance = 65;
            // 
            // emptySpaceItemTotals
            // 
            this.emptySpaceItemTotals.AllowHotTrack = false;
            this.emptySpaceItemTotals.Location = new System.Drawing.Point(163, 0);
            this.emptySpaceItemTotals.Name = "emptySpaceItemTotals";
            this.emptySpaceItemTotals.Size = new System.Drawing.Size(585, 46);
            this.emptySpaceItemTotals.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlGroupChart
            // 
            this.layoutControlGroupChart.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItemChart});
            this.layoutControlGroupChart.Location = new System.Drawing.Point(0, 86);
            this.layoutControlGroupChart.Name = "layoutControlGroupChart";
            this.layoutControlGroupChart.Size = new System.Drawing.Size(764, 455);
            this.layoutControlGroupChart.Text = "Продажи по Маршрутам";
            // 
            // layoutControlItemChart
            // 
            this.layoutControlItemChart.Control = this.chartControlSalesByRoute;
            this.layoutControlItemChart.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItemChart.Name = "layoutControlItemChart";
            this.layoutControlItemChart.Size = new System.Drawing.Size(740, 410);
            this.layoutControlItemChart.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItemChart.TextVisible = false;
            // 
            // frmSalesStatistics
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.layoutControl1);
            this.Name = "frmSalesStatistics";
            this.Text = "Статистика Продаж";
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateToFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dateFromFilter.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartControlSalesByRoute)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Root)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemDateTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemApplyFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemFilter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupTotals)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemTotalSales)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemTotalIncome)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItemTotals)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroupChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItemChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraEditors.SimpleButton btnApplyFilter;
        private DevExpress.XtraEditors.DateEdit dateToFilter;
        private DevExpress.XtraEditors.DateEdit dateFromFilter;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateFrom;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemDateTo;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemApplyFilter;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemFilter;
        private DevExpress.XtraCharts.ChartControl chartControlSalesByRoute;
        private DevExpress.XtraEditors.LabelControl lblTotalIncomeValue;
        private DevExpress.XtraEditors.LabelControl lblTotalSalesValue;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroupTotals;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemTotalSales;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemTotalIncome;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItemTotals;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroupChart;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItemChart;
    }
} 