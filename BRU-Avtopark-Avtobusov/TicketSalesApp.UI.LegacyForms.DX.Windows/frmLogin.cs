using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Serilog;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Security.Cryptography;
using System.Threading;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    /// <summary>
    /// Login Form
    /// </summary>
    public partial class frmLogin : DevExpress.XtraEditors.XtraForm
    {
        private readonly HttpClient _httpClient;
        private readonly Timer _qrCheckTimer;
        private string _deviceId;
        private const string BaseUrl = "http://localhost:5000";
        private bool _isAuthenticated = false;
        private CancellationTokenSource _ctsQr;

        // Properties
        private string Login { get; set; }
        private string Password { get; set; }
        private string ErrorMessage { get; set; }
        private bool IsLoading
        {
            get { return progressPanel.Visible; }
            set
            {
                if (progressPanel != null) progressPanel.Visible = value;
                if (panelUsername != null) panelUsername.Enabled = !value;
                if (panelPassword != null) panelPassword.Enabled = !value;
                if (panelQRCode != null) panelQRCode.Enabled = !value;
            }
        }

        public frmLogin()
        {
            InitializeComponent();

            // Initialize strings
            Login = string.Empty;
            Password = string.Empty;
            ErrorMessage = string.Empty;
            _deviceId = null;

            // Initialize HTTP client via service
            _httpClient = ApiClientService.Instance.CreateClient();

            // Initialize QR check timer
            _qrCheckTimer = new Timer(2000); // Check every 2 seconds
            _qrCheckTimer.Elapsed += new ElapsedEventHandler(CheckQRLoginStatus);

            // Set up event handlers - Use delegate
            txtUsername.EditValueChanged += delegate(object s, EventArgs e) { Login = txtUsername.Text; };
            txtPassword.EditValueChanged += delegate(object s, EventArgs e) { Password = txtPassword.Text; };

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
            lblLoginTitle.Text = string.Format("Вход для {0}", Login);
        }

        private void SwitchToQRLogin()
        {
            panelPassword.Visible = false;
            panelQRCode.Visible = true;
            Task.Factory.StartNew(() => RefreshQRCodeAsync());
        }

        #endregion

        #region Authentication Methods

        private async Task ContinueWithUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        MessageBox.Show("Пожалуйста, введите имя пользователя", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                } else {
                    MessageBox.Show("Пожалуйста, введите имя пользователя", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            HttpClient client = null;
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(delegate { IsLoading = true; })); } else { IsLoading = true; }

                client = _httpClient;
                var apiUrl = string.Format("{0}/api/auth/qr/direct/generate?username={1}&deviceType=desktop", BaseUrl, Login);
                Log.Debug("Requesting QR code from {ApiUrl}", apiUrl);
                var response = await client.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<QRCodeResponse>(jsonString);

                    if (result != null && !string.IsNullOrEmpty(result.qrCode))
                    {
                        var bytes = Convert.FromBase64String(result.qrCode);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var qrImage = Image.FromStream(ms);
                        
                            if (this.InvokeRequired) {
                                this.Invoke(new Action(delegate { 
                                    pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
                                    pictureBoxQR.Image = qrImage;
                                    _deviceId = result.deviceId;
                                    lblQRUsername.Text = string.Format("Вход для пользователя: {0}", Login);

                                    panelUsername.Visible = false;
                                    panelQRCode.Visible = true;

                                    _qrCheckTimer.Start();
                                 }));
                            } else {
                                    pictureBoxQR.SizeMode = PictureBoxSizeMode.Zoom;
                                    pictureBoxQR.Image = qrImage;
                                    _deviceId = result.deviceId;
                                    lblQRUsername.Text = string.Format("Вход для пользователя: {0}", Login);
                                    panelUsername.Visible = false;
                                    panelQRCode.Visible = true;
                                    _qrCheckTimer.Start();
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("QR Code generation returned success but result or qrCode was null/empty.");
                        if (this.InvokeRequired) {
                            this.Invoke(new Action(delegate {
                                ErrorMessage = "Не удалось получить данные QR-кода.";
                                MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        } else {
                                ErrorMessage = "Не удалось получить данные QR-кода.";
                                MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to generate QR code. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                    if (this.InvokeRequired) {
                        this.Invoke(new Action(delegate {
                             ErrorMessage = string.Format("Не удалось сгенерировать QR-код: {0}", response.ReasonPhrase);
                             MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    } else {
                             ErrorMessage = string.Format("Не удалось сгенерировать QR-код: {0}", response.ReasonPhrase);
                             MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        ErrorMessage = string.Format("Ошибка при генерации QR-кода: {0}", ex.Message);
                        MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                } else {
                        ErrorMessage = string.Format("Ошибка при генерации QR-кода: {0}", ex.Message);
                        MessageBox.Show(ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Log.Error(ex, "Error generating QR code");
            }
            finally
            {
                if (this.InvokeRequired) { this.Invoke(new Action(delegate { IsLoading = false; })); } else { IsLoading = false; }
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
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        lblError.Text = "Пожалуйста, введите логин и пароль";
                        lblError.Visible = true;
                    }));
                } else {
                        lblError.Text = "Пожалуйста, введите логин и пароль";
                        lblError.Visible = true;
                }
                return;
            }

            HttpClient client = null;
            try
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        IsLoading = true;
                        lblError.Visible = false;
                    }));
                } else {
                        IsLoading = true;
                        lblError.Visible = false;
                }

                var loginData = new { Login, Password };
                string jsonPayload = JsonConvert.SerializeObject(loginData);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var apiUrl = string.Format("{0}/api/auth/login", BaseUrl);
                client = _httpClient;
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AuthResponse>(jsonString);
                    
                    if (result != null && !string.IsNullOrEmpty(result.Token)) 
                    {
                        ApiClientService.Instance.AuthToken = result.Token;
                        
                        _isAuthenticated = true;
                        Log.Information("User successfully authenticated");
                        
                        if (this.InvokeRequired) {
                            this.Invoke(new Action(delegate {
                                WriteLoginUnitXML();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }));
                        } else {
                                WriteLoginUnitXML();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                        }
                    }
                    else
                    {
                        Log.Warning("Authentication succeeded but token was null or empty.");
                        if (this.InvokeRequired) {
                            this.Invoke(new Action(delegate {
                                lblError.Text = "Ошибка авторизации: Не удалось получить токен.";
                                lblError.Visible = true;
                            }));
                        } else {
                                lblError.Text = "Ошибка авторизации: Не удалось получить токен.";
                                lblError.Visible = true;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if (this.InvokeRequired) {
                        this.Invoke(new Action(delegate {
                            lblError.Text = string.Format("Ошибка авторизации: {0}", error);
                            lblError.Visible = true;
                        }));
                    } else {
                            lblError.Text = string.Format("Ошибка авторизации: {0}", error);
                            lblError.Visible = true;
                    }
                    Log.Warning("Authentication failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        lblError.Text = string.Format("Произошла ошибка при авторизации: {0}", ex.Message);
                        lblError.Visible = true;
                    }));
                } else {
                         lblError.Text = string.Format("Произошла ошибка при авторизации: {0}", ex.Message);
                         lblError.Visible = true;
                }
                Log.Error(ex, "Authentication error");
            }
            finally
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate { IsLoading = false; }));
                } else {
                    IsLoading = false;
                }
            }
        }

        private async void CheckQRLoginStatus(object source, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(_deviceId)) return;

            HttpClient client = null;
            try
            {
                client = _httpClient;
                var apiUrl = string.Format("{0}/api/auth/qr/direct/check?deviceId={1}", BaseUrl, _deviceId);
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<QRLoginStatusResponse>(jsonString);

                    if (result != null && result.success && !string.IsNullOrEmpty(result.token))
                    {
                        _qrCheckTimer.Stop();
                        
                        ApiClientService.Instance.AuthToken = result.token;
                        
                        _isAuthenticated = true;
                        
                        this.Invoke(new Action(delegate {
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

        private void AuthenticateUser()
        {
            Task.Factory.StartNew(() => AuthenticateUserAsync());
        }

        private async Task AuthenticateUserAsync()
        {
            var username = txtUsername.Text;
            var password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        lblError.Text = "Пожалуйста, введите логин и пароль";
                        lblError.Visible = true;
                    }));
                } else {
                        lblError.Text = "Пожалуйста, введите логин и пароль";
                        lblError.Visible = true;
                }
                return;
            }

            HttpClient client = null;
            try
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        IsLoading = true;
                        lblError.Visible = false;
                    }));
                } else {
                        IsLoading = true;
                        lblError.Visible = false;
                }

                var loginData = new { Login = username, Password = password };
                string jsonPayload = JsonConvert.SerializeObject(loginData);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var apiUrl = string.Format("{0}/api/auth/login", BaseUrl);
                client = _httpClient;
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<AuthResponse>(jsonString);
                    
                    if (result != null && !string.IsNullOrEmpty(result.Token)) 
                    {
                        ApiClientService.Instance.AuthToken = result.Token;
                        
                        _isAuthenticated = true;
                        Log.Information("User successfully authenticated");
                        
                        if (this.InvokeRequired) {
                            this.Invoke(new Action(delegate {
                                WriteLoginUnitXML();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }));
                        } else {
                                WriteLoginUnitXML();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                        }
                    }
                    else
                    {
                        Log.Warning("Authentication succeeded but token was null or empty.");
                        if (this.InvokeRequired) {
                            this.Invoke(new Action(delegate {
                                lblError.Text = "Ошибка авторизации: Не удалось получить токен.";
                                lblError.Visible = true;
                            }));
                        } else {
                                lblError.Text = "Ошибка авторизации: Не удалось получить токен.";
                                lblError.Visible = true;
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if (this.InvokeRequired) {
                        this.Invoke(new Action(delegate {
                            lblError.Text = string.Format("Ошибка авторизации: {0}", error);
                            lblError.Visible = true;
                        }));
                    } else {
                            lblError.Text = string.Format("Ошибка авторизации: {0}", error);
                            lblError.Visible = true;
                    }
                    Log.Warning("Authentication failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        lblError.Text = string.Format("Произошла ошибка при авторизации: {0}", ex.Message);
                        lblError.Visible = true;
                    }));
                } else {
                         lblError.Text = string.Format("Произошла ошибка при авторизации: {0}", ex.Message);
                         lblError.Visible = true;
                }
                Log.Error(ex, "Authentication error");
            }
            finally
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate { IsLoading = false; }));
                } else {
                    IsLoading = false;
                }
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            AuthenticateUser();
        }

        private void btnQrLogin_Click(object sender, EventArgs e)
        {
            StartQrCodeLoginProcess();
        }

        private void btnSwitchToPassword_Click(object sender, EventArgs e)
        {
            SwitchToPasswordLogin();
        }

        private void btnSwitchToQR_Click(object sender, EventArgs e)
        {
            SwitchToQRLogin();
        }

        private void btnRefreshQR_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => RefreshQRCodeAsync());
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            // Form load initialization if needed
            Task.Factory.StartNew(() => ContinueWithUsernameAsync());
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
                            string savedUsername = (userNode.Attributes["UserName"] != null ? userNode.Attributes["UserName"].Value : null);
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
                
                if (rootNode != null)
                {
                    rootNode.RemoveAll();
                    
                    XmlElement userElement = myXmlDocument.CreateElement("User");
                    XmlAttribute userNameAttr = myXmlDocument.CreateAttribute("UserName");
                    userNameAttr.Value = Login != null ? Login : string.Empty;
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
            public string qrCode { get; set; }
            public string deviceId { get; set; }
            public string rawData { get; set; }
        }

        private class QRLoginStatusResponse
        {
            public bool success { get; set; }
            public string token { get; set; }
        }

        private class AuthResponse
        {
            public string Token { get; set; }
        }

        #endregion

        private void StartQrCodeLoginProcess()
        {
            Task.Factory.StartNew(() => StartQrCodeLoginProcessAsync());
        }

        private async Task StartQrCodeLoginProcessAsync()
        {
            Log.Information("QR code login process started.");
            _ctsQr = new CancellationTokenSource();
            try
            {
                await ContinueWithUsernameAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting QR code login process");
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        lblError.Text = string.Format("Ошибка при запуске процесса входа: {0}", ex.Message);
                        lblError.Visible = true;
                    }));
                } else {
                        lblError.Text = string.Format("Ошибка при запуске процесса входа: {0}", ex.Message);
                        lblError.Visible = true;
                }
            }
        }

        private async Task PollForQrLoginStatus(string sessionId, CancellationToken cancellationToken)
        {
            var client = _httpClient;
            var pollUrl = string.Format("{0}/api/auth/qr-status/{1}", BaseUrl, sessionId);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var response = await client.GetAsync(pollUrl, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<QRLoginStatusResponse>(jsonString);

                        if (result != null && result.success && !string.IsNullOrEmpty(result.token))
                        {
                            _qrCheckTimer.Stop();
                            
                            ApiClientService.Instance.AuthToken = result.token;
                            
                            _isAuthenticated = true;
                            
                            this.Invoke(new Action(delegate {
                                WriteLoginUnitXML();
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }));
                        }
                    }
                    else
                    {
                        Log.Warning("QR login status polling failed. Status: {StatusCode}", response.StatusCode);
                    }

                    Thread.Sleep(500);
                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error polling for QR login status");
            }
        }

        private async Task RegisterUserAsync()
        {
            var username = txtUsername.Text;
            var password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        MessageBox.Show("Имя пользователя и пароль не могут быть пустыми.", "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }));
                } else {
                        MessageBox.Show("Имя пользователя и пароль не могут быть пустыми.", "Ошибка регистрации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            HttpClient client = null;
            try
            {
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        IsLoading = true;
                        lblError.Visible = false;
                    }));
                } else {
                        IsLoading = true;
                        lblError.Visible = false;
                }

                client = _httpClient;
                var registerDto = new { Login = username, Password = password };
                string jsonPayload = JsonConvert.SerializeObject(registerDto);
                HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(string.Format("{0}/api/auth/register", BaseUrl), content);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("User {Username} registered successfully.", username);
                    if (this.InvokeRequired) {
                        this.Invoke(new Action(delegate {
                            MessageBox.Show("Пользователь успешно зарегистрирован.", "Регистрация успешна", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    } else {
                            MessageBox.Show("Пользователь успешно зарегистрирован.", "Регистрация успешна", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Warning("Registration failed for {Username}. Status: {StatusCode}, Reason: {Reason}, Content: {Content}", username, response.StatusCode, response.ReasonPhrase, errorContent);
                    if (this.InvokeRequired) {
                        this.Invoke(new Action(delegate {
                            lblError.Text = string.Format("Ошибка регистрации: {0}", errorContent);
                            lblError.Visible = true;
                        }));
                    } else {
                            lblError.Text = string.Format("Ошибка регистрации: {0}", errorContent);
                            lblError.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during registration for {Username}", username);
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate {
                        MessageBox.Show(string.Format("Произошла ошибка: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                } else {
                        MessageBox.Show(string.Format("Произошла ошибка: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (client != null) client.Dispose();
                if (this.InvokeRequired) {
                    this.Invoke(new Action(delegate { IsLoading = false; }));
                } else {
                    IsLoading = false;
                }
            }
        }

        private void SetLoading(bool isLoading)
        {
            if (isLoading)
            {
                progressPanel.Visible = true;
            }
            else
            {
                progressPanel.Visible = false;
            }
            panelUsername.Enabled = !isLoading;
            panelPassword.Enabled = !isLoading;
            panelQRCode.Enabled = !isLoading;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => RegisterUserAsync());
        }

        private void btnContinueWithUsername_Click(object sender, EventArgs e)
        {
            Login = txtUsernameInput.Text;
            Task.Factory.StartNew(() => ContinueWithUsernameAsync());
        }

        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                AuthenticateUser();
                e.Handled = true;
            }
        }
    }
}