namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    partial class POSLogin
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
            // Dispose timer and cancellation token source
            if (_qrCheckTimer != null)
            {
                _qrCheckTimer.Stop();
                _qrCheckTimer.Dispose();
            }
            if (_ctsQr != null)
            {
                _ctsQr.Cancel();
                _ctsQr.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(POSLogin));
            this.usernameEntryPanel = new DevExpress.XtraEditors.PanelControl();
            this.btnContinueUsername = new DevExpress.XtraEditors.SimpleButton();
            this.txtUsernameInput = new DevExpress.XtraEditors.TextEdit();
            this.lblUsernamePrompt = new DevExpress.XtraEditors.LabelControl();
            this.lblWelcome = new DevExpress.XtraEditors.LabelControl();
            this.passwordLoginPanel = new DevExpress.XtraEditors.PanelControl();
            this.lblSignInOptions = new DevExpress.XtraEditors.LabelControl();
            this.lblSignInTo = new DevExpress.XtraEditors.LabelControl();
            this.lblTitle = new DevExpress.XtraEditors.LabelControl();
            this.lblError = new DevExpress.XtraEditors.LabelControl();
            this.btnSubmitLogin = new DevExpress.XtraEditors.SimpleButton();
            this.txtPassword = new DevExpress.XtraEditors.TextEdit();
            this.lblUsername = new DevExpress.XtraEditors.LabelControl();
            this.pictureUser = new DevExpress.XtraEditors.PictureEdit();
            this.qrLoginPanel = new DevExpress.XtraEditors.PanelControl();
            this.btnSwitchToPassword = new DevExpress.XtraEditors.SimpleButton();
            this.btnRefreshQR = new DevExpress.XtraEditors.SimpleButton();
            this.lblQRInfo = new DevExpress.XtraEditors.LabelControl();
            this.pictureBoxQR = new DevExpress.XtraEditors.PictureEdit();
            this.btnBack = new DevExpress.XtraEditors.SimpleButton();
            this.progressPanel = new DevExpress.XtraWaitForm.ProgressPanel();
            ((System.ComponentModel.ISupportInitialize)(this.usernameEntryPanel)).BeginInit();
            this.usernameEntryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtUsernameInput.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.passwordLoginPanel)).BeginInit();
            this.passwordLoginPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureUser.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.qrLoginPanel)).BeginInit();
            this.qrLoginPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQR.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // usernameEntryPanel
            // 
            this.usernameEntryPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.usernameEntryPanel.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.usernameEntryPanel.Appearance.Options.UseBackColor = true;
            this.usernameEntryPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.usernameEntryPanel.Controls.Add(this.btnContinueUsername);
            this.usernameEntryPanel.Controls.Add(this.txtUsernameInput);
            this.usernameEntryPanel.Controls.Add(this.lblUsernamePrompt);
            this.usernameEntryPanel.Controls.Add(this.lblWelcome);
            this.usernameEntryPanel.Location = new System.Drawing.Point(441, 292);
            this.usernameEntryPanel.Name = "usernameEntryPanel";
            this.usernameEntryPanel.Size = new System.Drawing.Size(400, 200);
            this.usernameEntryPanel.TabIndex = 4;
            // 
            // btnContinueUsername
            // 
            this.btnContinueUsername.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnContinueUsername.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnContinueUsername.Appearance.ForeColor = System.Drawing.Color.White;
            this.btnContinueUsername.Appearance.Options.UseBackColor = true;
            this.btnContinueUsername.Appearance.Options.UseFont = true;
            this.btnContinueUsername.Appearance.Options.UseForeColor = true;
            this.btnContinueUsername.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.btnContinueUsername.Location = new System.Drawing.Point(125, 140);
            this.btnContinueUsername.Name = "btnContinueUsername";
            this.btnContinueUsername.Size = new System.Drawing.Size(150, 30);
            this.btnContinueUsername.TabIndex = 3;
            this.btnContinueUsername.Text = "Продолжить";
            this.btnContinueUsername.Click += new System.EventHandler(this.btnContinueUsername_Click);
            // 
            // txtUsernameInput
            // 
            this.txtUsernameInput.Location = new System.Drawing.Point(50, 90);
            this.txtUsernameInput.Name = "txtUsernameInput";
            this.txtUsernameInput.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtUsernameInput.Properties.Appearance.Options.UseFont = true;
            this.txtUsernameInput.Properties.NullValuePrompt = "Имя пользователя";
            this.txtUsernameInput.Size = new System.Drawing.Size(300, 28);
            this.txtUsernameInput.TabIndex = 2;
            this.txtUsernameInput.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtUsernameInput_KeyPress);
            // 
            // lblUsernamePrompt
            // 
            this.lblUsernamePrompt.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblUsernamePrompt.Appearance.ForeColor = System.Drawing.Color.LightGray;
            this.lblUsernamePrompt.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.lblUsernamePrompt.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblUsernamePrompt.Location = new System.Drawing.Point(0, 55);
            this.lblUsernamePrompt.Name = "lblUsernamePrompt";
            this.lblUsernamePrompt.Size = new System.Drawing.Size(400, 20);
            this.lblUsernamePrompt.TabIndex = 1;
            this.lblUsernamePrompt.Text = "Введите имя пользователя для входа";
            // 
            // lblWelcome
            // 
            this.lblWelcome.Appearance.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F);
            this.lblWelcome.Appearance.ForeColor = System.Drawing.Color.White;
            this.lblWelcome.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.lblWelcome.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblWelcome.Location = new System.Drawing.Point(0, 20);
            this.lblWelcome.Name = "lblWelcome";
            this.lblWelcome.Size = new System.Drawing.Size(400, 30);
            this.lblWelcome.TabIndex = 0;
            this.lblWelcome.Text = "Добро пожаловать";
            // 
            // passwordLoginPanel
            // 
            this.passwordLoginPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.passwordLoginPanel.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.passwordLoginPanel.Appearance.Options.UseBackColor = true;
            this.passwordLoginPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.passwordLoginPanel.Controls.Add(this.lblSignInOptions);
            this.passwordLoginPanel.Controls.Add(this.lblSignInTo);
            this.passwordLoginPanel.Controls.Add(this.lblTitle);
            this.passwordLoginPanel.Controls.Add(this.lblError);
            this.passwordLoginPanel.Controls.Add(this.btnSubmitLogin);
            this.passwordLoginPanel.Controls.Add(this.txtPassword);
            this.passwordLoginPanel.Controls.Add(this.lblUsername);
            this.passwordLoginPanel.Controls.Add(this.pictureUser);
            this.passwordLoginPanel.Location = new System.Drawing.Point(312, 234);
            this.passwordLoginPanel.Name = "passwordLoginPanel";
            this.passwordLoginPanel.Size = new System.Drawing.Size(660, 300);
            this.passwordLoginPanel.TabIndex = 0;
            this.passwordLoginPanel.Visible = false;
            // 
            // lblSignInOptions
            // 
            this.lblSignInOptions.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblSignInOptions.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.lblSignInOptions.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblSignInOptions.Location = new System.Drawing.Point(230, 255);
            this.lblSignInOptions.Name = "lblSignInOptions";
            this.lblSignInOptions.Size = new System.Drawing.Size(158, 17);
            this.lblSignInOptions.TabIndex = 9;
            this.lblSignInOptions.Text = "Вход с помощью QR-кода";
            this.lblSignInOptions.Click += new System.EventHandler(this.btnSwitchUser_Click);
            // 
            // lblSignInTo
            // 
            this.lblSignInTo.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblSignInTo.Appearance.ForeColor = System.Drawing.Color.LightGray;
            this.lblSignInTo.Location = new System.Drawing.Point(230, 230);
            this.lblSignInTo.Name = "lblSignInTo";
            this.lblSignInTo.Size = new System.Drawing.Size(76, 17);
            this.lblSignInTo.TabIndex = 8;
            this.lblSignInTo.Text = "Войти в: POS";
            // 
            // lblTitle
            // 
            this.lblTitle.Appearance.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F);
            this.lblTitle.Appearance.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(230, 55);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(194, 30);
            this.lblTitle.TabIndex = 7;
            this.lblTitle.Text = "Введите ваш пароль";
            // 
            // lblError
            // 
            this.lblError.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblError.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.lblError.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.lblError.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblError.Location = new System.Drawing.Point(230, 185);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(390, 40);
            this.lblError.TabIndex = 5;
            this.lblError.Text = "Error Message Here";
            this.lblError.Visible = false;
            // 
            // btnSubmitLogin
            // 
            this.btnSubmitLogin.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnSubmitLogin.Appearance.Font = new System.Drawing.Font("Segoe UI Symbol", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSubmitLogin.Appearance.ForeColor = System.Drawing.Color.White;
            this.btnSubmitLogin.Appearance.Options.UseBackColor = true;
            this.btnSubmitLogin.Appearance.Options.UseFont = true;
            this.btnSubmitLogin.Appearance.Options.UseForeColor = true;
            this.btnSubmitLogin.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.btnSubmitLogin.Location = new System.Drawing.Point(580, 140);
            this.btnSubmitLogin.Name = "btnSubmitLogin";
            this.btnSubmitLogin.Size = new System.Drawing.Size(40, 28);
            this.btnSubmitLogin.TabIndex = 2;
            this.btnSubmitLogin.Text = "→";
            this.btnSubmitLogin.Click += new System.EventHandler(this.btnSubmitLogin_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(230, 140);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Properties.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtPassword.Properties.Appearance.Options.UseFont = true;
            this.txtPassword.Properties.NullValuePrompt = "Пароль";
            this.txtPassword.Properties.PasswordChar = '●';
            this.txtPassword.Properties.UseSystemPasswordChar = true;
            this.txtPassword.Size = new System.Drawing.Size(344, 28);
            this.txtPassword.TabIndex = 1;
            this.txtPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtPassword_KeyPress);
            // 
            // lblUsername
            // 
            this.lblUsername.Appearance.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUsername.Appearance.ForeColor = System.Drawing.Color.White;
            this.lblUsername.Location = new System.Drawing.Point(230, 100);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(85, 25);
            this.lblUsername.TabIndex = 3;
            this.lblUsername.Text = "Username";
            // 
            // pictureUser
            // 
            this.pictureUser.EditValue = ((object)(resources.GetObject("pictureUser.EditValue")));
            this.pictureUser.Location = new System.Drawing.Point(40, 80);
            this.pictureUser.Name = "pictureUser";
            this.pictureUser.Properties.AllowFocused = false;
            this.pictureUser.Properties.Appearance.BackColor = System.Drawing.Color.Gray;
            this.pictureUser.Properties.Appearance.Options.UseBackColor = true;
            this.pictureUser.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.pictureUser.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureUser.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            this.pictureUser.Size = new System.Drawing.Size(150, 150);
            this.pictureUser.TabIndex = 7;
            // 
            // qrLoginPanel
            // 
            this.qrLoginPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.qrLoginPanel.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.qrLoginPanel.Appearance.Options.UseBackColor = true;
            this.qrLoginPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.qrLoginPanel.Controls.Add(this.btnSwitchToPassword);
            this.qrLoginPanel.Controls.Add(this.btnRefreshQR);
            this.qrLoginPanel.Controls.Add(this.lblQRInfo);
            this.qrLoginPanel.Controls.Add(this.pictureBoxQR);
            this.qrLoginPanel.Location = new System.Drawing.Point(440, 164);
            this.qrLoginPanel.Name = "qrLoginPanel";
            this.qrLoginPanel.Size = new System.Drawing.Size(400, 440);
            this.qrLoginPanel.TabIndex = 1;
            this.qrLoginPanel.Visible = false;
            // 
            // btnSwitchToPassword
            // 
            this.btnSwitchToPassword.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnSwitchToPassword.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.btnSwitchToPassword.Appearance.Options.UseFont = true;
            this.btnSwitchToPassword.Appearance.Options.UseForeColor = true;
            this.btnSwitchToPassword.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.btnSwitchToPassword.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSwitchToPassword.Location = new System.Drawing.Point(100, 390);
            this.btnSwitchToPassword.Name = "btnSwitchToPassword";
            this.btnSwitchToPassword.Size = new System.Drawing.Size(200, 30);
            this.btnSwitchToPassword.TabIndex = 10;
            this.btnSwitchToPassword.Text = "Войти с помощью пароля";
            this.btnSwitchToPassword.Click += new System.EventHandler(this.btnSwitchToPassword_Click);
            // 
            // btnRefreshQR
            // 
            this.btnRefreshQR.Appearance.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.btnRefreshQR.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(255)))));
            this.btnRefreshQR.Appearance.Options.UseFont = true;
            this.btnRefreshQR.Appearance.Options.UseForeColor = true;
            this.btnRefreshQR.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.btnRefreshQR.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefreshQR.Location = new System.Drawing.Point(100, 355);
            this.btnRefreshQR.Name = "btnRefreshQR";
            this.btnRefreshQR.Size = new System.Drawing.Size(200, 30);
            this.btnRefreshQR.TabIndex = 9;
            this.btnRefreshQR.Text = "Обновить QR-код";
            this.btnRefreshQR.Click += new System.EventHandler(this.btnRefreshQR_Click);
            // 
            // lblQRInfo
            // 
            this.lblQRInfo.Appearance.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblQRInfo.Appearance.ForeColor = System.Drawing.Color.White;
            this.lblQRInfo.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.lblQRInfo.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.lblQRInfo.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.lblQRInfo.Location = new System.Drawing.Point(20, 10);
            this.lblQRInfo.Name = "lblQRInfo";
            this.lblQRInfo.Size = new System.Drawing.Size(360, 45);
            this.lblQRInfo.TabIndex = 8;
            this.lblQRInfo.Text = "Отсканируйте QR-код с помощью мобильного приложения для входа.";
            // 
            // pictureBoxQR
            // 
            this.pictureBoxQR.Location = new System.Drawing.Point(75, 65);
            this.pictureBoxQR.Name = "pictureBoxQR";
            this.pictureBoxQR.Properties.AllowFocused = false;
            this.pictureBoxQR.Properties.Appearance.BackColor = System.Drawing.Color.White;
            this.pictureBoxQR.Properties.Appearance.Options.UseBackColor = true;
            this.pictureBoxQR.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.pictureBoxQR.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureBoxQR.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze;
            this.pictureBoxQR.Size = new System.Drawing.Size(250, 250);
            this.pictureBoxQR.TabIndex = 7;
            // 
            // btnBack
            // 
            this.btnBack.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.btnBack.Appearance.Font = new System.Drawing.Font("Segoe UI Symbol", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.Appearance.ForeColor = System.Drawing.Color.White;
            this.btnBack.Appearance.Options.UseBackColor = true;
            this.btnBack.Appearance.Options.UseFont = true;
            this.btnBack.Appearance.Options.UseForeColor = true;
            this.btnBack.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.btnBack.Location = new System.Drawing.Point(30, 30);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(50, 50);
            this.btnBack.TabIndex = 3;
            this.btnBack.Text = "←";
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // progressPanel
            // 
            this.progressPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.progressPanel.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.progressPanel.Appearance.Options.UseBackColor = true;
            this.progressPanel.Caption = "Пожалуйста, подождите";
            this.progressPanel.Description = "Выполняется вход...";
            this.progressPanel.ImageHorzOffset = 8;
            this.progressPanel.Location = new System.Drawing.Point(508, 225);
            this.progressPanel.Name = "progressPanel";
            this.progressPanel.Size = new System.Drawing.Size(258, 324);
            this.progressPanel.TabIndex = 2;
            this.progressPanel.Text = "progressPanel1";
            this.progressPanel.Visible = false;
            // 
            // POSLogin
            // 
            this.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(64)))), ((int)(((byte)(111)))));
            this.Appearance.Options.UseBackColor = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1281, 768);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.progressPanel);
            this.Controls.Add(this.qrLoginPanel);
            this.Controls.Add(this.passwordLoginPanel);
            this.Controls.Add(this.usernameEntryPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.LookAndFeel.SkinName = "Metropolis Dark";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Name = "POSLogin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "POSLogin";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.POSLogin_Load);
            ((System.ComponentModel.ISupportInitialize)(this.usernameEntryPanel)).EndInit();
            this.usernameEntryPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.txtUsernameInput.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.passwordLoginPanel)).EndInit();
            this.passwordLoginPanel.ResumeLayout(false);
            this.passwordLoginPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassword.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureUser.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.qrLoginPanel)).EndInit();
            this.qrLoginPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxQR.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        // --- Control Declarations --- Must match instantiation above
        private DevExpress.XtraEditors.PanelControl usernameEntryPanel;
        private DevExpress.XtraEditors.LabelControl lblWelcome;
        private DevExpress.XtraEditors.LabelControl lblUsernamePrompt;
        private DevExpress.XtraEditors.TextEdit txtUsernameInput;
        private DevExpress.XtraEditors.SimpleButton btnContinueUsername;
        private DevExpress.XtraEditors.PanelControl passwordLoginPanel;
        private DevExpress.XtraEditors.PictureEdit pictureUser;
        private DevExpress.XtraEditors.LabelControl lblUsername;
        private DevExpress.XtraEditors.TextEdit txtPassword;
        private DevExpress.XtraEditors.SimpleButton btnSubmitLogin;
        private DevExpress.XtraEditors.LabelControl lblError;
        private DevExpress.XtraEditors.PanelControl qrLoginPanel;
        private DevExpress.XtraEditors.PictureEdit pictureBoxQR;
        private DevExpress.XtraEditors.LabelControl lblQRInfo;
        private DevExpress.XtraEditors.SimpleButton btnRefreshQR;
        private DevExpress.XtraEditors.SimpleButton btnSwitchToPassword;
        private DevExpress.XtraEditors.LabelControl lblTitle;
        private DevExpress.XtraEditors.SimpleButton btnBack;
        private DevExpress.XtraEditors.LabelControl lblSignInOptions;
        private DevExpress.XtraEditors.LabelControl lblSignInTo;
        private DevExpress.XtraWaitForm.ProgressPanel progressPanel;
    }
}