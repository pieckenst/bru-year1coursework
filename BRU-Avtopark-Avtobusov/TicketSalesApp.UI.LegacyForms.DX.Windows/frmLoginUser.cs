using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    /// <summary>
    /// 用户管理
    /// </summary>
    public partial class frmLoginUser : DevExpress.XtraEditors.XtraForm
    {
        DataTable dtl = new DataTable();

        public frmLoginUser()
        {
            InitializeComponent();
        }

        private void tsbtnSave_Click(object sender, EventArgs e)
        {



            LoadData();

            MessageBox.Show("保存成功!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void frmLoginUser_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {



        }

        private void tsBtnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //删除
        private void tsBtnDel_Click(object sender, EventArgs e)
        {
            

        }


        //得到网格当前选择行
        public int RowIndex(object dataSource)
        {
            int result = -1;
            if (dataSource != null)
            {
                result = this.BindingContext[dataSource].Position;
            }
            return result;
        }
    }
}