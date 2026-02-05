/**
 * Dynamic Wii Channel Management System
 * Handles loading, rendering, and interaction with configurable channels
 * Enhanced with comprehensive debug logging
 */

class WiiChannelManager {
    constructor() {
        this.channels = [];
        this.channelConfig = {};
        this.currentChannel = null;
        this.isLoading = false;
        this.isActivating = false; // Prevent multiple simultaneous activations
        this.cache = new Map();
        this.eventListeners = new Map();
        this.debugMode = true; // Enable debug logging
        this.lastActivation = 0; // Track last activation time
        
        // Configuration
        this.config = {
            apiBaseUrl: '/api/WiiChannel',
            cacheTimeout: 5 * 60 * 1000, // 5 minutes
            animationDefaults: {
                duration: 900,
                type: 'default'
            },
            positions: {
                maxChannels: 12,
                gridCols: 4,
                gridRows: 3
            }
        };

        this.log('info', 'WiiChannelManager constructor called', {
            config: this.config,
            userAgent: navigator.userAgent,
            timestamp: new Date().toISOString()
        });

        this.init();
    }

    /**
     * Enhanced logging function
     */
    log(level, message, data = null) {
        if (!this.debugMode) return;
        
        const timestamp = new Date().toISOString();
        const logEntry = {
            timestamp,
            level: level.toUpperCase(),
            component: 'WiiChannelManager',
            message,
            data
        };

        const consoleMethod = level === 'error' ? 'error' : level === 'warn' ? 'warn' : 'log';
        console[consoleMethod](`[${timestamp}] [${level.toUpperCase()}] WiiChannelManager: ${message}`, data || '');
        
        // Store logs for debugging
        if (!window.wiiChannelLogs) window.wiiChannelLogs = [];
        window.wiiChannelLogs.push(logEntry);
        
        // Keep only last 100 log entries
        if (window.wiiChannelLogs.length > 100) {
            window.wiiChannelLogs = window.wiiChannelLogs.slice(-100);
        }
    }

    /**
     * Initialize the channel manager
     */
    async init() {
        this.log('info', 'Initializing WiiChannelManager...');
        
        try {
            // Show loading state with proper Wii styling first
            this.log('debug', 'Rendering loading state...');
            this.renderLoadingState();
            
            this.log('debug', 'Loading channel configuration...');
            await this.loadChannelConfiguration();
            
            this.log('debug', 'Loading active channels...');
            await this.loadActiveChannels();
            
            this.log('debug', 'Rendering channels...');
            this.renderChannels();
            
            this.log('debug', 'Binding events...');
            this.bindEvents();
            
            // Check if we need to refresh channels due to cached menu load
            if (window.needsChannelRefresh) {
                this.log('info', 'Detected cached menu load - forcing channel refresh');
                window.needsChannelRefresh = false;
                await this.refreshChannels();
            }
            
            // Set up periodic validation to catch blank channel issues
            this.setupPeriodicValidation();
            
            this.log('info', 'WiiChannelManager initialized successfully', {
                channelCount: this.channels.length,
                configKeys: Object.keys(this.channelConfig),
                cacheSize: this.cache.size
            });
        } catch (error) {
            this.log('error', 'Failed to initialize WiiChannelManager', {
                error: error.message,
                stack: error.stack
            });
            this.renderFallbackChannels();
        }
    }

    /**
     * Load channel configuration from server
     */
    async loadChannelConfiguration() {
        const cacheKey = 'channel_config';
        const cached = this.getFromCache(cacheKey);
        
        if (cached) {
            this.log('debug', 'Using cached channel configuration', { cacheKey, dataSize: JSON.stringify(cached).length });
            this.channelConfig = cached;
            return;
        }

        this.log('debug', 'Fetching channel configuration from server', { url: `${this.config.apiBaseUrl}/config` });
        
        try {
            const startTime = performance.now();
            const response = await fetch(`${this.config.apiBaseUrl}/config`);
            const loadTime = performance.now() - startTime;
            
            this.log('debug', 'Channel configuration fetch response', {
                status: response.status,
                statusText: response.statusText,
                headers: Object.fromEntries(response.headers.entries()),
                loadTime: `${loadTime.toFixed(2)}ms`
            });
            
            if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            
            const config = await response.json();
            this.channelConfig = typeof config === 'string' ? JSON.parse(config) : config;
            
            this.log('info', 'Channel configuration loaded successfully', {
                configSize: JSON.stringify(this.channelConfig).length,
                channelCount: this.channelConfig.channels?.length || 0,
                loadTime: `${loadTime.toFixed(2)}ms`
            });
            
            this.setCache(cacheKey, this.channelConfig);
        } catch (error) {
            this.log('error', 'Failed to load channel configuration', {
                error: error.message,
                url: `${this.config.apiBaseUrl}/config`
            });
            this.channelConfig = this.getDefaultConfiguration();
            this.log('warn', 'Using default configuration as fallback');
        }
    }

    /**
     * Load active channels from server
     */
    async loadActiveChannels(forceRefresh = false) {
        const cacheKey = 'active_channels';
        const cached = forceRefresh ? null : this.getFromCache(cacheKey);
        
        if (cached && !forceRefresh) {
            this.log('debug', 'Using cached active channels', { 
                cacheKey, 
                channelCount: cached.length,
                channels: cached.map(c => ({ key: c.channelKey, name: c.name, position: c.position }))
            });
            this.channels = cached;
            return;
        }

        this.log('debug', 'Fetching active channels from server', { 
            url: `${this.config.apiBaseUrl}/active`,
            forceRefresh: forceRefresh
        });

        try {
            const startTime = performance.now();
            // Add cache-busting parameter when forcing refresh
            const url = forceRefresh 
                ? `${this.config.apiBaseUrl}/active?_t=${Date.now()}`
                : `${this.config.apiBaseUrl}/active`;
            const response = await fetch(url);
            const loadTime = performance.now() - startTime;
            
            this.log('debug', 'Active channels fetch response', {
                status: response.status,
                statusText: response.statusText,
                headers: Object.fromEntries(response.headers.entries()),
                loadTime: `${loadTime.toFixed(2)}ms`,
                forceRefresh: forceRefresh
            });
            
            if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            
            const responseData = await response.json();
            
            this.log('debug', 'Raw API response received', {
                responseType: typeof responseData,
                isArray: Array.isArray(responseData),
                hasChannelsProperty: responseData && typeof responseData === 'object' && 'channels' in responseData,
                responseKeys: responseData && typeof responseData === 'object' ? Object.keys(responseData) : null,
                responseLength: Array.isArray(responseData) ? responseData.length : null,
                firstItemKeys: Array.isArray(responseData) && responseData.length > 0 ? Object.keys(responseData[0]) : null,
                rawResponseSample: responseData && typeof responseData === 'object' ? 
                    (Array.isArray(responseData) ? 
                        responseData.slice(0, 2) : 
                        Object.keys(responseData).slice(0, 5).reduce((obj, key) => {
                            obj[key] = responseData[key];
                            return obj;
                        }, {})
                    ) : responseData
            });
            
            // Handle different response formats
            let parsedChannels = null;
            
            if (Array.isArray(responseData)) {
                // Direct array of channels
                parsedChannels = responseData;
                this.log('debug', 'Response is direct array', { length: responseData.length });
            } else if (responseData && Array.isArray(responseData.channels)) {
                // Response wrapped in object with channels property
                parsedChannels = responseData.channels;
                this.log('debug', 'Response has channels property', { length: responseData.channels.length });
            } else if (responseData && typeof responseData === 'object') {
                // Check if response is a single channel object
                if (responseData.channelKey || responseData.ChannelKey || responseData.id || responseData.Id) {
                    parsedChannels = [responseData];
                    this.log('debug', 'Response is single channel object', { channelKey: responseData.channelKey || responseData.ChannelKey });
                } else {
                    // Check if response has properties that look like channel data
                    const possibleChannelKeys = Object.keys(responseData);
                    this.log('debug', 'Analyzing object response', { 
                        keys: possibleChannelKeys,
                        firstValue: possibleChannelKeys.length > 0 ? responseData[possibleChannelKeys[0]] : null
                    });
                    
                    // Try to find channels in the response object
                    let foundChannels = null;
                    for (const key of possibleChannelKeys) {
                        if (Array.isArray(responseData[key])) {
                            foundChannels = responseData[key];
                            this.log('debug', 'Found array in response object', { key, length: foundChannels.length });
                            break;
                        }
                    }
                    
                    if (foundChannels) {
                        parsedChannels = foundChannels;
                    } else {
                        // Last resort: treat the entire object as a single channel
                        parsedChannels = [responseData];
                        this.log('warn', 'Treating entire response as single channel object');
                    }
                }
            } else {
                throw new Error('Invalid response format: expected array or object with channels');
            }
            
            if (!Array.isArray(parsedChannels)) {
                throw new Error(`Failed to parse channels: result is not an array. Got: ${typeof parsedChannels}`);
            }
            
            this.channels = parsedChannels;
            
            // Validate that we have valid channel data
            if (!Array.isArray(this.channels)) {
                throw new Error(`Channels is not an array after parsing: ${typeof this.channels}`);
            }
            
            if (this.channels.length === 0) {
                this.log('warn', 'No channels received from server - using fallback');
                this.channels = this.getDefaultChannels();
            }
            
            // Log detailed channel information
            this.log('debug', 'Channels after parsing', {
                channelCount: this.channels.length,
                channels: this.channels.map(c => ({
                    channelKey: c.channelKey || c.ChannelKey,
                    name: c.name || c.Name,
                    position: c.position || c.Position,
                    isActive: c.isActive || c.IsActive
                }))
            });
            
            this.log('info', 'Active channels loaded successfully', {
                channelCount: this.channels.length,
                loadTime: `${loadTime.toFixed(2)}ms`,
                forceRefresh: forceRefresh,
                channels: this.channels.map(c => ({
                    key: c.channelKey,
                    name: c.name,
                    position: c.position,
                    spriteId: c.spriteId,
                    iconPath: c.iconPath,
                    isActive: c.isActive
                }))
            });
            
            this.setCache(cacheKey, this.channels);
        } catch (error) {
            this.log('error', 'Failed to load active channels', {
                error: error.message,
                url: `${this.config.apiBaseUrl}/active`,
                forceRefresh: forceRefresh
            });
            this.channels = this.getDefaultChannels();
            this.log('warn', 'Using default channels as fallback', { channelCount: this.channels.length });
        }
    }

    /**
     * Load specific channel by key
     */
    async loadChannelByKey(channelKey) {
        this.log('debug', 'Loading channel by key', { channelKey });
        
        const cacheKey = `channel_${channelKey}`;
        const cached = this.getFromCache(cacheKey);
        
        if (cached) {
            this.log('debug', 'Using cached channel data', {
                channelKey,
                cacheKey,
                channelName: cached.name
            });
            return cached;
        }

        this.log('debug', 'Fetching channel from server', {
            channelKey,
            url: `${this.config.apiBaseUrl}/key/${channelKey}`
        });

        try {
            const startTime = performance.now();
            const response = await fetch(`${this.config.apiBaseUrl}/key/${channelKey}`);
            const loadTime = performance.now() - startTime;
            
            this.log('debug', 'Channel fetch response', {
                channelKey,
                status: response.status,
                statusText: response.statusText,
                loadTime: `${loadTime.toFixed(2)}ms`
            });
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const channel = await response.json();
            this.log('info', 'Channel loaded successfully', {
                channelKey,
                name: channel.name,
                actionType: channel.actionType,
                spriteId: channel.spriteId,
                iconPath: channel.iconPath,
                loadTime: `${loadTime.toFixed(2)}ms`
            });
            
            this.setCache(cacheKey, channel);
            return channel;
        } catch (error) {
            this.log('error', 'Failed to load channel by key', {
                channelKey,
                error: error.message,
                url: `${this.config.apiBaseUrl}/key/${channelKey}`
            });
            return null;
        }
    }

    /**
     * Render channels in the menu grid
     */
    renderChannels() {
        // Don't render if we're not on the menu view
        if (typeof currentView !== 'undefined' && currentView !== 'menu') {
            this.log('debug', 'Skipping channel rendering - not on menu view', { currentView });
            return;
        }
        
        this.log('info', 'Rendering channels in menu grid', {
            channelCount: this.channels.length,
            gridStructure: `${this.config.positions.gridCols}x${this.config.positions.gridRows}`,
            maxChannels: this.config.positions.maxChannels
        });

        // Find channels container with multiple fallback selectors
        const channelsContainer = this.findChannelsContainer();
        
        if (!channelsContainer) {
            this.log('error', 'Channels container not found - likely not on menu view', {
                currentView: typeof currentView !== 'undefined' ? currentView : 'undefined',
                availableSelectors: [
                    document.querySelector('.channels') ? '.channels' : null,
                    document.querySelector('#dynamic-channels') ? '#dynamic-channels' : null,
                    document.querySelector('.main-menu .channels') ? '.main-menu .channels' : null,
                    document.querySelector('.main-menu') ? '.main-menu (parent exists)' : null
                ].filter(Boolean)
            });
            return;
        }

        // Clear existing channels
        channelsContainer.innerHTML = '';
        this.log('debug', 'Cleared existing channel content');

        // Create the authentic Wii grid structure (4 columns, 3 rows)
        const cols = [];
        for (let i = 0; i < this.config.positions.gridCols; i++) {
            const col = document.createElement('div');
            col.className = i === 0 ? 'col first' : 'col';
            cols.push(col);
        }
        
        this.log('debug', 'Created grid columns', {
            columnCount: cols.length,
            firstColumnClass: 'col first',
            otherColumnsClass: 'col'
        });

        // Sort channels by position
        const sortedChannels = [...this.channels].sort((a, b) => a.position - b.position);
        this.log('debug', 'Sorted channels by position', {
            originalOrder: this.channels.map(c => ({ key: c.channelKey, pos: c.position })),
            sortedOrder: sortedChannels.map(c => ({ key: c.channelKey, pos: c.position }))
        });

        // Fill all 12 positions (authentic Wii layout)
        for (let position = 1; position <= this.config.positions.maxChannels; position++) {
            const channel = sortedChannels.find(c => c.position === position);
            const colIndex = Math.floor((position - 1) / this.config.positions.gridRows);
            
            this.log('debug', `Processing position ${position}`, {
                position,
                colIndex,
                hasChannel: !!channel,
                channelKey: channel?.channelKey || null
            });
            
            if (colIndex < cols.length) {
                const channelElement = this.createChannelElement(channel, position);
                cols[colIndex].appendChild(channelElement);
                
                this.log('debug', `Added channel element to column ${colIndex}`, {
                    position,
                    colIndex,
                    channelKey: channel?.channelKey || 'blank',
                    elementClasses: channelElement.className
                });
            } else {
                this.log('warn', `Column index out of bounds for position ${position}`, {
                    position,
                    colIndex,
                    maxColumns: cols.length
                });
            }
        }

        // Add columns to container
        cols.forEach((col, index) => {
            channelsContainer.appendChild(col);
            this.log('debug', `Added column ${index} to container`, {
                columnIndex: index,
                childCount: col.children.length,
                className: col.className
            });
        });

        this.log('info', 'Channel rendering completed successfully', {
            totalPositions: this.config.positions.maxChannels,
            occupiedPositions: sortedChannels.length,
            blankPositions: this.config.positions.maxChannels - sortedChannels.length,
            columnsCreated: cols.length
        });
    }

    /**
     * Create a channel element following authentic Wii styling with sprite support
     */
    createChannelElement(channel, position) {
        this.log('debug', `Creating channel element for position ${position}`, {
            position,
            channel: channel ? {
                key: channel.channelKey,
                name: channel.name,
                spriteId: channel.spriteId,
                iconPath: channel.iconPath,
                isActive: channel.isActive
            } : null
        });

        const channelDiv = document.createElement('div');
        
        if (channel && channel.isActive) {
            // Occupied channel - follows original Wii structure exactly
            channelDiv.className = 'channel-icon occupied';
            
            const img = document.createElement('img');
            let finalImagePath = '';
            
            // Use sprite-based icon system with fallback to channel-wiilogo.png
            if (channel.spriteId) {
                // Use sprite from channel spritesheet
                finalImagePath = `/customerui/assets/images/channel-${channel.spriteId}.png`;
                img.src = finalImagePath;
                
                this.log('debug', `Using sprite for channel ${channel.channelKey}`, {
                    spriteId: channel.spriteId,
                    imagePath: finalImagePath
                });
                
                img.onerror = () => {
                    // Fallback to generic Wii logo
                    const fallbackPath = '/customerui/assets/images/channel-wiilogo.png';
                    this.log('warn', `Sprite failed for channel ${channel.channelKey}, falling back to Wii logo`, {
                        originalPath: finalImagePath,
                        fallbackPath: fallbackPath
                    });
                    img.src = fallbackPath;
                };
            } else if (channel.iconPath) {
                // Use custom icon path
                finalImagePath = channel.iconPath;
                img.src = finalImagePath;
                
                this.log('debug', `Using custom icon path for channel ${channel.channelKey}`, {
                    iconPath: finalImagePath
                });
                
                img.onerror = () => {
                    // Fallback to generic Wii logo
                    const fallbackPath = '/customerui/assets/images/channel-wiilogo.png';
                    this.log('warn', `Custom icon failed for channel ${channel.channelKey}, falling back to Wii logo`, {
                        originalPath: finalImagePath,
                        fallbackPath: fallbackPath
                    });
                    img.src = fallbackPath;
                };
            } else {
                // Default to generic Wii logo
                finalImagePath = '/customerui/assets/images/channel-wiilogo.png';
                img.src = finalImagePath;
                
                this.log('debug', `Using default Wii logo for channel ${channel.channelKey}`, {
                    imagePath: finalImagePath
                });
            }
            
            img.alt = channel.name || 'Channel';
            img.onload = () => {
                this.log('debug', `Image loaded successfully for channel ${channel.channelKey}`, {
                    imagePath: img.src,
                    naturalWidth: img.naturalWidth,
                    naturalHeight: img.naturalHeight
                });
            };
            
            const hoverDiv = document.createElement('div');
            hoverDiv.className = 'hover';
            hoverDiv.id = position.toString();
            hoverDiv.setAttribute('data-channel-key', channel.channelKey);
            hoverDiv.setAttribute('data-channel-name', channel.name);
            hoverDiv.setAttribute('data-img', channel.splashImagePath || channel.iconPath || '/customerui/assets/images/channel-wiilogo.png');
            hoverDiv.setAttribute('data-position', position.toString());
            
            // Add hover sound effect
            hoverDiv.setAttribute('onmouseover', 'hover()');
            
            // Use ONLY the direct event listener to prevent duplicate activations
            // Remove any existing event listeners first
            hoverDiv.onclick = null;
            
            // Add single click handler with proper event management
            hoverDiv.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation(); // Prevent other handlers from firing
                
                console.log('=== CHANNEL CLICK DEBUG ===');
                console.log('Channel clicked:', channel.channelKey);
                console.log('Position:', position);
                console.log('Event target:', e.target);
                console.log('Current target:', e.currentTarget);
                
                if (window.channelManager && window.channelManager.activateChannel) {
                    window.channelManager.activateChannel(channel.channelKey, position);
                } else {
                    console.error('Channel manager not available for direct activation');
                }
            }, { once: false, capture: true }); // Use capture phase to handle first
            
            channelDiv.appendChild(img);
            channelDiv.appendChild(hoverDiv);
            
            this.log('debug', `Created occupied channel element for position ${position}`, {
                channelKey: channel.channelKey,
                finalImagePath: finalImagePath,
                elementClasses: channelDiv.className
            });
        } else {
            // Blank channel - uses existing Wii CSS classes with sprite animation
            channelDiv.className = 'channel-icon blank';
            
            const hoverDiv = document.createElement('div');
            hoverDiv.className = 'hover';
            hoverDiv.id = position.toString();
            
            channelDiv.appendChild(hoverDiv);
            
            this.log('debug', `Created blank channel element for position ${position}`, {
                elementClasses: channelDiv.className
            });
        }
        
        return channelDiv;
    }

    /**
     * Activate a channel
     */
    async activateChannel(channelKey, position) {
        console.log('=== CHANNEL ACTIVATION DEBUG ===');
        console.log('Method called with:', { channelKey, position });
        console.log('Channel manager instance:', this);
        console.log('Available channels:', this.channels.map(c => c.channelKey));
        
        this.log('info', `Channel activation requested: ${channelKey} at position ${position}`, {
            channelKey,
            position,
            isLoading: this.isLoading,
            currentChannel: this.currentChannel?.channelKey || null
        });

        // Prevent duplicate activations
        if (this.isLoading || this.isActivating) {
            this.log('warn', 'Channel activation blocked - already loading or activating', { 
                channelKey, 
                position,
                isLoading: this.isLoading,
                isActivating: this.isActivating
            });
            return;
        }
        
        // Prevent rapid successive activations of the same channel
        const now = Date.now();
        const lastActivation = this.lastActivation || 0;
        const timeSinceLastActivation = now - lastActivation;
        
        if (timeSinceLastActivation < 1000) { // 1 second cooldown
            this.log('warn', 'Channel activation blocked - too soon after last activation', { 
                channelKey, 
                position,
                timeSinceLastActivation: `${timeSinceLastActivation}ms`
            });
            return;
        }
        
        this.lastActivation = now;
        this.isLoading = true;
        this.isActivating = true;
        const startTime = performance.now();
        
        try {
            // Play activation sound
            this.log('debug', 'Playing activation sound: zip', { channelKey });
            this.playSound('zip');
            
            // Find channel data from already loaded channels instead of fetching individually
            this.log('debug', 'Finding channel data from loaded channels...', { channelKey });
            const channel = this.channels.find(c => c.channelKey === channelKey);
            if (!channel) {
                this.log('error', `Channel not found in loaded channels: ${channelKey}`, { 
                    channelKey, 
                    position,
                    availableChannels: this.channels.map(c => c.channelKey)
                });
                return;
            }
            
            this.log('info', 'Channel data found successfully', {
                channelKey: channel.channelKey,
                name: channel.name,
                actionType: channel.actionType,
                actionUrl: channel.actionUrl,
                spriteId: channel.spriteId,
                iconPath: channel.iconPath
            });
            
            this.currentChannel = channel;
            
            // Calculate animation origin
            const hoverElement = document.querySelector(`[data-channel-key="${channelKey}"]`);
            if (!hoverElement) {
                this.log('error', 'Hover element not found for channel', { channelKey });
                return;
            }
            
            const centerX = hoverElement.offsetLeft + hoverElement.offsetWidth / 2;
            const centerY = hoverElement.offsetTop + hoverElement.offsetHeight / 2;
            
            this.log('debug', 'Animation origin calculated', {
                channelKey,
                centerX,
                centerY,
                elementOffset: { left: hoverElement.offsetLeft, top: hoverElement.offsetTop },
                elementSize: { width: hoverElement.offsetWidth, height: hoverElement.offsetHeight }
            });
            
            // Set transform origin for animation
            const mainMenu = document.querySelector('.main-menu');
            if (mainMenu) {
                mainMenu.style.transformOrigin = `${centerX}px ${centerY}px 0px`;
                this.log('debug', 'Transform origin set on main menu', { 
                    channelKey, 
                    transformOrigin: mainMenu.style.transformOrigin 
                });
            } else {
                this.log('warn', 'Main menu element not found', { channelKey });
            }
            
            // Configure splash screen
            this.log('debug', 'Configuring splash screen...', { channelKey });
            await this.configureSplashScreen(channel, centerX, centerY);
            
            // Apply animation
            this.log('debug', 'Applying channel animation...', { 
                channelKey, 
                animationType: channel.animationType,
                animationDuration: channel.animationDuration 
            });
            this.applyChannelAnimation(channel);
            
            // DO NOT automatically execute channel action - wait for user to click "Start" button
            // The splash screen is now shown and user must manually click "Start" to proceed
            this.log('debug', 'Channel splash screen displayed - waiting for user interaction', { 
                channelKey,
                splashScreenVisible: true,
                requiresManualStart: true
            });
            
            const activationTime = performance.now() - startTime;
            this.log('info', 'Channel activation completed successfully - splash screen shown', {
                channelKey,
                activationTime: `${activationTime.toFixed(2)}ms`,
                status: 'waiting_for_user_input'
            });
            
        } catch (error) {
            this.log('error', 'Error activating channel', {
                channelKey,
                position,
                error: error.message,
                stack: error.stack
            });
        } finally {
            this.isLoading = false;
            this.isActivating = false;
            this.log('debug', 'Channel activation loading and activating states cleared', { channelKey });
        }
    }

    /**
     * Configure splash screen for channel
     */
    async configureSplashScreen(channel, centerX, centerY) {
        this.log('debug', 'Configuring splash screen', {
            channelKey: channel.channelKey,
            centerX,
            centerY,
            splashImagePath: channel.splashImagePath,
            splashBackgroundColor: channel.splashBackgroundColor,
            splashCssClasses: channel.splashCssClasses,
            splashHtmlContent: channel.splashHtmlContent ? 'present' : 'none',
            showSplashBar: channel.showSplashBar
        });

        const splashScreen = document.querySelector('.splash-screen');
        const splashBar = document.querySelector('.splash-bar');
        
        if (!splashScreen) {
            this.log('warn', 'Splash screen element not found', { channelKey: channel.channelKey });
            return;
        }
        
        this.log('debug', 'Found splash screen elements', {
            channelKey: channel.channelKey,
            splashScreen: !!splashScreen,
            splashBar: !!splashBar
        });
        
        // Reset splash screen
        splashScreen.innerHTML = '';
        splashScreen.className = 'splash-screen';
        this.log('debug', 'Reset splash screen', { channelKey: channel.channelKey });
        
        // Set transform origin
        splashScreen.style.transformOrigin = `${centerX}px ${centerY}px 0px`;
        
        // Use cover for all channels - banners should be designed to fit
        splashScreen.style.backgroundSize = 'cover';
        splashScreen.style.backgroundPosition = 'center';
        splashScreen.style.backgroundRepeat = 'no-repeat';
        this.log('debug', 'Set splash screen transform origin and background properties', {
            channelKey: channel.channelKey,
            transformOrigin: splashScreen.style.transformOrigin,
            backgroundSize: splashScreen.style.backgroundSize
        });
        
        // Configure background
        if (channel.splashImagePath) {
            // Check if we're using the default Wii logo
            const isDefaultWiiLogo = channel.splashImagePath.includes('channel-wiilogo.png');
            
            if (isDefaultWiiLogo) {
                // For default Wii logo, use a nice gradient background instead of the image
                const gradientBackground = 'linear-gradient(135deg, #4a90e2 0%, #357abd 50%, #1e5f99 100%)';
                splashScreen.style.setProperty('background', gradientBackground, 'important');
                splashScreen.style.setProperty('background-image', 'none', 'important');
                splashScreen.style.setProperty('background-color', '#4a90e2', 'important');
                
                // Add the Wii logo as a centered element instead of background
                const logoContainer = document.createElement('div');
                logoContainer.className = 'wii-logo-container';
                logoContainer.style.cssText = `
                    position: absolute;
                    top: 50%;
                    left: 50%;
                    transform: translate(-50%, -50%);
                    text-align: center;
                    z-index: 1;
                `;
                
                const logoImg = document.createElement('img');
                logoImg.src = channel.splashImagePath;
                logoImg.alt = 'Wii Logo';
                logoImg.style.cssText = `
                    max-width: 200px;
                    max-height: 120px;
                    opacity: 0.9;
                    filter: drop-shadow(0 2px 8px rgba(0,0,0,0.3));
                `;
                
                logoContainer.appendChild(logoImg);
                splashScreen.appendChild(logoContainer);
                
                this.log('debug', 'Applied Wii logo with gradient background using !important', {
                    channelKey: channel.channelKey,
                    backgroundType: 'gradient',
                    logoPath: channel.splashImagePath
                });
            } else {
                // Use custom splash image as background
                splashScreen.style.setProperty('background-image', `url(${channel.splashImagePath})`, 'important');
                this.log('debug', 'Set splash background image', {
                    channelKey: channel.channelKey,
                    imagePath: channel.splashImagePath
                });
            }
        } else if (channel.splashBackgroundColor) {
            splashScreen.style.setProperty('background-color', channel.splashBackgroundColor, 'important');
            this.log('debug', 'Set splash background color', {
                channelKey: channel.channelKey,
                backgroundColor: channel.splashBackgroundColor
            });
        } else {
            // Default gradient background for channels without specific splash settings
            const defaultGradient = 'linear-gradient(135deg, #4a90e2 0%, #357abd 50%, #1e5f99 100%)';
            splashScreen.style.setProperty('background', defaultGradient, 'important');
            splashScreen.style.setProperty('background-color', '#4a90e2', 'important');
            this.log('debug', 'Applied default gradient background with !important', {
                channelKey: channel.channelKey,
                backgroundType: 'default_gradient'
            });
        }
        
        // Add custom CSS classes
        if (channel.splashCssClasses) {
            const classes = channel.splashCssClasses.split(' ');
            splashScreen.classList.add(...classes);
            this.log('debug', 'Added custom CSS classes to splash screen', {
                channelKey: channel.channelKey,
                classes: classes
            });
        }
        
        // Add custom HTML content
        if (channel.splashHtmlContent) {
            splashScreen.innerHTML = channel.splashHtmlContent;
            this.log('debug', 'Set custom HTML content', {
                channelKey: channel.channelKey,
                contentLength: channel.splashHtmlContent.length
            });
        } else {
            // Create default splash content
            this.log('debug', 'Creating default splash content', { channelKey: channel.channelKey });
            this.createDefaultSplashContent(splashScreen, channel);
        }
        
        // Configure splash bar
        if (channel.showSplashBar && splashBar) {
            if (channel.customSplashBar) {
                splashBar.innerHTML = channel.customSplashBar;
                this.log('debug', 'Set custom splash bar content', {
                    channelKey: channel.channelKey,
                    contentLength: channel.customSplashBar.length
                });
            } else {
                this.log('debug', 'Creating default splash bar', { channelKey: channel.channelKey });
                this.createDefaultSplashBar(splashBar, channel);
            }
        } else if (splashBar) {
            splashBar.style.display = 'none';
            this.log('debug', 'Hidden splash bar', { 
                channelKey: channel.channelKey,
                reason: channel.showSplashBar ? 'splash bar element not found' : 'showSplashBar is false'
            });
        }
        
        // Apply custom styles
        if (channel.splashTextColor) {
            splashScreen.style.color = channel.splashTextColor;
            this.log('debug', 'Set splash text color', {
                channelKey: channel.channelKey,
                textColor: channel.splashTextColor
            });
        }

        this.log('info', 'Splash screen configuration completed', {
            channelKey: channel.channelKey,
            hasBackground: !!(channel.splashImagePath || channel.splashBackgroundColor),
            hasCustomClasses: !!channel.splashCssClasses,
            hasCustomContent: !!channel.splashHtmlContent,
            hasSplashBar: channel.showSplashBar && !!splashBar
        });
    }

    /**
     * Create default splash content
     */
    createDefaultSplashContent(container, channel) {
        this.log('debug', 'Creating default splash content', {
            channelKey: channel.channelKey,
            splashTitle: channel.splashTitle,
            splashSubtitle: channel.splashSubtitle,
            hasSplashImage: !!channel.splashImagePath
        });

        // Skip title/subtitle if using custom splash image (not default Wii logo)
        // Show text only when using default Wii logo or no image at all
        const isDefaultWiiLogo = channel.splashImagePath && channel.splashImagePath.includes('channel-wiilogo.png');
        const hasCustomBanner = channel.splashImagePath && !isDefaultWiiLogo;
        
        if (hasCustomBanner) {
            this.log('debug', 'Skipping title/subtitle - custom banner provided', {
                channelKey: channel.channelKey,
                splashImagePath: channel.splashImagePath
            });
            return;
        }

        if (channel.splashTitle || channel.splashSubtitle) {
            const titleContainer = document.createElement('div');
            titleContainer.className = 'splash-title-container';
            titleContainer.style.cssText = `
                position: absolute;
                bottom: 20%;
                left: 50%;
                transform: translateX(-50%);
                text-align: center;
                color: white;
                text-shadow: 0 2px 4px rgba(0,0,0,0.5);
                z-index: 2;
            `;
            
            if (channel.splashTitle) {
                const title = document.createElement('h1');
                title.className = 'splash-title';
                title.textContent = channel.splashTitle;
                title.style.cssText = `
                    font-size: 2.5em;
                    font-weight: bold;
                    margin: 0 0 10px 0;
                    color: white;
                    text-shadow: 0 2px 6px rgba(0,0,0,0.7);
                `;
                titleContainer.appendChild(title);
                this.log('debug', 'Added splash title', {
                    channelKey: channel.channelKey,
                    title: channel.splashTitle
                });
            }
            
            if (channel.splashSubtitle) {
                const subtitle = document.createElement('p');
                subtitle.className = 'splash-subtitle';
                subtitle.textContent = channel.splashSubtitle;
                subtitle.style.cssText = `
                    font-size: 1.2em;
                    margin: 0;
                    color: rgba(255,255,255,0.9);
                    text-shadow: 0 1px 3px rgba(0,0,0,0.5);
                `;
                titleContainer.appendChild(subtitle);
                this.log('debug', 'Added splash subtitle', {
                    channelKey: channel.channelKey,
                    subtitle: channel.splashSubtitle
                });
            }
            
            container.appendChild(titleContainer);
            this.log('debug', 'Default splash content created successfully', {
                channelKey: channel.channelKey,
                hasTitle: !!channel.splashTitle,
                hasSubtitle: !!channel.splashSubtitle,
                isDefaultWiiLogo: isDefaultWiiLogo
            });
        } else {
            this.log('debug', 'No splash title or subtitle provided, skipping default content', {
                channelKey: channel.channelKey
            });
        }
    }

    /**
     * Create default splash bar
     */
    createDefaultSplashBar(container, channel) {
        this.log('debug', 'Creating default splash bar', {
            channelKey: channel.channelKey,
            splashButtonText: channel.splashButtonText
        });

        const buttonText = channel.splashButtonText || 'Start';
        const barHtml = `
            <div class="splash-buttons">
                <a href="javascript:void(0)" class="btn menu-btn" onmouseover="hover()" onclick="channelManager.returnToMenu()">Wii Menu</a>
                <a href="javascript:void(0)" class="btn" onmouseover="hover()" onclick="channelManager.startChannel()">${buttonText}</a>
            </div>
        `;
        
        container.innerHTML = barHtml;
        
        this.log('debug', 'Default splash bar created successfully', {
            channelKey: channel.channelKey,
            buttonText: buttonText,
            htmlLength: barHtml.length
        });
    }

    /**
     * Apply channel activation animation
     */
    applyChannelAnimation(channel) {
        this.log('debug', 'Applying channel animation', {
            channelKey: channel.channelKey,
            animationType: channel.animationType,
            animationDuration: channel.animationDuration
        });

        const body = document.body;
        const mainMenu = document.querySelector('.main-menu');
        
        // Stop background music
        const music = document.getElementById('bg-music');
        if (music) {
            music.pause();
            this.log('debug', 'Background music paused', { channelKey: channel.channelKey });
        } else {
            this.log('debug', 'Background music element not found', { channelKey: channel.channelKey });
        }
        
        // Add animation classes
        if (mainMenu) {
            mainMenu.classList.add('channel-splash');
            this.log('debug', 'Added channel-splash class to main menu', { channelKey: channel.channelKey });
        } else {
            this.log('warn', 'Main menu element not found for animation', { channelKey: channel.channelKey });
        }
        
        if (body) {
            body.classList.add('channel-splash');
            this.log('debug', 'Added channel-splash class to body', { channelKey: channel.channelKey });
        }
        
        // Remove splash-switch class after animation
        const animationDuration = channel.animationDuration || this.config.animationDefaults.duration;
        setTimeout(() => {
            if (body) {
                body.classList.remove('splash-switch');
                this.log('debug', 'Removed splash-switch class from body after animation', {
                    channelKey: channel.channelKey,
                    delay: animationDuration
                });
            }
        }, animationDuration);

        this.log('info', 'Channel animation applied successfully', {
            channelKey: channel.channelKey,
            animationDuration: animationDuration,
            classesAdded: ['channel-splash']
        });
    }

    /**
     * Execute channel action
     */
    executeChannelAction(channel) {
        this.log('info', 'Executing channel action', {
            channelKey: channel.channelKey,
            actionType: channel.actionType,
            actionUrl: channel.actionUrl,
            hasCustomJavaScript: !!channel.customJavaScript
        });

        if (!channel.actionUrl && !channel.customJavaScript) {
            this.log('warn', 'No action URL or custom JavaScript provided', { channelKey: channel.channelKey });
            return;
        }
        
        switch (channel.actionType.toLowerCase()) {
            case 'url':
                this.log('debug', 'Executing URL action', {
                    channelKey: channel.channelKey,
                    url: channel.actionUrl
                });
                window.location.href = channel.actionUrl;
                break;
                
            case 'view':
                this.log('debug', 'Executing view action', {
                    channelKey: channel.channelKey,
                    viewName: channel.actionUrl
                });
                if (typeof changeView === 'function') {
                    changeView(channel.actionUrl, 'fade');
                    this.log('debug', 'View change function called', {
                        channelKey: channel.channelKey,
                        viewName: channel.actionUrl,
                        transition: 'fade'
                    });
                } else {
                    this.log('error', 'changeView function not available', { channelKey: channel.channelKey });
                }
                break;
                
            case 'javascript':
                this.log('debug', 'Executing JavaScript action', {
                    channelKey: channel.channelKey,
                    scriptLength: channel.customJavaScript?.length || 0
                });
                if (channel.customJavaScript) {
                    try {
                        eval(channel.customJavaScript);
                        this.log('debug', 'Custom JavaScript executed successfully', {
                            channelKey: channel.channelKey
                        });
                    } catch (error) {
                        this.log('error', 'Error executing custom JavaScript', {
                            channelKey: channel.channelKey,
                            error: error.message,
                            stack: error.stack,
                            script: channel.customJavaScript
                        });
                    }
                } else {
                    this.log('warn', 'No custom JavaScript provided for JavaScript action', {
                        channelKey: channel.channelKey
                    });
                }
                break;
                
            case 'api':
                this.log('debug', 'Executing API action', {
                    channelKey: channel.channelKey,
                    apiUrl: channel.actionUrl
                });
                fetch(channel.actionUrl, { method: 'POST' })
                    .then(response => {
                        this.log('debug', 'API response received', {
                            channelKey: channel.channelKey,
                            status: response.status,
                            statusText: response.statusText
                        });
                        return response.json();
                    })
                    .then(data => {
                        this.log('info', 'API action completed successfully', {
                            channelKey: channel.channelKey,
                            responseData: data
                        });
                    })
                    .catch(error => {
                        this.log('error', 'API action failed', {
                            channelKey: channel.channelKey,
                            error: error.message,
                            url: channel.actionUrl
                        });
                    });
                break;
                
            default:
                this.log('warn', 'Unknown action type', {
                    channelKey: channel.channelKey,
                    actionType: channel.actionType,
                    availableTypes: ['url', 'view', 'javascript', 'api']
                });
        }
    }

    /**
     * Return to main menu (called when user clicks "Wii Menu" button)
     */
    returnToMenu() {
        this.log('info', 'Returning to main menu', {
            currentChannel: this.currentChannel?.channelKey || null,
            currentView: typeof currentView !== 'undefined' ? currentView : 'unknown'
        });

        this.playSound('back');
        
        const mainMenu = document.querySelector('.main-menu');
        const body = document.body;
        
        // Reset visual states
        if (mainMenu) {
            mainMenu.classList.remove('channel-splash');
            this.log('debug', 'Removed channel-splash class from main menu');
        } else {
            this.log('warn', 'Main menu element not found during return to menu');
        }
        
        if (body) {
            body.classList.remove('channel-splash');
            body.classList.add('splash-switch');
            this.log('debug', 'Updated body classes for menu return animation');
        }
        
        // Clear splash screen content and reset background
        const splashScreen = document.querySelector('.splash-screen');
        if (splashScreen) {
            splashScreen.innerHTML = '';
            splashScreen.style.removeProperty('background');
            splashScreen.style.removeProperty('background-image');
            splashScreen.style.removeProperty('background-color');
            this.log('debug', 'Cleared splash screen content and background');
        }
        
        // Reset channel manager state
        this.currentChannel = null;
        this.isLoading = false;
        this.isActivating = false;
        
        // Reset animation classes after delay
        setTimeout(() => {
            if (body) {
                body.classList.remove('splash-switch');
                this.log('debug', 'Removed splash-switch class after return animation');
            }
            
            // Ensure we're back to menu view and refresh channels if needed
            if (typeof changeView === 'function') {
                changeView('menu', 'fade');
                this.log('debug', 'Changed view back to menu');
                
                // Small delay to ensure view is loaded, then validate channels
                setTimeout(() => {
                    if (!this.validateChannelsDisplay()) {
                        this.log('info', 'Channels not properly displayed after return - re-rendering');
                        this.renderChannels();
                    }
                }, 200);
            }
        }, 900);
        
        this.log('info', 'Successfully initiated return to main menu', {
            stateReset: true,
            currentChannelCleared: true,
            loadingStatesCleared: true
        });
    }

    /**
     * Start current channel (called when user clicks "Start" button)
     */
    startChannel() {
        if (this.currentChannel) {
            this.log('info', 'User manually starting current channel', {
                channelKey: this.currentChannel.channelKey,
                actionType: this.currentChannel.actionType,
                actionUrl: this.currentChannel.actionUrl,
                userInitiated: true
            });
            
            this.playSound('select');
            this.executeChannelAction(this.currentChannel);
        } else {
            this.log('warn', 'No current channel to start - user clicked Start but no channel is active');
        }
    }

    /**
     * Play sound effect
     */
    playSound(soundName) {
        this.log('debug', 'Playing sound effect', { soundName });
        
        const audio = document.getElementById(soundName);
        if (audio) {
            audio.play()
                .then(() => {
                    this.log('debug', 'Sound played successfully', { soundName });
                })
                .catch(error => {
                    this.log('warn', 'Could not play sound', {
                        soundName,
                        error: error.message
                    });
                });
        } else {
            this.log('warn', 'Sound element not found', { 
                soundName,
                availableAudioElements: Array.from(document.querySelectorAll('audio')).map(a => a.id).filter(id => id)
            });
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        this.log('debug', 'Binding event listeners...');
        
        // Listen for channel updates
        document.addEventListener('channelUpdated', (event) => {
            this.log('info', 'Channel updated event received', {
                channelData: event.detail
            });
            this.handleChannelUpdate(event.detail);
        });
        
        // Listen for configuration changes
        document.addEventListener('configurationUpdated', () => {
            this.log('info', 'Configuration updated event received');
            // Only refresh if we're on the menu view
            if (typeof currentView !== 'undefined' && currentView === 'menu') {
                this.refreshChannels();
            } else {
                this.log('debug', 'Skipping config refresh - not on menu view', { currentView });
            }
        });

        // Listen for page visibility changes to refresh channels when page becomes visible
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden) {
                this.log('info', 'Page became visible - validating channel display');
                setTimeout(() => {
                    // Only validate if we're on the menu view
                    if (typeof currentView !== 'undefined' && currentView === 'menu') {
                        if (!this.validateChannelsDisplay()) {
                            this.log('info', 'Channel display validation failed - auto-fixing');
                            this.autoFixBlankChannels();
                        }
                    } else {
                        this.log('debug', 'Skipping validation - not on menu view', { currentView });
                    }
                }, 200); // Small delay to ensure DOM is ready
            }
        });

        // Listen for focus events to refresh stale channels
        window.addEventListener('focus', () => {
            this.log('debug', 'Window gained focus - checking channel freshness');
            
            // Only refresh if we're on the menu view
            if (typeof currentView !== 'undefined' && currentView !== 'menu') {
                this.log('debug', 'Skipping focus refresh - not on menu view', { currentView });
                return;
            }
            
            const lastRefresh = this.cache.get('active_channels')?.timestamp || 0;
            const timeSinceRefresh = Date.now() - lastRefresh;
            
            // Refresh if it's been more than 2 minutes since last refresh
            if (timeSinceRefresh > 2 * 60 * 1000) {
                this.log('info', 'Channels are stale - refreshing on window focus', {
                    timeSinceRefresh: `${timeSinceRefresh}ms`,
                    threshold: '2 minutes'
                });
                this.refreshChannels();
            }
        });

        this.log('debug', 'Event listeners bound successfully', {
            events: ['channelUpdated', 'configurationUpdated', 'visibilitychange', 'focus']
        });
    }

    /**
     * Handle channel update
     */
    handleChannelUpdate(channelData) {
        this.log('info', 'Handling channel update', {
            channelKey: channelData.channelKey,
            name: channelData.name,
            position: channelData.position,
            isActive: channelData.isActive
        });

        const index = this.channels.findIndex(c => c.channelKey === channelData.channelKey);
        if (index !== -1) {
            this.channels[index] = channelData;
            this.log('debug', 'Updated existing channel', {
                channelKey: channelData.channelKey,
                index: index
            });
        } else {
            this.channels.push(channelData);
            this.log('debug', 'Added new channel', {
                channelKey: channelData.channelKey,
                newChannelCount: this.channels.length
            });
        }
        
        this.clearCache();
        this.renderChannels();
        
        this.log('info', 'Channel update handled successfully', {
            channelKey: channelData.channelKey,
            totalChannels: this.channels.length
        });
    }

    /**
     * Refresh channels from server
     */
    async refreshChannels() {
        // Don't refresh if we're not on the menu view
        if (typeof currentView !== 'undefined' && currentView !== 'menu') {
            this.log('debug', 'Skipping channel refresh - not on menu view', { currentView });
            return;
        }
        
        this.log('info', 'Refreshing channels from server...');
        
        try {
            const startTime = performance.now();
            
            this.clearCache();
            await this.loadChannelConfiguration();
            await this.loadActiveChannels(true); // Force refresh from server
            
            // Only render if we're still on menu view
            if (typeof currentView !== 'undefined' && currentView === 'menu') {
                this.renderChannels();
            } else {
                this.log('debug', 'Skipping channel rendering - view changed during refresh', { currentView });
            }
            
            const refreshTime = performance.now() - startTime;
            this.log('info', 'Channels refreshed successfully', {
                refreshTime: `${refreshTime.toFixed(2)}ms`,
                channelCount: this.channels.length,
                configKeys: Object.keys(this.channelConfig)
            });
        } catch (error) {
            this.log('error', 'Failed to refresh channels', {
                error: error.message,
                stack: error.stack
            });
        }
    }

    /**
     * Cache management
     */
    setCache(key, data) {
        this.cache.set(key, {
            data: data,
            timestamp: Date.now()
        });
        
        this.log('debug', 'Data cached', {
            key: key,
            dataSize: JSON.stringify(data).length,
            cacheSize: this.cache.size,
            timestamp: new Date().toISOString()
        });
    }

    getFromCache(key) {
        const cached = this.cache.get(key);
        if (!cached) {
            this.log('debug', 'Cache miss', { key: key });
            return null;
        }
        
        const age = Date.now() - cached.timestamp;
        if (age > this.config.cacheTimeout) {
            this.cache.delete(key);
            this.log('debug', 'Cache expired and removed', {
                key: key,
                age: `${age}ms`,
                timeout: `${this.config.cacheTimeout}ms`
            });
            return null;
        }
        
        this.log('debug', 'Cache hit', {
            key: key,
            age: `${age}ms`,
            dataSize: JSON.stringify(cached.data).length
        });
        
        return cached.data;
    }

    clearCache() {
        const oldSize = this.cache.size;
        this.cache.clear();
        this.log('debug', 'Cache cleared', {
            previousSize: oldSize,
            currentSize: this.cache.size
        });
    }

    /**
     * Render fallback channels with proper Wii styling
     */
    renderFallbackChannels() {
        this.log('warn', 'Rendering fallback channels with Wii styling', {
            reason: 'Failed to load channels from server'
        });
        
        this.channels = this.getDefaultChannels();
        this.log('info', 'Default channels loaded for fallback', {
            channelCount: this.channels.length,
            channels: this.channels.map(c => ({ key: c.channelKey, name: c.name, position: c.position }))
        });
        
        this.renderChannels();
    }

    /**
     * Render loading state with authentic Wii grid structure
     */
    renderLoadingState() {
        this.log('debug', 'Rendering loading state with authentic Wii grid structure');
        
        const channelsContainer = document.querySelector('.channels');
        if (!channelsContainer) {
            this.log('error', 'Channels container not found for loading state');
            return;
        }

        // Clear existing content
        channelsContainer.innerHTML = '';
        this.log('debug', 'Cleared existing channel content');

        // Create the authentic Wii grid structure even during loading
        const cols = [];
        for (let i = 0; i < this.config.positions.gridCols; i++) {
            const col = document.createElement('div');
            col.className = i === 0 ? 'col first' : 'col';
            cols.push(col);
        }
        
        this.log('debug', 'Created grid columns for loading state', {
            columnCount: cols.length,
            gridCols: this.config.positions.gridCols
        });

        // Fill all 12 positions with blank channels during loading
        for (let position = 1; position <= this.config.positions.maxChannels; position++) {
            const colIndex = Math.floor((position - 1) / this.config.positions.gridRows);
            
            if (colIndex < cols.length) {
                const channelDiv = document.createElement('div');
                channelDiv.className = 'channel-icon blank loading';
                
                const hoverDiv = document.createElement('div');
                hoverDiv.className = 'hover';
                hoverDiv.id = position.toString();
                
                channelDiv.appendChild(hoverDiv);
                cols[colIndex].appendChild(channelDiv);
            }
        }

        // Add columns to container
        cols.forEach(col => channelsContainer.appendChild(col));
        
        this.log('info', 'Loading state rendered successfully', {
            totalPositions: this.config.positions.maxChannels,
            gridStructure: `${this.config.positions.gridCols}x${this.config.positions.gridRows}`
        });
    }

    /**
     * Get default configuration
     */
    getDefaultConfiguration() {
        return {
            channels: this.getDefaultChannels(),
            lastUpdated: new Date().toISOString()
        };
    }

    /**
     * Find channels container with fallback selectors
     */
    findChannelsContainer() {
        let channelsContainer = document.querySelector('.channels');
        if (!channelsContainer) {
            channelsContainer = document.querySelector('#dynamic-channels');
        }
        if (!channelsContainer) {
            channelsContainer = document.querySelector('.main-menu .channels');
        }
        return channelsContainer;
    }

    /**
     * Check if channels are properly loaded and visible
     */
    validateChannelsDisplay() {
        this.log('debug', 'Validating channels display...');
        
        const channelsContainer = document.querySelector('.channels');
        if (!channelsContainer) {
            this.log('warn', 'Channels container not found during validation');
            return false;
        }
        
        const occupiedChannels = channelsContainer.querySelectorAll('.occupied');
        const blankChannels = channelsContainer.querySelectorAll('.blank');
        const loadingChannels = channelsContainer.querySelectorAll('.loading');
        
        const validation = {
            hasContainer: !!channelsContainer,
            occupiedCount: occupiedChannels.length,
            blankCount: blankChannels.length,
            loadingCount: loadingChannels.length,
            totalChannels: this.channels.length,
            activeChannels: this.channels.filter(c => c.isActive).length
        };
        
        this.log('debug', 'Channel display validation results', validation);
        
        // Check if we have active channels but no occupied display elements
        const hasActiveChannels = validation.activeChannels > 0;
        const hasOccupiedDisplay = validation.occupiedCount > 0;
        const isStillLoading = validation.loadingCount > 0;
        
        if (hasActiveChannels && !hasOccupiedDisplay && !isStillLoading) {
            this.log('warn', 'Detected mismatch: have active channels but no occupied display elements', {
                activeChannels: validation.activeChannels,
                occupiedDisplay: validation.occupiedCount,
                recommendation: 'Should re-render channels'
            });
            return false;
        }
        
        this.log('info', 'Channel display validation passed', validation);
        return true;
    }

    /**
     * Auto-fix blank channels by re-rendering
     */
    async autoFixBlankChannels() {
        this.log('info', 'Auto-fixing blank channels...');
        
        try {
            // First try to re-render with existing data
            if (this.channels.length > 0) {
                this.log('debug', 'Re-rendering channels with existing data');
                this.renderChannels();
                
                // Validate after re-render
                setTimeout(() => {
                    if (!this.validateChannelsDisplay()) {
                        this.log('warn', 'Re-render failed, forcing refresh from server');
                        this.refreshChannels();
                    }
                }, 100);
            } else {
                this.log('debug', 'No channel data available, refreshing from server');
                await this.refreshChannels();
            }
        } catch (error) {
            this.log('error', 'Failed to auto-fix blank channels', {
                error: error.message,
                stack: error.stack
            });
            this.renderFallbackChannels();
        }
    }aultConfiguration() {
        return {
            channels: this.getDefaultChannels(),
            lastUpdated: new Date().toISOString()
        };
    }

    /**
     * Check if channels are properly loaded and visible
     */
    validateChannelsDisplay() {
        this.log('debug', 'Validating channels display...');
        
        const channelsContainer = document.querySelector('.channels');
        if (!channelsContainer) {
            this.log('warn', 'Channels container not found during validation');
            return false;
        }
        
        const occupiedChannels = channelsContainer.querySelectorAll('.occupied');
        const blankChannels = channelsContainer.querySelectorAll('.blank');
        const loadingChannels = channelsContainer.querySelectorAll('.loading');
        
        const validation = {
            hasContainer: !!channelsContainer,
            occupiedCount: occupiedChannels.length,
            blankCount: blankChannels.length,
            loadingCount: loadingChannels.length,
            totalChannels: this.channels.length,
            activeChannels: this.channels.filter(c => c.isActive).length
        };
        
        this.log('debug', 'Channel display validation results', validation);
        
        // Check if we have active channels but no occupied display elements
        const hasActiveChannels = validation.activeChannels > 0;
        const hasOccupiedDisplay = validation.occupiedCount > 0;
        const isStillLoading = validation.loadingCount > 0;
        
        if (hasActiveChannels && !hasOccupiedDisplay && !isStillLoading) {
            this.log('warn', 'Detected mismatch: have active channels but no occupied display elements', {
                activeChannels: validation.activeChannels,
                occupiedDisplay: validation.occupiedCount,
                recommendation: 'Should re-render channels'
            });
            return false;
        }
        
        this.log('info', 'Channel display validation passed', validation);
        return true;
    }

    /**
     * Auto-fix blank channels by re-rendering
     */
    async autoFixBlankChannels() {
        this.log('info', 'Auto-fixing blank channels...');
        
        try {
            // First try to re-render with existing data
            if (this.channels.length > 0) {
                this.log('debug', 'Re-rendering channels with existing data');
                this.renderChannels();
                
                // Validate after re-render
                setTimeout(() => {
                    if (!this.validateChannelsDisplay()) {
                        this.log('warn', 'Re-render failed, forcing refresh from server');
                        this.refreshChannels();
                    }
                }, 100);
            } else {
                this.log('debug', 'No channel data available, refreshing from server');
                await this.refreshChannels();
            }
        } catch (error) {
            this.log('error', 'Failed to auto-fix blank channels', {
                error: error.message,
                stack: error.stack
            });
            this.renderFallbackChannels();
        }
    }
}

// Initialize channel manager when DOM is ready
let channelManager;

document.addEventListener('DOMContentLoaded', () => {
    channelManager = new WiiChannelManager();
    // Make it available globally immediately
    window.channelManager = channelManager;
});

// Add periodic validation methods to the prototype
WiiChannelManager.prototype.setupPeriodicValidation = function() {
    this.log('debug', 'Setting up periodic channel validation...');
    
    // Check every 30 seconds if channels are properly displayed
    this.validationInterval = setInterval(() => {
        // Only validate if we're on the menu view
        if (typeof currentView !== 'undefined' && currentView === 'menu') {
            if (!this.validateChannelsDisplay()) {
                this.log('warn', 'Periodic validation detected blank channels - auto-fixing');
                this.autoFixBlankChannels();
            }
        }
    }, 30000); // 30 seconds
    
    this.log('debug', 'Periodic validation set up successfully', {
        interval: '30 seconds',
        validationId: this.validationInterval
    });
};

WiiChannelManager.prototype.cleanup = function() {
    if (this.validationInterval) {
        clearInterval(this.validationInterval);
        this.log('debug', 'Periodic validation cleaned up');
    }
};

// Add global helper functions for debugging
window.refreshWiiChannels = function() {
    if (window.channelManager) {
        console.log('Manually refreshing Wii channels...');
        return window.channelManager.refreshChannels();
    } else {
        console.warn('Channel manager not available');
    }
};

window.validateWiiChannels = function() {
    if (window.channelManager) {
        console.log('Validating Wii channels display...');
        return window.channelManager.validateChannelsDisplay();
    } else {
        console.warn('Channel manager not available');
    }
};

window.fixBlankWiiChannels = function() {
    if (window.channelManager) {
        console.log('Auto-fixing blank Wii channels...');
        return window.channelManager.autoFixBlankChannels();
    } else {
        console.warn('Channel manager not available');
    }
};

window.debugChannelState = function() {
    if (window.channelManager) {
        console.log('=== CHANNEL MANAGER DEBUG STATE ===');
        console.log('isLoading:', window.channelManager.isLoading);
        console.log('isActivating:', window.channelManager.isActivating);
        console.log('currentChannel:', window.channelManager.currentChannel?.channelKey || null);
        console.log('lastActivation:', window.channelManager.lastActivation);
        console.log('timeSinceLastActivation:', Date.now() - (window.channelManager.lastActivation || 0), 'ms');
        console.log('channels count:', window.channelManager.channels.length);
        console.log('active channels:', window.channelManager.channels.filter(c => c.isActive).length);
        
        // Check for duplicate event listeners
        const hoverElements = document.querySelectorAll('.occupied .hover');
        console.log('Hover elements found:', hoverElements.length);
        hoverElements.forEach((el, index) => {
            console.log(`Element ${index}:`, {
                channelKey: el.getAttribute('data-channel-key'),
                position: el.getAttribute('data-position'),
                hasOnclick: !!el.onclick,
                eventListeners: getEventListeners ? getEventListeners(el) : 'DevTools required'
            });
        });
        
        return {
            isLoading: window.channelManager.isLoading,
            isActivating: window.channelManager.isActivating,
            currentChannel: window.channelManager.currentChannel?.channelKey || null,
            channelsCount: window.channelManager.channels.length,
            activeChannelsCount: window.channelManager.channels.filter(c => c.isActive).length,
            hoverElementsCount: hoverElements.length
        };
    } else {
        console.warn('Channel manager not available');
        return null;
    }
};

window.forceReturnToMenu = function() {
    if (window.channelManager) {
        console.log('Forcing return to main menu...');
        window.channelManager.returnToMenu();
        return true;
    } else {
        console.warn('Channel manager not available');
        return false;
    }
};

window.clearChannelSplashState = function() {
    console.log('Clearing all channel splash state...');
    
    const mainMenu = document.querySelector('.main-menu');
    const body = document.body;
    const splashScreen = document.querySelector('.splash-screen');
    
    if (mainMenu) {
        mainMenu.classList.remove('channel-splash');
        console.log('Cleared channel-splash class from main menu');
    }
    
    if (body) {
        body.classList.remove('channel-splash', 'splash-switch');
        console.log('Cleared channel splash classes from body');
    }
    
    if (splashScreen) {
        splashScreen.innerHTML = '';
        splashScreen.style.removeProperty('background');
        splashScreen.style.removeProperty('background-image');
        splashScreen.style.removeProperty('background-color');
        console.log('Cleared splash screen content and styles');
    }
    
    // Reset channel manager state if available
    if (window.channelManager) {
        window.channelManager.currentChannel = null;
        window.channelManager.isLoading = false;
        window.channelManager.isActivating = false;
        console.log('Reset channel manager state');
    }
    
    return true;
};

// Export for global access
window.channelManager = channelManager;