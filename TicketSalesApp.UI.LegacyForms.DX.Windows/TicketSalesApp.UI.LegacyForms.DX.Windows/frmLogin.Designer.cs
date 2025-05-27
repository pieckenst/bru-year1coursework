namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class frmLogin
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
            panelUsername = new DevExpress.XtraEditors.PanelControl();
            btnContinueWithUsername = new DevExpress.XtraEditors.SimpleButton();
            txtUsernameInput = new DevExpress.XtraEditors.TextEdit();
            lblUsernameSubtitle = new DevExpress.XtraEditors.LabelControl();
            lblWelcome = new DevExpress.XtraEditors.LabelControl();
            txtUsername = new DevExpress.XtraEditors.TextEdit();
            panelQRCode = new DevExpress.XtraEditors.PanelControl();
            lblQRUsername = new DevExpress.XtraEditors.LabelControl();
            btnSwitchToPassword = new DevExpress.XtraEditors.SimpleButton();
            btnRefreshQR = new DevExpress.XtraEditors.SimpleButton();
            pictureBoxQR = new System.Windows.Forms.PictureBox();
            lblQRSubtitle = new DevExpress.XtraEditors.LabelControl();
            lblQRTitle = new DevExpress.XtraEditors.LabelControl();
            panelPassword = new DevExpress.XtraEditors.PanelControl();
            btnSwitchToQR = new DevExpress.XtraEditors.SimpleButton();
            btnLogin = new DevExpress.XtraEditors.SimpleButton();
            lblError = new DevExpress.XtraEditors.LabelControl();
            txtPassword = new DevExpress.XtraEditors.TextEdit();
            lblLoginTitle = new DevExpress.XtraEditors.LabelControl();
            progressPanel = new DevExpress.XtraEditors.PanelControl();
            lblLoading = new DevExpress.XtraEditors.LabelControl();
            progressIndicator = new DevExpress.XtraWaitForm.ProgressPanel();
            ((System.ComponentModel.ISupportInitialize)panelUsername).BeginInit();
            panelUsername.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtUsernameInput.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtUsername.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelQRCode).BeginInit();
            panelQRCode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxQR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelPassword).BeginInit();
            panelPassword.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtPassword.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)progressPanel).BeginInit();
            progressPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panelUsername
            // 
            panelUsername.Appearance.BackColor = System.Drawing.Color.White;
            panelUsername.Appearance.Options.UseBackColor = true;
            panelUsername.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelUsername.Controls.Add(btnContinueWithUsername);
            panelUsername.Controls.Add(txtUsernameInput);
            panelUsername.Controls.Add(lblUsernameSubtitle);
            panelUsername.Controls.Add(lblWelcome);
            panelUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            panelUsername.Location = new System.Drawing.Point(0, 0);
            panelUsername.Name = "panelUsername";
            panelUsername.Padding = new System.Windows.Forms.Padding(32);
            panelUsername.Size = new System.Drawing.Size(392, 600);
            panelUsername.TabIndex = 0;
            // 
            // btnContinueWithUsername
            // 
            btnContinueWithUsername.Location = new System.Drawing.Point(32, 175);
            btnContinueWithUsername.Name = "btnContinueWithUsername";
            btnContinueWithUsername.Size = new System.Drawing.Size(328, 43);
            btnContinueWithUsername.TabIndex = 0;
            btnContinueWithUsername.Text = "Продолжить";
            btnContinueWithUsername.Click += btnContinueWithUsername_Click;
            // 
            // txtUsernameInput
            // 
            txtUsernameInput.Location = new System.Drawing.Point(75, 131);
            txtUsernameInput.Name = "txtUsernameInput";
            txtUsernameInput.Properties.AutoHeight = false;
            txtUsernameInput.Size = new System.Drawing.Size(250, 36);
            txtUsernameInput.TabIndex = 4;
            // 
            // lblUsernameSubtitle
            // 
            lblUsernameSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblUsernameSubtitle.Appearance.Options.UseFont = true;
            lblUsernameSubtitle.Appearance.Options.UseTextOptions = true;
            lblUsernameSubtitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblUsernameSubtitle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            lblUsernameSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblUsernameSubtitle.Dock = System.Windows.Forms.DockStyle.Top;
            lblUsernameSubtitle.Location = new System.Drawing.Point(32, 82);
            lblUsernameSubtitle.Name = "lblUsernameSubtitle";
            lblUsernameSubtitle.Padding = new System.Windows.Forms.Padding(0, 8, 0, 24);
            lblUsernameSubtitle.Size = new System.Drawing.Size(328, 54);
            lblUsernameSubtitle.TabIndex = 1;
            lblUsernameSubtitle.Text = "Введите имя пользователя для продолжения";
            // 
            // lblWelcome
            // 
            lblWelcome.Appearance.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
            lblWelcome.Appearance.Options.UseFont = true;
            lblWelcome.Appearance.Options.UseTextOptions = true;
            lblWelcome.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblWelcome.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblWelcome.Dock = System.Windows.Forms.DockStyle.Top;
            lblWelcome.Location = new System.Drawing.Point(32, 32);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            lblWelcome.Size = new System.Drawing.Size(328, 50);
            lblWelcome.TabIndex = 0;
            lblWelcome.Text = "Добро пожаловать";
            // 
            // txtUsername
            // 
            txtUsername.Dock = System.Windows.Forms.DockStyle.Top;
            txtUsername.Location = new System.Drawing.Point(32, 102);
            txtUsername.Name = "txtUsername";
            txtUsername.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            txtUsername.Properties.Appearance.Options.UseFont = true;
            txtUsername.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            txtUsername.Properties.NullText = "Имя пользователя";
            txtUsername.Size = new System.Drawing.Size(328, 24);
            txtUsername.TabIndex = 2;
            // 
            // panelQRCode
            // 
            panelQRCode.Appearance.BackColor = System.Drawing.Color.White;
            panelQRCode.Appearance.Options.UseBackColor = true;
            panelQRCode.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelQRCode.Controls.Add(lblQRUsername);
            panelQRCode.Controls.Add(btnSwitchToPassword);
            panelQRCode.Controls.Add(btnRefreshQR);
            panelQRCode.Controls.Add(pictureBoxQR);
            panelQRCode.Controls.Add(lblQRSubtitle);
            panelQRCode.Controls.Add(lblQRTitle);
            panelQRCode.Dock = System.Windows.Forms.DockStyle.Fill;
            panelQRCode.Location = new System.Drawing.Point(0, 0);
            panelQRCode.Name = "panelQRCode";
            panelQRCode.Padding = new System.Windows.Forms.Padding(32);
            panelQRCode.Size = new System.Drawing.Size(392, 600);
            panelQRCode.TabIndex = 1;
            panelQRCode.Visible = false;
            // 
            // lblQRUsername
            // 
            lblQRUsername.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblQRUsername.Appearance.Options.UseFont = true;
            lblQRUsername.Appearance.Options.UseTextOptions = true;
            lblQRUsername.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblQRUsername.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblQRUsername.Location = new System.Drawing.Point(32, 504);
            lblQRUsername.Name = "lblQRUsername";
            lblQRUsername.Size = new System.Drawing.Size(336, 20);
            lblQRUsername.TabIndex = 5;
            // 
            // btnSwitchToPassword
            // 
            btnSwitchToPassword.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            btnSwitchToPassword.Appearance.Options.UseFont = true;
            btnSwitchToPassword.Location = new System.Drawing.Point(32, 456);
            btnSwitchToPassword.Name = "btnSwitchToPassword";
            btnSwitchToPassword.Size = new System.Drawing.Size(336, 40);
            btnSwitchToPassword.TabIndex = 4;
            btnSwitchToPassword.Text = "Или войти с использованием пароля";
            btnSwitchToPassword.Click += btnSwitchToPassword_Click;
            // 
            // btnRefreshQR
            // 
            btnRefreshQR.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            btnRefreshQR.Appearance.Options.UseFont = true;
            btnRefreshQR.Location = new System.Drawing.Point(32, 408);
            btnRefreshQR.Name = "btnRefreshQR";
            btnRefreshQR.Size = new System.Drawing.Size(336, 40);
            btnRefreshQR.TabIndex = 3;
            btnRefreshQR.Text = "Обновить QR-код";
            btnRefreshQR.Click += btnRefreshQR_Click;
            // 
            // pictureBoxQR
            // 
            pictureBoxQR.BackColor = System.Drawing.Color.White;
            pictureBoxQR.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pictureBoxQR.Location = new System.Drawing.Point(75, 142);
            pictureBoxQR.Name = "pictureBoxQR";
            pictureBoxQR.Size = new System.Drawing.Size(250, 250);
            pictureBoxQR.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            pictureBoxQR.TabIndex = 2;
            pictureBoxQR.TabStop = false;
            // 
            // lblQRSubtitle
            // 
            lblQRSubtitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblQRSubtitle.Appearance.Options.UseFont = true;
            lblQRSubtitle.Appearance.Options.UseTextOptions = true;
            lblQRSubtitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblQRSubtitle.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            lblQRSubtitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblQRSubtitle.Dock = System.Windows.Forms.DockStyle.Top;
            lblQRSubtitle.Location = new System.Drawing.Point(32, 82);
            lblQRSubtitle.Name = "lblQRSubtitle";
            lblQRSubtitle.Padding = new System.Windows.Forms.Padding(0, 8, 0, 24);
            lblQRSubtitle.Size = new System.Drawing.Size(328, 60);
            lblQRSubtitle.TabIndex = 1;
            lblQRSubtitle.Text = "Отсканируйте QR-код с помощью мобильного приложения";
            // 
            // lblQRTitle
            // 
            lblQRTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
            lblQRTitle.Appearance.Options.UseFont = true;
            lblQRTitle.Appearance.Options.UseTextOptions = true;
            lblQRTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblQRTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblQRTitle.Dock = System.Windows.Forms.DockStyle.Top;
            lblQRTitle.Location = new System.Drawing.Point(32, 32);
            lblQRTitle.Name = "lblQRTitle";
            lblQRTitle.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            lblQRTitle.Size = new System.Drawing.Size(328, 50);
            lblQRTitle.TabIndex = 0;
            lblQRTitle.Text = "QR-код для входа";
            // 
            // panelPassword
            // 
            panelPassword.Appearance.BackColor = System.Drawing.Color.White;
            panelPassword.Appearance.Options.UseBackColor = true;
            panelPassword.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelPassword.Controls.Add(btnSwitchToQR);
            panelPassword.Controls.Add(btnLogin);
            panelPassword.Controls.Add(lblError);
            panelPassword.Controls.Add(txtPassword);
            panelPassword.Controls.Add(txtUsername);
            panelPassword.Controls.Add(lblLoginTitle);
            panelPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            panelPassword.Location = new System.Drawing.Point(0, 0);
            panelPassword.Name = "panelPassword";
            panelPassword.Padding = new System.Windows.Forms.Padding(32);
            panelPassword.Size = new System.Drawing.Size(392, 600);
            panelPassword.TabIndex = 2;
            panelPassword.Visible = false;
            // 
            // btnSwitchToQR
            // 
            btnSwitchToQR.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            btnSwitchToQR.Appearance.Options.UseFont = true;
            btnSwitchToQR.Location = new System.Drawing.Point(32, 296);
            btnSwitchToQR.Name = "btnSwitchToQR";
            btnSwitchToQR.Size = new System.Drawing.Size(336, 40);
            btnSwitchToQR.TabIndex = 5;
            btnSwitchToQR.Text = "Вернуться к QR-коду";
            btnSwitchToQR.Click += btnSwitchToQR_Click;
            // 
            // btnLogin
            // 
            btnLogin.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            btnLogin.Appearance.Options.UseFont = true;
            btnLogin.Location = new System.Drawing.Point(32, 248);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new System.Drawing.Size(336, 40);
            btnLogin.TabIndex = 4;
            btnLogin.Text = "Войти";
            btnLogin.Click += btnLogin_Click;
            // 
            // lblError
            // 
            lblError.Appearance.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblError.Appearance.ForeColor = System.Drawing.Color.Red;
            lblError.Appearance.Options.UseFont = true;
            lblError.Appearance.Options.UseForeColor = true;
            lblError.Appearance.Options.UseTextOptions = true;
            lblError.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            lblError.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblError.Location = new System.Drawing.Point(32, 200);
            lblError.Name = "lblError";
            lblError.Size = new System.Drawing.Size(336, 40);
            lblError.TabIndex = 3;
            lblError.Visible = false;
            // 
            // txtPassword
            // 
            txtPassword.Location = new System.Drawing.Point(32, 152);
            txtPassword.Name = "txtPassword";
            txtPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            txtPassword.Properties.Appearance.Options.UseFont = true;
            txtPassword.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            txtPassword.Properties.NullText = "Пароль";
            txtPassword.Properties.PasswordChar = '●';
            txtPassword.Size = new System.Drawing.Size(336, 24);
            txtPassword.TabIndex = 2;
            txtPassword.KeyPress += txtPassword_KeyPress;
            // 
            // lblLoginTitle
            // 
            lblLoginTitle.Appearance.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold);
            lblLoginTitle.Appearance.Options.UseFont = true;
            lblLoginTitle.Appearance.Options.UseTextOptions = true;
            lblLoginTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblLoginTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblLoginTitle.Dock = System.Windows.Forms.DockStyle.Top;
            lblLoginTitle.Location = new System.Drawing.Point(32, 32);
            lblLoginTitle.Name = "lblLoginTitle";
            lblLoginTitle.Padding = new System.Windows.Forms.Padding(0, 20, 0, 20);
            lblLoginTitle.Size = new System.Drawing.Size(328, 70);
            lblLoginTitle.TabIndex = 0;
            lblLoginTitle.Text = "Вход в систему";
            // 
            // progressPanel
            // 
            progressPanel.Appearance.BackColor = System.Drawing.Color.FromArgb(128, 0, 0, 0);
            progressPanel.Appearance.Options.UseBackColor = true;
            progressPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            progressPanel.Controls.Add(lblLoading);
            progressPanel.Controls.Add(progressIndicator);
            progressPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            progressPanel.Location = new System.Drawing.Point(0, 0);
            progressPanel.Name = "progressPanel";
            progressPanel.Size = new System.Drawing.Size(392, 600);
            progressPanel.TabIndex = 3;
            progressPanel.Visible = false;
            // 
            // lblLoading
            // 
            lblLoading.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            lblLoading.Appearance.ForeColor = System.Drawing.Color.White;
            lblLoading.Appearance.Options.UseFont = true;
            lblLoading.Appearance.Options.UseForeColor = true;
            lblLoading.Appearance.Options.UseTextOptions = true;
            lblLoading.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblLoading.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblLoading.Location = new System.Drawing.Point(100, 350);
            lblLoading.Name = "lblLoading";
            lblLoading.Size = new System.Drawing.Size(200, 20);
            lblLoading.TabIndex = 1;
            lblLoading.Text = "Подождите...";
            // 
            // progressIndicator
            // 
            progressIndicator.Appearance.BackColor = System.Drawing.Color.Transparent;
            progressIndicator.Appearance.ForeColor = System.Drawing.Color.White;
            progressIndicator.Appearance.Options.UseBackColor = true;
            progressIndicator.Appearance.Options.UseForeColor = true;
            progressIndicator.Location = new System.Drawing.Point(100, 248);
            progressIndicator.Name = "progressIndicator";
            progressIndicator.Size = new System.Drawing.Size(250, 100);
            progressIndicator.TabIndex = 0;
            progressIndicator.Text = "progressPanel1";
            // 
            // frmLogin
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(392, 600);
            Controls.Add(progressPanel);
            Controls.Add(panelUsername);
            Controls.Add(panelQRCode);
            Controls.Add(panelPassword);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            LookAndFeel.SkinName = "DevExpress Style";
            LookAndFeel.UseDefaultLookAndFeel = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmLogin";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Авторизация";
            Load += frmLogin_Load;
            ((System.ComponentModel.ISupportInitialize)panelUsername).EndInit();
            panelUsername.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)txtUsernameInput.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtUsername.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelQRCode).EndInit();
            panelQRCode.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxQR).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelPassword).EndInit();
            panelPassword.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)txtPassword.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)progressPanel).EndInit();
            progressPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelUsername;
        private DevExpress.XtraEditors.PanelControl panelQRCode;
        private DevExpress.XtraEditors.PanelControl panelPassword;
        private DevExpress.XtraEditors.PanelControl progressPanel;
        
        private DevExpress.XtraEditors.LabelControl lblWelcome;
        private DevExpress.XtraEditors.TextEdit txtUsername;
        
        private DevExpress.XtraEditors.LabelControl lblQRTitle;
        private DevExpress.XtraEditors.LabelControl lblQRSubtitle;
        private System.Windows.Forms.PictureBox pictureBoxQR;
        private DevExpress.XtraEditors.SimpleButton btnRefreshQR;
        private DevExpress.XtraEditors.SimpleButton btnSwitchToPassword;
        private DevExpress.XtraEditors.LabelControl lblQRUsername;
        
        private DevExpress.XtraEditors.LabelControl lblLoginTitle;
        private DevExpress.XtraEditors.TextEdit txtPassword;
        private DevExpress.XtraEditors.LabelControl lblError;
        private DevExpress.XtraEditors.SimpleButton btnLogin;
        private DevExpress.XtraEditors.SimpleButton btnSwitchToQR;
        
        private DevExpress.XtraWaitForm.ProgressPanel progressIndicator;
        private DevExpress.XtraEditors.LabelControl lblLoading;
        private DevExpress.XtraEditors.TextEdit txtUsernameInput;
        private DevExpress.XtraEditors.SimpleButton btnContinueWithUsername;
        private DevExpress.XtraEditors.LabelControl lblUsernameSubtitle;
    }
}