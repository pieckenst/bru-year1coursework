using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using NLog;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Xml;
using DevExpress.XtraEditors;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class POSLogin : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly HttpClient _httpClient;
        private readonly Timer _qrCheckTimer;
        private string _deviceId;
        private const string BaseUrl = "http://localhost:5000"; // TODO: Move to config
        private bool _isAuthenticated = false;
        private CancellationTokenSource _ctsQr;

        // Properties
        private string CurrentUsername { get; set; } // Renamed from Login
        private string CurrentPassword // Renamed from Password
        {
            get { return txtPassword.Text; } 
            set { txtPassword.Text = value; }
        }
        private string ErrorMessage
        {
            get { return lblError.Text; }
            set
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(delegate {
                        lblError.Text = value;
                        lblError.Visible = !string.IsNullOrEmpty(value);
                    }));
                } else {
                    lblError.Text = value;
                    lblError.Visible = !string.IsNullOrEmpty(value);
                }
            }
        }
        private bool IsLoading
        {
            get { return progressPanel.Visible; }
            set
            {
                 if (this.InvokeRequired)
                {
                    this.Invoke(new Action(delegate { SetLoadingState(value); }));
                } else {
                    SetLoadingState(value);
                }
            }
        }

        public POSLogin()
        {
            InitializeComponent();

            // Initialize strings & states
            CurrentUsername = string.Empty;
            ErrorMessage = string.Empty;
            _deviceId = null;
            passwordLoginPanel.Visible = true;
            qrLoginPanel.Visible = false;
            usernameEntryPanel.Visible = true;
            IsLoading = false;

            // Initialize HTTP client via service
            _httpClient = ApiClientService.Instance.CreateClient();

            // Initialize QR check timer
            _qrCheckTimer = new Timer(2000); // Check every 2 seconds
            _qrCheckTimer.Elapsed += new ElapsedEventHandler(CheckQRLoginStatus);

            // Set default user picture (add a resource named default_user)
            // User commented out EditValue in designer, so skip setting image here
            // If you add the resource later, uncomment this section.
            /*
            try {
                 if (Resources.default_user != null) { // Check resource exists
                     pictureUser.Image = Resources.default_user;
                 } else {
                     Log.Warn("Default user image resource (Properties.Resources.default_user) is null.");
                 }
            } catch (Exception ex) {
                 string warnMsg = string.Format("Error accessing default user image resource: {0}", ex.ToString());
                 Log.Warn(warnMsg);
             }
            */

            // Ensure initial state AFTER InitializeComponent()
            passwordLoginPanel.Visible = false;
            qrLoginPanel.Visible = false;
            usernameEntryPanel.Visible = true;
            IsLoading = false;
            CenterPanels(); // Center initially
        }

        private void POSLogin_Load(object sender, EventArgs e)
        {
            // Load saved login info if available
            GetLoginUnitXML();
            txtUsernameInput.Text = CurrentUsername;
            txtUsernameInput.Focus();
        }

        private void SetLoadingState(bool isLoading)
        {
            progressPanel.Visible = isLoading;
            progressPanel.BringToFront(); // Ensure progress is on top
            
            // Enable/disable the *visible* panel
            if (usernameEntryPanel.Visible) {
                usernameEntryPanel.Enabled = !isLoading;
            }
            if (passwordLoginPanel.Visible) {
                 passwordLoginPanel.Enabled = !isLoading;
            }
            
            if (isLoading)
            {
                // No need to hide individual controls if the parent panel is disabled
                // Keep main panel controls hidden/disabled while loading
            }
            else
            {
                // No need to explicitly show controls here, as the parent panel
                // will become visible and enabled in ShowPasswordLoginView/ShowQRLoginView
                // Restore visibility based on which panel is active
            }
        }

        #region Navigation and UI Switching

        private void ShowPasswordLoginView()
        {
            usernameEntryPanel.Visible = false;
            qrLoginPanel.Visible = false;
            passwordLoginPanel.Visible = true;
            passwordLoginPanel.Enabled = true; // Ensure it's enabled
            if (_qrCheckTimer != null) _qrCheckTimer.Stop();
            if (_ctsQr != null) _ctsQr.Cancel();
            lblUsername.Text = string.IsNullOrEmpty(CurrentUsername) ? "Вход" : CurrentUsername;
            btnBack.Visible = false; // Hide back button on password view
            txtPassword.Focus();
        }

        private void ShowQRLoginView()
        {
            passwordLoginPanel.Visible = false;
            usernameEntryPanel.Visible = false;
            btnBack.Visible = true; // Show back button on QR view
            qrLoginPanel.Visible = true;
            qrLoginPanel.Enabled = true; // Ensure it's enabled
            IsLoading = true; // Show loading while QR generates
            // Use Task.Factory for .NET 4.0 compatibility
            Task.Factory.StartNew(() => GenerateAndDisplayQRCodeAsync());
        }
        
        private void SwitchUser()
        {
            passwordLoginPanel.Visible = false;
            qrLoginPanel.Visible = false;
            usernameEntryPanel.Visible = true;
            txtUsernameInput.Text = CurrentUsername;
            txtUsernameInput.Focus();
            txtUsernameInput.SelectAll();
            btnBack.Visible = false;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (qrLoginPanel.Visible)
            {
                IsLoading = false;
                ShowPasswordLoginView();
            }
        }

        #endregion

        #region Authentication Methods (Adapted from frmLogin)

        // Generates QR Code for the CurrentUsername
        private async Task GenerateAndDisplayQRCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUsername))
            {
                // Should not happen if called after GetLoginUnitXML, but handle defensively
                Log.Warn("GenerateAndDisplayQRCodeAsync called with no CurrentUsername.");
                ErrorMessage = "Имя пользователя не найдено для генерации QR.";
                ShowPasswordLoginView(); // Go back to password view
                IsLoading = false;
                return;
            }

            HttpClient client = null;
            try
            {
                client = _httpClient;
                var apiUrl = string.Format("{0}/api/auth/qr/direct/generate?username={1}&deviceType=desktop", BaseUrl, CurrentUsername);
                Log.Debug("Requesting QR code from {0}", apiUrl);
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
                        
                            this.Invoke(new Action(delegate { 
                                pictureBoxQR.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Squeeze; // Use Squeeze for potentially non-square QR
                                pictureBoxQR.Image = qrImage;
                                _deviceId = result.deviceId;
                                lblQRInfo.Text = string.Format("Отсканируйте для входа как: {0}", CurrentUsername);
                                _qrCheckTimer.Start();
                             }));
                        }
                    }
                    else
                    {
                        Log.Warn("QR Code generation returned success but result or qrCode was null/empty.");
                        ErrorMessage = "Не удалось получить данные QR-кода.";
                        ShowPasswordLoginView(); // Go back
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    string errorMsg = string.Format("Failed to generate QR code. Status: {0}, Content: {1}", response.StatusCode, errorContent);
                    Log.Error(errorMsg);
                    ErrorMessage = string.Format("Не удалось сгенерировать QR-код: {0}", response.ReasonPhrase);
                    ShowPasswordLoginView(); // Go back
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Format("Ошибка при генерации QR-кода: {0}", ex.Message);
                string errorMsg = string.Format("Error generating QR code for user {0}. Exception: {1}", CurrentUsername, ex.ToString());
                Log.Error(errorMsg);
                ShowPasswordLoginView(); // Go back
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshQRCodeAsync()
        {
             if (_qrCheckTimer != null) _qrCheckTimer.Stop();
            IsLoading = true; 
            await GenerateAndDisplayQRCodeAsync(); // Regenerate for the same user
        }

        // Password Login Logic
        private async Task LoginAsync()
        {
            string user = CurrentUsername;
            string pass = CurrentPassword;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ErrorMessage = "Пожалуйста, введите пароль"; // Username should be loaded
                return;
            }

            HttpClient client = null;
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty; // Clear previous errors

                var loginData = new { Login = user, Password = pass };
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
                        Log.Info("User '{0}' successfully authenticated via password.", user);
                        
                        this.Invoke(new Action(delegate {
                            WriteLoginUnitXML(); // Save successful username
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }));
                    }
                    else
                    {
                        string warnMsg = string.Format("Authentication succeeded for '{0}' but token was null or empty.", user);
                        Log.Warn(warnMsg);
                        ErrorMessage = "Ошибка авторизации: Не удалось получить токен.";
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    // Try to parse a more specific error message if backend returns JSON error
                    string displayError = "Неверный пароль или ошибка сервера.";
                    try {
                         var errorObj = JsonConvert.DeserializeObject<ErrorResponse>(error);
                         if (errorObj != null && !string.IsNullOrEmpty(errorObj.Message)) {
                             displayError = errorObj.Message;
                         }
                    } catch { /* Ignore if error is not JSON */ }
                    
                    ErrorMessage = string.Format("Ошибка авторизации: {0}", displayError);
                    string warnMsg = string.Format("Authentication failed for '{0}': {1} ({2}) - {3}", user, response.StatusCode, response.ReasonPhrase, error);
                    Log.Warn(warnMsg);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Format("Произошла ошибка при авторизации: {0}", ex.Message);
                string errorMsg = string.Format("Authentication error for user {0}. Exception: {1}", user, ex.ToString());
                Log.Error(errorMsg);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // QR Code Status Check Logic
        private async void CheckQRLoginStatus(object source, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(_deviceId) || !_qrCheckTimer.Enabled) return;

            // Temporarily stop timer to prevent reentry if check takes longer than interval
            _qrCheckTimer.Enabled = false; 

            HttpClient client = null;
            try
            {
                client = _httpClient;
                var apiUrl = string.Format("{0}/api/auth/qr/direct/check?deviceId={1}", BaseUrl, _deviceId);
                var response = await client.GetAsync(apiUrl); // Consider adding timeout/cancellation
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<QRLoginStatusResponse>(jsonString);

                    if (result != null && result.success && !string.IsNullOrEmpty(result.token))
                    {
                        // Stop timer permanently on success
                        // _qrCheckTimer.Stop(); // Already stopped via Enabled=false
                        
                        ApiClientService.Instance.AuthToken = result.token;
                        _isAuthenticated = true;
                        Log.Info("User '{0}' successfully authenticated via QR code.", CurrentUsername);
                        
                        this.Invoke(new Action(delegate {
                            WriteLoginUnitXML(); // Save successful username
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }));
                    }
                    else
                    {
                        // Re-enable timer if not successful
                        _qrCheckTimer.Enabled = true; 
                    }
                }
                else
                {
                    string warnMsg = string.Format("QR check failed for deviceId {0}. Status: {1}", _deviceId, response.StatusCode);
                    Log.Warn(warnMsg);
                    _qrCheckTimer.Enabled = true; // Re-enable on failure
                }
            }
            catch (TaskCanceledException) { 
                 Log.Debug("QR Check task cancelled");
                 _qrCheckTimer.Enabled = false; // Keep disabled if form is closing
            }
            catch (Exception ex)
            {
                
                string errorMsg = string.Format("Error checking QR login status for deviceId {0}. Exception: {1}", _deviceId, ex.ToString());
                Log.Error(errorMsg);
                _qrCheckTimer.Enabled = true; // Re-enable on error
            }
        }

        #endregion

        #region Event Handlers

        // Called by button click or Enter key
        private void AuthenticateUser() 
        {
            Task.Factory.StartNew(() => LoginAsync());
        }

        private void btnSubmitLogin_Click(object sender, EventArgs e)
        {
            AuthenticateUser();
        }

        private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                AuthenticateUser();
                e.Handled = true; // Prevent ding sound
            }
        }

        private void btnSwitchUser_Click(object sender, EventArgs e)
        {
            ShowQRLoginView();
        }

        private void btnSwitchToPassword_Click(object sender, EventArgs e)
        {
            ShowPasswordLoginView();
        }

        private void btnRefreshQR_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() => RefreshQRCodeAsync());
        }
        
        private void btnContinueUsername_Click(object sender, EventArgs e)
        {
            string enteredUsername = txtUsernameInput.Text;
            if (string.IsNullOrWhiteSpace(enteredUsername))
            {
                MessageBox.Show("Пожалуйста, введите имя пользователя.", "Вход", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsernameInput.Focus();
                return;
            }
            
            CurrentUsername = enteredUsername;
            lblUsername.Text = CurrentUsername;
            lblSignInTo.Text = "Войти как: " + CurrentUsername;
            txtPassword.Text = string.Empty;
            
            ShowPasswordLoginView(); 
        }

        private void txtUsernameInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                btnContinueUsername_Click(sender, e);
                e.Handled = true;
            }
        }

        #endregion

        #region XML Configuration (Copied from frmLogin)

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
                                CurrentUsername = savedUsername;
                                // Update UI in Load event after controls are initialized
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error loading login history. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
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
                
                // Ensure file and root node exist
                XmlDocument myXmlDocument = new XmlDocument();
                if (!File.Exists(fileName))
                {
                    XmlDeclaration xmlDeclaration = myXmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                    myXmlDocument.AppendChild(xmlDeclaration);
                    XmlElement root = myXmlDocument.CreateElement("LoginHistory");
                    myXmlDocument.AppendChild(root);
                }
                else
                {
                     try {
                        myXmlDocument.Load(fileName);
                     } catch (XmlException xmlEx) {
                         string errorMsg = string.Format("Corrupted login history file. Creating new one. Exception: {0}", xmlEx.ToString());
                         Log.Error(errorMsg);
                          File.Delete(fileName); // Delete corrupted file
                          XmlDeclaration xmlDeclaration = myXmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                          myXmlDocument.AppendChild(xmlDeclaration);
                          XmlElement root = myXmlDocument.CreateElement("LoginHistory");
                          myXmlDocument.AppendChild(root);
                     }
                }

                XmlNode rootNode = myXmlDocument.DocumentElement;
                
                if (rootNode != null && !string.IsNullOrEmpty(CurrentUsername))
                {
                    // Clear previous entries (assuming single user history)
                    rootNode.RemoveAll();
                    
                    // Add current user
                    XmlElement userElement = myXmlDocument.CreateElement("User");
                    XmlAttribute userNameAttr = myXmlDocument.CreateAttribute("UserName");
                    userNameAttr.Value = CurrentUsername; 
                    userElement.Attributes.Append(userNameAttr);
                    rootNode.AppendChild(userElement);
                    
                    myXmlDocument.Save(fileName);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error saving login history for user {0}. Exception: {1}", CurrentUsername, ex.ToString());
                Log.Error(errorMsg);
            }
        }

        #endregion

        #region Response Classes (Copied from frmLogin)

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

        private class ErrorResponse
        {
            public string Message { get; set; }
            // Add other potential error fields if needed
        }

        #endregion

        #region Layouting

        private void CenterPanels()
        {
            usernameEntryPanel.Location = new Point(
                (this.ClientSize.Width - usernameEntryPanel.Width) / 2,
                (this.ClientSize.Height - usernameEntryPanel.Height) / 2);

            passwordLoginPanel.Location = new Point(
                (this.ClientSize.Width - passwordLoginPanel.Width) / 2,
                (this.ClientSize.Height - passwordLoginPanel.Height) / 2);

            qrLoginPanel.Location = new Point(
                (this.ClientSize.Width - qrLoginPanel.Width) / 2,
                (this.ClientSize.Height - qrLoginPanel.Height) / 2);

            progressPanel.Location = new Point(
                (this.ClientSize.Width - progressPanel.Width) / 2,
                (this.ClientSize.Height - progressPanel.Height) / 2);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterPanels(); // Recenter when form size changes
        }

        #endregion
    }
}