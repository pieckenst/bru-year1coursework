// Wii Settings Manager
const wiiSettings = {
  settings: {
    language: 'en',
    audio: 'stereo',
    screen: '16:9',
    sensor: 'above',
    volumes: {
      master: 80,
      sfx: 70,
      music: 60
    },
    sensorSensitivity: 3,
    burnInReduction: false,
    timeSync: true,
    wiiConnect24: true,
    standbyConnection: true,
    parentalControls: false
  },

  init: function() {
    console.log('Initializing Wii Settings...');
    
    // Load settings from localStorage
    this.loadSettings();
    
    // Update UI with current settings
    this.updateAllDisplays();
    
    // Start clock update
    this.updateClock();
    setInterval(() => this.updateClock(), 1000);
    
    // Animate settings container
    setTimeout(() => {
      const container = document.querySelector('.wii-settings-container');
      if (container) {
        container.classList.add('animate');
      }
    }, 100);
  },

  loadSettings: function() {
    const saved = localStorage.getItem('wii_settings');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        this.settings = { ...this.settings, ...parsed };
        console.log('Loaded settings from localStorage:', this.settings);
      } catch (e) {
        console.error('Failed to parse saved settings:', e);
      }
    }
  },

  saveSettings: function() {
    localStorage.setItem('wii_settings', JSON.stringify(this.settings));
    console.log('Settings saved to localStorage');
  },

  updateAllDisplays: function() {
    // Update language display
    const langNames = {
      'en': 'English',
      'ru': 'Русский',
      'de': 'Deutsch',
      'fr': 'Français',
      'es': 'Español',
      'ja': '日本語'
    };
    const langEl = document.getElementById('current-language');
    if (langEl) langEl.textContent = langNames[this.settings.language] || 'English';

    // Update audio display
    const audioEl = document.getElementById('current-audio');
    if (audioEl) {
      const audioName = this.settings.audio.charAt(0).toUpperCase() + this.settings.audio.slice(1);
      audioEl.textContent = audioName;
    }

    // Update screen display
    const screenEl = document.getElementById('current-screen');
    if (screenEl) {
      screenEl.textContent = this.settings.screen === '16:9' ? '16:9 Widescreen' : '4:3 Standard';
    }

    // Update sensor display
    const sensorEl = document.getElementById('current-sensor');
    if (sensorEl) {
      sensorEl.textContent = this.settings.sensor === 'above' ? 'Above TV' : 'Below TV';
    }

    // Update WiiConnect24 display
    const wiiconnectEl = document.getElementById('current-wiiconnect');
    if (wiiconnectEl) {
      wiiconnectEl.textContent = this.settings.wiiConnect24 ? 'On' : 'Off';
    }
  },

  updateClock: function() {
    const now = new Date();
    
    // Update datetime display in main view
    const datetimeEl = document.getElementById('current-datetime');
    if (datetimeEl) {
      const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
      datetimeEl.textContent = timeStr;
    }

    // Update datetime modal if open
    const dateDisplay = document.getElementById('date-display');
    const timeDisplay = document.getElementById('time-display');
    if (dateDisplay && timeDisplay) {
      const dateStr = now.toLocaleDateString('en-US', { 
        month: '2-digit', 
        day: '2-digit', 
        year: 'numeric' 
      });
      const timeStr = now.toLocaleTimeString('en-US', { 
        hour: '2-digit', 
        minute: '2-digit', 
        second: '2-digit',
        hour12: false
      });
      dateDisplay.textContent = dateStr;
      timeDisplay.textContent = timeStr;
    }
  },

  // Modal Management
  openModal: function(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.add('active');
      
      // Update modal content based on current settings
      this.updateModalContent(modalId);
    }
  },

  closeModal: function(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.remove('active');
    }
  },

  updateModalContent: function(modalId) {
    switch(modalId) {
      case 'language-modal':
        this.updateLanguageChecks();
        break;
      case 'audio-modal':
        this.updateAudioChecks();
        this.updateVolumeSliders();
        break;
      case 'screen-modal':
        this.updateScreenChecks();
        this.updateBurnInToggle();
        break;
      case 'sensor-modal':
        this.updateSensorChecks();
        this.updateSensitivitySlider();
        break;
      case 'wiiconnect24-modal':
        this.updateWiiConnect24Toggles();
        break;
      case 'parental-modal':
        this.updateParentalToggle();
        break;
    }
  },

  // Language Settings
  openLanguageSettings: function() {
    this.openModal('language-modal');
  },

  setLanguage: function(lang) {
    this.settings.language = lang;
    this.saveSettings();
    this.updateAllDisplays();
    this.updateLanguageChecks();
    
    // TODO: Implement actual language switching for HTML content
    console.log('Language set to:', lang);
  },

  updateLanguageChecks: function() {
    const langs = ['en', 'ru', 'de', 'fr', 'es', 'ja'];
    langs.forEach(lang => {
      const check = document.getElementById(`check-${lang}`);
      if (check) {
        check.textContent = lang === this.settings.language ? '✓' : '';
      }
    });
  },

  // Audio Settings
  openAudioSettings: function() {
    this.openModal('audio-modal');
  },

  setAudio: function(mode) {
    this.settings.audio = mode;
    this.saveSettings();
    this.updateAllDisplays();
    this.updateAudioChecks();
    console.log('Audio mode set to:', mode);
  },

  updateAudioChecks: function() {
    const modes = ['stereo', 'mono', 'surround'];
    modes.forEach(mode => {
      const check = document.getElementById(`check-${mode}`);
      if (check) {
        check.textContent = mode === this.settings.audio ? '✓' : '';
      }
    });
  },

  updateVolume: function(type, value) {
    this.settings.volumes[type] = parseInt(value);
    this.saveSettings();
    
    const valueEl = document.getElementById(`${type}-volume-value`);
    if (valueEl) {
      valueEl.textContent = value + '%';
    }
    
    console.log(`${type} volume set to:`, value);
  },

  updateVolumeSliders: function() {
    ['master', 'sfx', 'music'].forEach(type => {
      const slider = document.getElementById(`${type}-volume`);
      const valueEl = document.getElementById(`${type}-volume-value`);
      if (slider && valueEl) {
        slider.value = this.settings.volumes[type];
        valueEl.textContent = this.settings.volumes[type] + '%';
      }
    });
  },

  // Screen Settings
  openScreenSettings: function() {
    this.openModal('screen-modal');
  },

  setScreen: function(ratio) {
    this.settings.screen = ratio;
    this.saveSettings();
    this.updateAllDisplays();
    this.updateScreenChecks();
    console.log('Screen ratio set to:', ratio);
  },

  updateScreenChecks: function() {
    const check169 = document.getElementById('check-16-9');
    const check43 = document.getElementById('check-4-3');
    if (check169) check169.textContent = this.settings.screen === '16:9' ? '✓' : '';
    if (check43) check43.textContent = this.settings.screen === '4:3' ? '✓' : '';
  },

  toggleBurnIn: function() {
    this.settings.burnInReduction = !this.settings.burnInReduction;
    this.saveSettings();
    this.updateBurnInToggle();
    console.log('Burn-in reduction:', this.settings.burnInReduction);
  },

  updateBurnInToggle: function() {
    const toggle = document.getElementById('burnin-toggle');
    if (toggle) {
      if (this.settings.burnInReduction) {
        toggle.classList.add('active');
      } else {
        toggle.classList.remove('active');
      }
    }
  },

  // Date & Time Settings
  openDateTimeSettings: function() {
    this.openModal('datetime-modal');
    this.updateClock();
  },

  toggleTimeSync: function() {
    this.settings.timeSync = !this.settings.timeSync;
    this.saveSettings();
    
    const toggle = document.getElementById('timesync-toggle');
    if (toggle) {
      if (this.settings.timeSync) {
        toggle.classList.add('active');
      } else {
        toggle.classList.remove('active');
      }
    }
    console.log('Time sync:', this.settings.timeSync);
  },

  // Internet Settings
  openInternetSettings: function() {
    this.openModal('internet-modal');
  },

  // Sensor Bar Settings
  openSensorSettings: function() {
    this.openModal('sensor-modal');
  },

  setSensor: function(position) {
    this.settings.sensor = position;
    this.saveSettings();
    this.updateAllDisplays();
    this.updateSensorChecks();
    console.log('Sensor position set to:', position);
  },

  updateSensorChecks: function() {
    const checkAbove = document.getElementById('check-above');
    const checkBelow = document.getElementById('check-below');
    if (checkAbove) checkAbove.textContent = this.settings.sensor === 'above' ? '✓' : '';
    if (checkBelow) checkBelow.textContent = this.settings.sensor === 'below' ? '✓' : '';
  },

  updateSensitivity: function(value) {
    this.settings.sensorSensitivity = parseInt(value);
    this.saveSettings();
    
    const valueEl = document.getElementById('sensor-sensitivity-value');
    if (valueEl) {
      valueEl.textContent = value;
    }
    console.log('Sensor sensitivity set to:', value);
  },

  updateSensitivitySlider: function() {
    const slider = document.getElementById('sensor-sensitivity');
    const valueEl = document.getElementById('sensor-sensitivity-value');
    if (slider && valueEl) {
      slider.value = this.settings.sensorSensitivity;
      valueEl.textContent = this.settings.sensorSensitivity;
    }
  },

  // WiiConnect24 Settings
  openWiiConnect24Settings: function() {
    this.openModal('wiiconnect24-modal');
  },

  toggleWiiConnect24: function() {
    this.settings.wiiConnect24 = !this.settings.wiiConnect24;
    this.saveSettings();
    this.updateAllDisplays();
    this.updateWiiConnect24Toggles();
    console.log('WiiConnect24:', this.settings.wiiConnect24);
  },

  toggleStandbyConnection: function() {
    this.settings.standbyConnection = !this.settings.standbyConnection;
    this.saveSettings();
    this.updateWiiConnect24Toggles();
    console.log('Standby connection:', this.settings.standbyConnection);
  },

  updateWiiConnect24Toggles: function() {
    const wiiconnectToggle = document.getElementById('wiiconnect24-toggle');
    const standbyToggle = document.getElementById('standby-toggle');
    
    if (wiiconnectToggle) {
      if (this.settings.wiiConnect24) {
        wiiconnectToggle.classList.add('active');
      } else {
        wiiconnectToggle.classList.remove('active');
      }
    }
    
    if (standbyToggle) {
      if (this.settings.standbyConnection) {
        standbyToggle.classList.add('active');
      } else {
        standbyToggle.classList.remove('active');
      }
    }
  },

  // Parental Controls
  openParentalSettings: function() {
    this.openModal('parental-modal');
  },

  toggleParentalControls: function() {
    // Parental controls require PIN - show info message
    alert('Parental Controls require a PIN. Please contact your administrator to set up parental controls.');
    console.log('Parental controls toggle attempted');
  },

  updateParentalToggle: function() {
    const toggle = document.getElementById('parental-toggle');
    if (toggle) {
      if (this.settings.parentalControls) {
        toggle.classList.add('active');
      } else {
        toggle.classList.remove('active');
      }
    }
  }
};

// Initialize when DOM is ready or if already loaded
if (!window.wiiSettingsInitialized) {
  window.wiiSettingsInitialized = true;
  
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
      wiiSettings.init();
    });
  } else {
    wiiSettings.init();
  }
}
