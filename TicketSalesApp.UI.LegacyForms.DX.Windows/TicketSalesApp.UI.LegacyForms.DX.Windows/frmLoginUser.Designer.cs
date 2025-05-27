namespace TicketSalesApp.UI.LegacyForms.DX.Windows

{
    partial class frmLoginUser
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLoginUser));
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsbtnSave = new System.Windows.Forms.ToolStripButton();
            tsBtnDel = new System.Windows.Forms.ToolStripButton();
            tsBtnExit = new System.Windows.Forms.ToolStripButton();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            txtUserID = new System.Windows.Forms.TextBox();
            txtUserName = new System.Windows.Forms.TextBox();
            txtPassword = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            txtEmail = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            gridLoginPerson = new DevExpress.XtraGrid.GridControl();
            gridVProGroup = new DevExpress.XtraGrid.Views.Grid.GridView();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridLoginPerson).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridVProGroup).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsbtnSave, tsBtnDel, tsBtnExit });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(550, 25);
            toolStrip1.TabIndex = 67;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbtnSave
            // 
            tsbtnSave.Image = (System.Drawing.Image)resources.GetObject("tsbtnSave.Image");
            tsbtnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbtnSave.Name = "tsbtnSave";
            tsbtnSave.Size = new System.Drawing.Size(85, 22);
            tsbtnSave.Text = "Сохранить";
            tsbtnSave.Click += tsbtnSave_Click;
            // 
            // tsBtnDel
            // 
            tsBtnDel.Image = (System.Drawing.Image)resources.GetObject("tsBtnDel.Image");
            tsBtnDel.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBtnDel.Name = "tsBtnDel";
            tsBtnDel.Size = new System.Drawing.Size(71, 22);
            tsBtnDel.Text = "Стирать";
            tsBtnDel.Click += tsBtnDel_Click;
            // 
            // tsBtnExit
            // 
            tsBtnExit.Image = (System.Drawing.Image)resources.GetObject("tsBtnExit.Image");
            tsBtnExit.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsBtnExit.Name = "tsBtnExit";
            tsBtnExit.Size = new System.Drawing.Size(61, 22);
            tsBtnExit.Text = "Выход";
            tsBtnExit.Click += tsBtnExit_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(246, 60);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(100, 13);
            label2.TabIndex = 70;
            label2.Text = "Имя пользователя";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(31, 60);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(161, 13);
            label1.TabIndex = 71;
            label1.Text = "Учетная запись пользователя";
            // 
            // txtUserID
            // 
            txtUserID.Location = new System.Drawing.Point(31, 76);
            txtUserID.Name = "txtUserID";
            txtUserID.Size = new System.Drawing.Size(145, 21);
            txtUserID.TabIndex = 72;
            // 
            // txtUserName
            // 
            txtUserName.Location = new System.Drawing.Point(354, 57);
            txtUserName.Name = "txtUserName";
            txtUserName.Size = new System.Drawing.Size(166, 21);
            txtUserName.TabIndex = 73;
            // 
            // txtPassword
            // 
            txtPassword.Location = new System.Drawing.Point(31, 117);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new System.Drawing.Size(137, 21);
            txtPassword.TabIndex = 75;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(31, 101);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(118, 13);
            label3.TabIndex = 74;
            label3.Text = "Пароль пользователя";
            // 
            // txtEmail
            // 
            txtEmail.Location = new System.Drawing.Point(354, 94);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new System.Drawing.Size(166, 21);
            txtEmail.TabIndex = 77;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(246, 101);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(108, 13);
            label4.TabIndex = 76;
            label4.Text = "Электронная Почта";
            // 
            // gridLoginPerson
            // 
            gridLoginPerson.Location = new System.Drawing.Point(25, 146);
            gridLoginPerson.MainView = gridVProGroup;
            gridLoginPerson.Name = "gridLoginPerson";
            gridLoginPerson.Size = new System.Drawing.Size(513, 267);
            gridLoginPerson.TabIndex = 78;
            gridLoginPerson.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridVProGroup });
            // 
            // gridVProGroup
            // 
            gridVProGroup.DetailHeight = 379;
            gridVProGroup.GridControl = gridLoginPerson;
            gridVProGroup.Name = "gridVProGroup";
            gridVProGroup.OptionsCustomization.AllowFilter = false;
            gridVProGroup.OptionsCustomization.AllowGroup = false;
            gridVProGroup.OptionsCustomization.AllowSort = false;
            gridVProGroup.OptionsView.ShowGroupPanel = false;
            // 
            // frmLoginUser
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(550, 421);
            Controls.Add(gridLoginPerson);
            Controls.Add(txtEmail);
            Controls.Add(label4);
            Controls.Add(txtPassword);
            Controls.Add(label3);
            Controls.Add(txtUserName);
            Controls.Add(txtUserID);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(toolStrip1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            LookAndFeel.SkinMaskColor = System.Drawing.Color.DimGray;
            LookAndFeel.SkinName = "Whiteprint";
            LookAndFeel.UseDefaultLookAndFeel = false;
            Name = "frmLoginUser";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Управление пользователями для входа в систему";
            Load += frmLoginUser_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridLoginPerson).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridVProGroup).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsbtnSave;
        private System.Windows.Forms.ToolStripButton tsBtnDel;
        private System.Windows.Forms.ToolStripButton tsBtnExit;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserID;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label label4;
        private DevExpress.XtraGrid.GridControl gridLoginPerson;
        private DevExpress.XtraGrid.Views.Grid.GridView gridVProGroup;
    }
}