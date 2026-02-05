// Authentication Channel Manager
const authChannel = {
  currentUser: null,
  isWindows: false,
  qrCheckInterval: null,
  
  init: function() {
    console.log('Initializing authentication channel...');
    
    // Check Windows authentication availability from server
    this.checkWindowsAvailability();
    
    this.checkAuthStatus();
  },
  
  checkWindowsAvailability: function() {
    console.log('Checking Windows authentication availability from server...');
    
    fetch('/api/v1/auth/windows-available', {
      method: 'GET'
    })
    .then(response => response.json())
    .then(data => {
      console.log('Windows auth availability:', data);
      
      this.isWindows = data.available;
      
      // Show/hide Windows login button based on server response
      const windowsLoginBtn = document.getElementById('windows-login-btn');
      if (windowsLoginBtn) {
        windowsLoginBtn.style.display = this.isWindows ? 'block' : 'none';
        
        if (!this.isWindows && data.message) {
          console.log('Windows auth not available:', data.message);
        }
      }
    })
    .catch(error => {
      console.error('Error checking Windows availability:', error);
      // Default to client-side detection as fallback
      this.isWindows = navigator.platform.toLowerCase().includes('win');
      const windowsLoginBtn = document.getElementById('windows-login-btn');
      if (windowsLoginBtn) {
        windowsLoginBtn.style.display = this.isWindows ? 'block' : 'none';
      }
    });
  },
  
  checkAuthStatus: function() {
    console.log('Checking authentication status...');
    
    // Check if we have a JWT token in localStorage
    const token = localStorage.getItem('auth_token');
    if (token) {
      // Validate token by making a test request
      fetch('/api/v1/auth/validate', {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      .then(response => {
        if (response.ok) {
          return response.json();
        }
        throw new Error('Token invalid');
      })
      .then(data => {
        console.log('Auth status response:', data);
        this.updateAuthStatus({ isAuthenticated: true, user: data });
      })
      .catch(error => {
        console.log('No valid authentication found:', error);
        localStorage.removeItem('auth_token');
        this.updateAuthStatus({ isAuthenticated: false });
      });
    } else {
      console.log('No authentication token found');
      this.updateAuthStatus({ isAuthenticated: false });
    }
  },
  
  updateAuthStatus: function(data) {
    const statusEl = document.getElementById('auth-status');
    const userInfoEl = document.getElementById('auth-user-info');
    const authOptionsSection = document.getElementById('auth-options-section');
    const authActionsSection = document.getElementById('auth-actions-section');
    const windowsLoginSection = document.getElementById('windows-login-section');
    
    if (data.isAuthenticated) {
      this.currentUser = data.user;
      
      // Update status indicator
      statusEl.innerHTML = `
        <div class="status-indicator status-authenticated">
          <div class="status-dot"></div>
          <span>Authenticated</span>
        </div>
      `;
      
      // Show user info
      document.getElementById('username').textContent = data.user.login || data.user.username || 'N/A';
      document.getElementById('user-role').textContent = this.getRoleName(data.user.role);
      document.getElementById('session-status').textContent = 'Active';
      userInfoEl.style.display = 'block';
      
      // Hide authentication options section
      if (authOptionsSection) {
        authOptionsSection.style.display = 'none';
      }
      
      // Hide Windows login section
      if (windowsLoginSection) {
        windowsLoginSection.style.display = 'none';
      }
      
      // Show authenticated actions section (Sign Out)
      if (authActionsSection) {
        authActionsSection.style.display = 'block';
      }
      
    } else {
      // Update status indicator
      statusEl.innerHTML = `
        <div class="status-indicator status-error">
          <div class="status-dot"></div>
          <span>Not Authenticated</span>
        </div>
      `;
      
      userInfoEl.style.display = 'none';
      
      // Show authentication options section
      if (authOptionsSection) {
        authOptionsSection.style.display = 'block';
      }
      
      // Show Windows login if available (will be set by checkWindowsAvailability)
      // Don't change windowsLoginSection here, let checkWindowsAvailability handle it
      
      // Hide authenticated actions section
      if (authActionsSection) {
        authActionsSection.style.display = 'none';
      }
    }
  },
  
  getRoleName: function(roleId) {
    const roles = {
      1: 'Administrator',
      2: 'Manager',
      3: 'Operator',
      4: 'User'
    };
    return roles[roleId] || 'User';
  },
  
  showLogin: function() {
    document.getElementById('login-form-section').style.display = 'block';
    document.getElementById('login-form-section').scrollIntoView({ behavior: 'smooth' });
  },
  
  hideForm: function() {
    document.getElementById('login-form-section').style.display = 'none';
    document.getElementById('login-message').className = 'form-message';
    document.getElementById('login-message').textContent = '';
  },
  
  doLogin: function() {
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    const messageEl = document.getElementById('login-message');
    
    // Client-side validation
    if (!username || !password) {
      messageEl.className = 'form-message error';
      messageEl.textContent = 'Please enter both username and password';
      return;
    }
    
    // Note: No length checks for login to support legacy/seeded data
    
    console.log('Attempting login with v1 API...');
    messageEl.className = 'form-message';
    messageEl.textContent = 'Authenticating...';
    
    fetch('/api/v1/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        login: username,
        password: password
      })
    })
    .then(response => {
      if (response.status === 401) {
        throw new Error('Invalid username or password');
      }
      if (response.status === 400) {
        return response.json().then(data => {
          throw new Error(data.message || 'Invalid request');
        });
      }
      if (!response.ok) {
        throw new Error('Login failed. Please try again.');
      }
      return response.json();
    })
    .then(data => {
      console.log('Login successful:', data);
      
      // Store JWT token
      if (data.token) {
        localStorage.setItem('auth_token', data.token);
      }
      
      messageEl.className = 'form-message success';
      messageEl.textContent = 'Login successful!';
      
      setTimeout(() => {
        this.hideForm();
        this.checkAuthStatus();
      }, 1500);
    })
    .catch(error => {
      console.error('Login error:', error);
      messageEl.className = 'form-message error';
      messageEl.textContent = error.message || 'Login failed. Please check your credentials.';
    });
  },
  
  showRegister: function() {
    alert('Registration feature coming soon! Please contact an administrator to create an account.');
  },
  
  showQRLogin: function() {
    const qrFormSection = document.getElementById('qr-login-form-section');
    if (qrFormSection.style.display === 'none' || !qrFormSection.style.display) {
      qrFormSection.style.display = 'block';
      qrForm.scrollIntoView({ behavior: 'smooth' });
      this.generateQRCode();
    } else {
      qrForm.style.display = 'none';
      this.stopQRPolling();
    }
  },
  
  generateQRCode: function() {
    const qrContainer = document.getElementById('qr-code-display');
    const qrMessage = document.getElementById('qr-message');
    
    qrContainer.innerHTML = '<div style="text-align: center; padding: 20px;">Generating QR code...</div>';
    qrMessage.textContent = 'Generating QR code for login...';
    
    console.log('Generating direct login QR code...');
    
    // Generate a device ID for this session
    const deviceId = 'desktop_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    
    fetch(`/api/v1/auth/qr/direct/generate?username=guest&deviceType=desktop`, {
      method: 'GET'
    })
    .then(response => {
      if (!response.ok) {
        throw new Error('Failed to generate QR code');
      }
      return response.json();
    })
    .then(data => {
      console.log('QR code generated successfully');
      
      // Display QR code
      qrContainer.innerHTML = `<img src="${data.qrCode}" alt="QR Code" style="max-width: 100%; height: auto;" />`;
      qrMessage.textContent = 'Scan this QR code with your mobile device to log in';
      
      // Start polling for login success
      this.startQRPolling(deviceId);
    })
    .catch(error => {
      console.error('Error generating QR code:', error);
      qrContainer.innerHTML = '<div style="text-align: center; padding: 20px; color: #ff4444;">Failed to generate QR code</div>';
      qrMessage.textContent = 'Error generating QR code. Please try again.';
    });
  },
  
  startQRPolling: function(deviceId) {
    console.log('Starting QR login polling for device:', deviceId);
    
    this.stopQRPolling(); // Clear any existing interval
    
    this.qrCheckInterval = setInterval(() => {
      fetch(`/api/v1/auth/qr/direct/check?deviceId=${deviceId}`, {
        method: 'GET'
      })
      .then(response => response.json())
      .then(data => {
        if (data.success && data.token) {
          console.log('QR login successful!');
          
          // Store JWT token
          localStorage.setItem('auth_token', data.token);
          
          // Stop polling
          this.stopQRPolling();
          
          // Hide QR card and show success
          document.getElementById('qr-login-form-section').style.display = 'none';
          document.getElementById('qr-message').textContent = 'Login successful!';
          
          // Refresh auth status
          this.checkAuthStatus();
        }
      })
      .catch(error => {
        console.error('Error checking QR login status:', error);
      });
    }, 2000); // Poll every 2 seconds
  },
  
  stopQRPolling: function() {
    if (this.qrCheckInterval) {
      clearInterval(this.qrCheckInterval);
      this.qrCheckInterval = null;
      console.log('Stopped QR login polling');
    }
  },
  
  hideQRLogin: function() {
    this.stopQRPolling();
    document.getElementById('qr-login-form-section').style.display = 'none';
    document.getElementById('qr-code-display').innerHTML = '';
  },
  
  doWindowsLogin: function() {
    console.log('Attempting Windows authentication...');
    
    const messageEl = document.getElementById('windows-message');
    if (messageEl) {
      messageEl.textContent = 'Authenticating with Windows...';
      messageEl.className = 'form-message';
    }
    
    // First verify Windows auth is available
    fetch('/api/v1/auth/windows-available', {
      method: 'GET'
    })
    .then(response => response.json())
    .then(availabilityData => {
      if (!availabilityData.available) {
        throw new Error(availabilityData.message || 'Windows authentication is not available');
      }
      
      // Proceed with Windows authentication
      return fetch('/api/v1/auth/windows/windows-login', {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Accept': 'application/json'
        }
      });
    })
    .then(response => {
      if (response.status === 401) {
        // Windows authentication challenge - browser will handle this
        throw new Error('Windows authentication required. Please ensure you are logged into Windows.');
      }
      
      if (response.status === 418) {
        // Special case: blank password security issue
        return response.json().then(data => {
          throw new Error(data.message || 'Account security issue detected');
        });
      }
      
      if (!response.ok) {
        throw new Error('Windows authentication failed');
      }
      
      return response.json();
    })
    .then(data => {
      console.log('Windows login successful:', data);
      
      // Store JWT token
      if (data.token) {
        localStorage.setItem('auth_token', data.token);
      }
      
      if (messageEl) {
        messageEl.className = 'form-message success';
        messageEl.textContent = 'Windows authentication successful!';
      }
      
      setTimeout(() => {
        this.checkAuthStatus();
      }, 1500);
    })
    .catch(error => {
      console.error('Windows login error:', error);
      if (messageEl) {
        messageEl.className = 'form-message error';
        messageEl.textContent = error.message || 'Windows authentication failed. Please try again.';
      }
    });
  },
  
  logout: function() {
    console.log('Logging out...');
    
    // Clear stored token
    localStorage.removeItem('auth_token');
    
    // Reset UI
    this.currentUser = null;
    this.checkAuthStatus();
    
    // Show login options again
    document.getElementById('login-card').style.display = 'block';
    document.getElementById('register-card').style.display = 'block';
    document.getElementById('qr-login-card').style.display = 'block';
    
    const windowsLoginBtn = document.getElementById('windows-login-btn');
    if (windowsLoginBtn && this.isWindows) {
      windowsLoginBtn.style.display = 'block';
    }
  },
  
  showWebAuthn: function() {
    // Redirect to WebAuthn management page
    window.location.href = '/WebAuthn/manage';
  },
  
  showError: function(message) {
    const statusEl = document.getElementById('auth-status');
    statusEl.innerHTML = `
      <div class="status-indicator status-error">
        <div class="status-dot"></div>
        <span>${message}</span>
      </div>
    `;
  }
};

// Initialize when DOM is ready or if already loaded
// Use a flag to prevent double initialization
if (!window.authChannelInitialized) {
  window.authChannelInitialized = true;
  
  if (document.readyState === 'loading') {
    // Document still loading, wait for DOMContentLoaded
    document.addEventListener('DOMContentLoaded', function() {
      authChannel.init();
    });
  } else {
    // Document already loaded (AJAX view load), initialize immediately
    authChannel.init();
  }
}
