// Authentication Channel Manager
// Use window.authChannel to avoid redeclaration errors on view reload
if (!window.authChannel) {
  window.authChannel = {
    currentUser: null,
    isWindows: false,
    qrCheckInterval: null,
    
    // Play sound effect using the global channel manager's sound system
    playSound: function(soundName) {
      if (window.channelManager && typeof window.channelManager.playSound === 'function') {
        window.channelManager.playSound(soundName);
      } else {
        console.warn('Channel manager sound system not available');
      }
    },
    
    init: function() {
    console.log('Initializing authentication channel...');
    
    // First, check Windows authentication availability to render UI correctly
    // Then check auth status to hide/show sections appropriately
    this.checkWindowsAvailability()
      .then(() => {
        // After Windows availability is determined and UI is rendered, check auth status
        this.checkAuthStatus();
      })
      .catch(() => {
        // Even if Windows check fails, still check auth status
        this.checkAuthStatus();
      });
  },
  
  checkWindowsAvailability: function() {
    console.log('Checking Windows authentication availability from server...');
    
    return fetch('/api/v1/auth/windows-available', {
      method: 'GET'
    })
    .then(response => response.json())
    .then(data => {
      console.log('Windows auth availability:', data);
      
      this.isWindows = data.available;
      
      // Show/hide Windows login section based on server response
      const windowsLoginSection = document.getElementById('windows-login-section');
      if (windowsLoginSection) {
        windowsLoginSection.style.display = this.isWindows ? 'block' : 'none';
        
        if (!this.isWindows && data.message) {
          console.log('Windows auth not available:', data.message);
        }
      }
      
      console.log('Windows availability check complete, UI rendered');
    })
    .catch(error => {
      console.error('Error checking Windows availability:', error);
      // Default to client-side detection as fallback
      this.isWindows = navigator.platform.toLowerCase().includes('win');
      const windowsLoginSection = document.getElementById('windows-login-section');
      if (windowsLoginSection) {
        windowsLoginSection.style.display = this.isWindows ? 'block' : 'none';
      }
      
      console.log('Windows availability check failed, used fallback detection');
      throw error; // Re-throw to allow init() to handle it
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
    const statusBadge = document.getElementById('auth-status-badge');
    const userDetails = document.getElementById('auth-user-details');
    const authOptionsSection = document.getElementById('auth-options-section');
    const authenticatedActions = document.getElementById('authenticated-actions');
    const windowsLoginSection = document.getElementById('windows-login-section');
    
    if (data.isAuthenticated) {
      // The response has nested structure: data.user contains {$id, isAuthenticated, user}
      // The ACTUAL user data is at data.user.user
      let userObj = data.user;
      
      console.log('Raw response data:', data);
      console.log('Initial user object:', userObj);
      console.log('User object keys:', userObj ? Object.keys(userObj) : 'null');
      
      // Check if userObj has the nested structure with actual user data inside
      if (userObj && userObj.user && typeof userObj.user === 'object') {
        console.log('Found nested user object, extracting actual user data');
        userObj = userObj.user;
      }
      
      this.currentUser = userObj;
      
      // Update status badge
      statusBadge.textContent = 'Authenticated';
      statusBadge.className = 'status-badge authenticated';
      
      if (!userObj) {
        console.error('User object is null or undefined');
        document.getElementById('username-display').textContent = 'Error';
        document.getElementById('role-display').textContent = 'Unknown';
        document.getElementById('session-display').textContent = 'Active';
        userDetails.style.display = 'block';
        return;
      }
      
      // Extract username
      let username = 'N/A';
      if (userObj.username && userObj.username.trim() !== '') {
        username = userObj.username;
      } else if (userObj.login && userObj.login.trim() !== '') {
        username = userObj.login;
      } else if (userObj.userName && userObj.userName.trim() !== '') {
        username = userObj.userName;
      }
      
      // Extract role - handle 0 (Administrator) correctly
      let roleId = 4; // Default to User role
      if (typeof userObj.role === 'number') {
        roleId = userObj.role;
      } else if (typeof userObj.roleId === 'number') {
        roleId = userObj.roleId;
      }
      
      console.log('Extracted username:', username, 'from userObj.username:', userObj.username, 'userObj.login:', userObj.login);
      console.log('Extracted roleId:', roleId, 'from userObj.role:', userObj.role);
      
      // Show user details
      document.getElementById('username-display').textContent = username;
      document.getElementById('role-display').textContent = this.getRoleName(roleId);
      document.getElementById('session-display').textContent = 'Active';
      userDetails.style.display = 'block';
      
      console.log('Displaying user:', username, 'Role:', this.getRoleName(roleId), '(roleId:', roleId, ')');
      
      // Hide authentication options section
      if (authOptionsSection) {
        authOptionsSection.style.display = 'none';
      }
      
      // Hide Windows login section
      if (windowsLoginSection) {
        windowsLoginSection.style.display = 'none';
      }
      
      // Show authenticated actions section (Sign Out)
      if (authenticatedActions) {
        authenticatedActions.style.display = 'block';
      }
      
    } else {
      // Update status badge
      statusBadge.textContent = 'Not Authenticated';
      statusBadge.className = 'status-badge error';
      
      userDetails.style.display = 'none';
      
      // Show authentication options section
      if (authOptionsSection) {
        authOptionsSection.style.display = 'block';
      }
      
      // Show Windows login if available (will be set by checkWindowsAvailability)
      // Don't change windowsLoginSection here, let checkWindowsAvailability handle it
      
      // Hide authenticated actions section
      if (authenticatedActions) {
        authenticatedActions.style.display = 'none';
      }
    }
  },
  
  getRoleName: function(roleId) {
    const roles = {
      0: 'Administrator',
      1: 'Administrator',
      2: 'Manager',
      3: 'Operator',
      4: 'User'
    };
    return roles[roleId] || 'User';
  },
  
  showLogin: function() {
    // Play select sound when opening modal
    this.playSound('button-select');
    // Show modal instead of inline form
    this.openModal('login-modal');
  },
  
  openModal: function(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.add('active');
      
      // Special handling for QR modal
      if (modalId === 'qr-modal') {
        this.generateModalQRCode();
      }
    }
  },
  
  closeModal: function(modalId) {
    // Play back sound when closing modal
    this.playSound('back');
    
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.remove('active');
      
      // Clear any messages
      const messageEl = document.getElementById('modal-login-message');
      if (messageEl) {
        messageEl.className = 'form-message';
        messageEl.textContent = '';
      }
      
      // Stop QR polling if closing QR modal
      if (modalId === 'qr-modal') {
        this.stopQRPolling();
      }
    }
  },
  
  doModalLogin: function() {
    const username = document.getElementById('modal-login-username').value;
    const password = document.getElementById('modal-login-password').value;
    const messageEl = document.getElementById('modal-login-message');
    
    // Client-side validation
    if (!username || !password) {
      // Play error sound for validation failure
      this.playSound('Error');
      messageEl.className = 'form-message error';
      messageEl.textContent = 'Please enter both username and password';
      return;
    }
    
    console.log('Attempting modal login with v1 API...');
    
    // Play select sound for login attempt
    this.playSound('button-select');
    
    // Close login modal and show connection test modal
    this.closeModal('login-modal');
    this.showConnectionTest();
    
    // Simulate connection steps with delays
    setTimeout(() => this.updateConnectionStep(1, 'active'), 300);
    
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
      // Update progress
      this.updateConnectionProgress(33);
      this.updateConnectionStep(1, 'complete');
      setTimeout(() => this.updateConnectionStep(2, 'active'), 200);
      
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
      console.log('Modal login successful:', data);
      
      // Play notification sound for successful login
      this.playSound('Notification');
      
      // Update progress
      this.updateConnectionProgress(66);
      this.updateConnectionStep(2, 'complete');
      setTimeout(() => this.updateConnectionStep(3, 'active'), 200);
      
      // Store JWT token
      if (data.token) {
        localStorage.setItem('auth_token', data.token);
      }
      
      // Complete connection
      setTimeout(() => {
        this.updateConnectionProgress(100);
        this.updateConnectionStep(3, 'complete');
        this.updateConnectionStatus('Connection established successfully!', 'success');
        
        // Close connection modal and refresh status
        setTimeout(() => {
          this.closeModal('connection-test-modal');
          this.checkAuthStatus();
        }, 1500);
      }, 500);
    })
    .catch(error => {
      console.error('Modal login error:', error);
      
      // Play error sound for failed login
      this.playSound('Error');
      
      // Show error in connection test
      this.updateConnectionStatus(error.message || 'Connection failed', 'error');
      this.updateConnectionStep(2, 'error');
      
      // Close connection modal after delay and show error in login modal
      setTimeout(() => {
        this.closeModal('connection-test-modal');
        this.openModal('login-modal');
        messageEl.className = 'form-message error';
        messageEl.textContent = error.message || 'Login failed. Please check your credentials.';
      }, 2000);
    });
  },
  
  showConnectionTest: function() {
    this.openModal('connection-test-modal');
    
    // Reset connection test state
    this.updateConnectionProgress(0);
    this.updateConnectionStatus('Establishing connection...', 'connecting');
    
    // Reset all steps
    for (let i = 1; i <= 3; i++) {
      const step = document.getElementById(`step-${i}`);
      if (step) {
        step.className = 'connection-step';
        const status = step.querySelector('.step-status');
        if (status) status.textContent = '⏳';
      }
    }
  },
  
  updateConnectionStep: function(stepNumber, state) {
    const step = document.getElementById(`step-${stepNumber}`);
    if (!step) return;
    
    step.className = `connection-step ${state}`;
    
    const statusEl = step.querySelector('.step-status');
    if (!statusEl) return;
    
    switch(state) {
      case 'active':
        statusEl.textContent = '🔄';
        break;
      case 'complete':
        statusEl.textContent = '✅';
        break;
      case 'error':
        statusEl.textContent = '❌';
        break;
      default:
        statusEl.textContent = '⏳';
    }
  },
  
  updateConnectionProgress: function(percent) {
    const progressFill = document.getElementById('connection-progress-fill');
    if (progressFill) {
      progressFill.style.width = percent + '%';
    }
  },
  
  updateConnectionStatus: function(text, type) {
    const statusText = document.getElementById('connection-status-text');
    if (statusText) {
      statusText.textContent = text;
      statusText.style.color = type === 'error' ? '#f44336' : (type === 'success' ? '#4caf50' : '#666');
    }
  },
  
  generateModalQRCode: function() {
    const qrContainer = document.getElementById('modal-qr-code-display');
    const qrMessage = document.getElementById('modal-qr-message');
    
    qrContainer.innerHTML = '<span style="color:#999;">Generating...</span>';
    qrMessage.textContent = 'Generating QR code for login...';
    
    console.log('Generating direct login QR code for modal...');
    
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
      console.log('Modal QR code generated successfully');
      
      // Display QR code
      qrContainer.innerHTML = `<img src="${data.qrCode}" alt="QR Code" style="max-width: 100%; height: auto;" />`;
      qrMessage.textContent = 'Scan this QR code with your mobile device to log in';
      
      // Start polling for login success
      this.startQRPolling(deviceId);
    })
    .catch(error => {
      console.error('Error generating modal QR code:', error);
      qrContainer.innerHTML = '<span style="color:#ff4444;">Failed to generate</span>';
      qrMessage.textContent = 'Error generating QR code. Please try again.';
    });
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
    this.playSound('button-select');
    this.openModal('register-modal');
  },
  
  showQRLogin: function() {
    this.playSound('button-select');
    this.openModal('qr-modal');
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
          
          // Play notification sound for successful QR login
          authChannel.playSound('Notification');
          
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
    
    // Play select sound for Windows login attempt
    this.playSound('button-select');
    
    const messageEl = document.getElementById('windows-message');
    if (messageEl) {
      messageEl.textContent = '';
      messageEl.className = 'form-message';
    }
    
    // Show connection test modal
    this.showConnectionTest();
    setTimeout(() => this.updateConnectionStep(1, 'active'), 300);
    
    // First verify Windows auth is available
    fetch('/api/v1/auth/windows-available', {
      method: 'GET'
    })
    .then(response => response.json())
    .then(availabilityData => {
      if (!availabilityData.available) {
        throw new Error(availabilityData.message || 'Windows authentication is not available');
      }
      
      this.updateConnectionProgress(33);
      this.updateConnectionStep(1, 'complete');
      setTimeout(() => this.updateConnectionStep(2, 'active'), 200);
      
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
      this.updateConnectionProgress(66);
      this.updateConnectionStep(2, 'complete');
      setTimeout(() => this.updateConnectionStep(3, 'active'), 200);
      
      if (response.status === 401) {
        throw new Error('Windows authentication required. Please ensure you are logged into Windows.');
      }
      
      if (response.status === 418) {
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
      
      // Play notification sound for successful Windows login
      this.playSound('Notification');
      
      // Store JWT token
      if (data.token) {
        localStorage.setItem('auth_token', data.token);
      }
      
      // Complete connection
      this.updateConnectionProgress(100);
      this.updateConnectionStep(3, 'complete');
      this.updateConnectionStatus('Windows authentication successful!', 'success');
      
      setTimeout(() => {
        this.closeModal('connection-test-modal');
        this.checkAuthStatus();
      }, 1500);
    })
    .catch(error => {
      console.error('Windows login error:', error);
      
      // Play error sound for failed Windows login
      this.playSound('Error');
      
      this.updateConnectionStatus(error.message || 'Windows authentication failed', 'error');
      this.updateConnectionStep(1, 'error');
      
      setTimeout(() => {
        this.closeModal('connection-test-modal');
        if (messageEl) {
          messageEl.className = 'form-message error';
          messageEl.textContent = error.message || 'Windows authentication failed. Please try again.';
        }
      }, 2000);
    });
  },
  
  logout: function() {
    console.log('Logging out...');
    
    // Play back sound for logout
    this.playSound('back');
    
    // Clear stored token
    localStorage.removeItem('auth_token');
    
    // Reset UI
    this.currentUser = null;
    this.checkAuthStatus();
  },
  
  showWebAuthn: function() {
    this.playSound('button-select');
    this.openModal('webauthn-modal');
  },
  
  confirmWebAuthn: function() {
    this.playSound('button-select');
    this.closeModal('webauthn-modal');
    // Redirect to WebAuthn management page
    window.location.href = '/WebAuthn/manage';
  },
  
  showError: function(message) {
    const statusBadge = document.getElementById('auth-status-badge');
    if (statusBadge) {
      statusBadge.textContent = message;
      statusBadge.className = 'status-badge error';
    }
  }
  };
}

// Initialize when DOM is ready or if already loaded
// Always reinitialize on view load (handles AJAX navigation)
if (document.readyState === 'loading') {
  // Document still loading, wait for DOMContentLoaded
  document.addEventListener('DOMContentLoaded', function() {
    window.authChannel.init();
  });
} else {
  // Document already loaded (AJAX view load), initialize immediately
  window.authChannel.init();
}
