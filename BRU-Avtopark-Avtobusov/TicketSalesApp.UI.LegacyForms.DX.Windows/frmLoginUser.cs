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
    /// �û�����
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

            MessageBox.Show("����ɹ�!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        //ɾ��
        private void tsBtnDel_Click(object sender, EventArgs e)
        {
            

        }


        //�õ�����ǰѡ����
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