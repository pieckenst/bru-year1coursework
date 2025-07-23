using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Serilog;
using System.IO;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    /// <summary>
    /// Login Form
    /// </summary>
    public partial class frmLogin : DevExpress.XtraEditors.XtraForm
    {
        private readonly HttpClient _httpClient;
        private readonly Timer _qrCheckTimer;
        private string? _deviceId;
        private const string BaseUrl = "http://localhost:5000";
        private bool _isAuthenticated = false;

        // Properties
        private string Login { get; set; } = string.Empty;
        private string Password { get; set; } = string.Empty;
        private string ErrorMessage { get; set; } = string.Empty;
        private bool IsLoading
        {
            get => progressPanel.Visible;
            set
            {
                progressPanel.Visible = value;
                panelUsername.Enabled = !value;
                panelPassword.Enabled = !value;
                panelQRCode.Enabled = !value;
            }
        }

        public frmLogin()
        {
            InitializeComponent();

            // Initialize HTTP client
            _httpClient = ApiClientService.Instance.CreateClient();

            // Initialize QR check timer
            _qrCheckTimer = new Timer(2000); // Check every 2 seconds
            _qrCheckTimer.Elapsed += (s, e) => CheckQRLoginStatus();

            // Set up event handlers
            txtUsername.EditValueChanged += (s, e) => Login = txtUsername.Text;
            txtPassword.EditValueChanged += (s, e) => Password = txtPassword.Text;

            // Start with username entry panel visible
            panelUsername.Visible = true;
            panelQRCode.Visible = false;
            panelPassword.Visible = false;
            progressPanel.Visible = false;

            // Load saved login info if available
            GetLoginUnitXML();
        }

        #region Navigation Methods

        private void SwitchToPasswordLogin()
        {
            panelQRCode.Visible = false;
            panelPassword.Visible = true;
            _qrCheckTimer.Stop();
            lblLoginTitle.Text = $"Вход для {Login}";
        }

        private void SwitchToQRLogin()
        {
            panelPassword.Visible = false;
            panelQRCode.Visible = true;
            Task.Run(async () => await RefreshQRCodeAsync());
        }

        #endregion

        #region Authentication Methods

        private async Task ContinueWithUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                this.Invoke(new Action(() => {
                    MessageBox.Show("Пожалуйста, введите имя пользователя", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
                return;
            }

            try
            {
                this.Invoke(new Action(() => IsLoading = true));

                // Generate QR code for the username
                var response = await _httpClient.GetAsync($"{BaseUrl}/api/auth/qr/direct/generate?username={Login}&deviceType=desktop");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRCodeResponse>();
                    if (result != null)
                    {
                        // Convert base64 to image
                        var bytes = Convert.FromBase64String(result.qrCode);
                        using var ms = new MemoryStream(bytes);
                        var qrImage = Image.FromStream(ms);
                        
                        this.Invoke(new Action(() => {
                            // Resize QR code to fit in the picture box while maintaining aspect ratio
                            pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
                            pictureBoxQR.Image = qrImage;
                            _deviceId = result.deviceId;
                            lblQRUsername.Text = $"Вход для пользователя: {Login}";

                            // Switch to QR view
                            panelUsername.Visible = false;
                            panelQRCode.Visible = true;

                            // Start polling for login status
                            _qrCheckTimer.Start();
                        }));
                    }
                }
                else
                {
                    this.Invoke(new Action(() => {
                        ErrorMessage = "Не удалось сгенерировать QR-код";
                        MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => {
                    ErrorMessage = "Ошибка при генерации QR-кода";
                    MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
                Log.Error(ex, "Error generating QR code");
            }
            finally
            {
                this.Invoke(new Action(() => IsLoading = false));
            }
        }

        private async Task RefreshQRCodeAsync()
        {
            _qrCheckTimer.Stop();
            await ContinueWithUsernameAsync();
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                this.Invoke(new Action(() => {
                    lblError.Text = "Пожалуйста, введите логин и пароль";
                    lblError.Visible = true;
                }));
                return;
            }

            try
            {
                this.Invoke(new Action(() => {
                    IsLoading = true;
                    lblError.Visible = false;
                }));

                var loginData = new { Login, Password };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result?.Token != null)
                    {
                        // Store token in the ApiClientService
                        ApiClientService.Instance.AuthToken = result.Token;
                        
                        _isAuthenticated = true;
                        Log.Information("User successfully authenticated");
                        
                        // Save login info
                        this.Invoke(new Action(() => {
                            WriteLoginUnitXML();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }));
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    this.Invoke(new Action(() => {
                        lblError.Text = $"Ошибка авторизации: {error}";
                        lblError.Visible = true;
                    }));
                    Log.Warning("Authentication failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => {
                    lblError.Text = "Произошла ошибка при авторизации";
                    lblError.Visible = true;
                }));
                Log.Error(ex, "Authentication error");
            }
            finally
            {
                this.Invoke(new Action(() => IsLoading = false));
            }
        }

        private async Task CheckQRLoginStatus()
        {
            if (string.IsNullOrEmpty(_deviceId)) return;

            try
            {
                var response = await _httpClient.GetAsync($"/api/auth/qr/direct/check?deviceId={_deviceId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRLoginStatusResponse>();
                    if (result?.success == true && result?.token != null)
                    {
                        _qrCheckTimer.Stop();
                        
                        // Store token in the ApiClientService
                        ApiClientService.Instance.AuthToken = result.token;
                        
                        // Handle successful login
                        _isAuthenticated = true;
                        
                        // Need to invoke on UI thread since timer callback is on a different thread
                        this.Invoke(new Action(() => {
                            WriteLoginUnitXML();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking QR login status");
            }
        }

        #endregion

        #region Event Handlers

        private void login()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Пожалуйста, введите пароль!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Use async login method
            Task.Run(async () => await LoginAsync());
        }

        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.ToString() == "\r")
            {
                login();
            }
        }

        private void btnContinueWithUsername_Click(object sender, EventArgs e)
        {
            Login = txtUsernameInput.Text;
            txtUsername.Text= Login;
            Task.Run(async () => await ContinueWithUsernameAsync());
        }

        private void btnSwitchToPassword_Click(object sender, EventArgs e)
        {
            SwitchToPasswordLogin();
        }

        private void btnSwitchToQR_Click(object sender, EventArgs e)
        {
            SwitchToQRLogin();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Task.Run(async () => await LoginAsync());
        }

        private void btnRefreshQR_Click(object sender, EventArgs e)
        {
            Task.Run(async () => await RefreshQRCodeAsync());
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            // Form load initialization if needed
        }

        #endregion

        #region XML Configuration

        /// <summary>
        /// Load saved login information
        /// </summary>
        private void GetLoginUnitXML()
        {
            try
            {
                string fileName = Path.Combine(Application.StartupPath, "HistoryLogin.xml");
                if (File.Exists(fileName))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(fileName);
                    XmlNode rootNode = xmlDoc.DocumentElement;
                    if (rootNode != null && rootNode.HasChildNodes)
                    {
                        XmlNode userNode = rootNode.FirstChild;
                        if (userNode != null && userNode.Attributes != null)
                        {
                            string savedUsername = userNode.Attributes["UserName"]?.Value ?? string.Empty;
                            if (!string.IsNullOrEmpty(savedUsername))
                            {
                                txtUsername.Text = savedUsername;
                                Login = savedUsername;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading login history");
            }
        }

        /// <summary>
        /// Save login information to XML
        /// </summary>
        private void WriteLoginUnitXML()
        {
            try
            {
                string fileName = Path.Combine(Application.StartupPath, "HistoryLogin.xml");
                
                // Create file if it doesn't exist
                if (!File.Exists(fileName))
                {
                    XmlDocument newDoc = new XmlDocument();
                    XmlDeclaration xmlDeclaration = newDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                    newDoc.AppendChild(xmlDeclaration);
                    XmlElement root = newDoc.CreateElement("LoginHistory");
                    newDoc.AppendChild(root);
                    newDoc.Save(fileName);
                }

                XmlDocument myXmlDocument = new XmlDocument();
                myXmlDocument.Load(fileName);
                XmlNode rootNode = myXmlDocument.DocumentElement;
                
                // Clear existing nodes
                if (rootNode != null)
                {
                    rootNode.RemoveAll();
                    
                    // Add current user
                    XmlElement userElement = myXmlDocument.CreateElement("User");
                    XmlAttribute userNameAttr = myXmlDocument.CreateAttribute("UserName");
                    userNameAttr.Value = Login;
                    userElement.Attributes.Append(userNameAttr);
                    rootNode.AppendChild(userElement);
                    
                    myXmlDocument.Save(fileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving login history");
            }
        }

        #endregion

        #region Response Classes

        private class QRCodeResponse
        {
            public string qrCode { get; set; } = string.Empty;
            public string deviceId { get; set; } = string.Empty;
            public string? rawData { get; set; }
        }

        private class QRLoginStatusResponse
        {
            public bool success { get; set; }
            public string? token { get; set; }
        }

        private class AuthResponse
        {
            public string? Token { get; set; }
        }

        #endregion
    }
}