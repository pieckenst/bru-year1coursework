/* WebTV HD Loading Panel System - Modular Component */

/**
 * WebTVLoadingPanel - Manages loading and reconnect panels
 * Handles page loading indicators and connection status
 */
class WebTVLoadingPanel extends EventTarget {
	constructor(config = {}) {
		super();

		// DOM elements - Loading Panel
		this.loadingPanel = document.getElementById('loadingPanel');
		this.loadingMessage = document.getElementById('loadingMessage');
		this.loadingProgress = document.getElementById('loadingProgress');
		this.loadingIndicator = document.querySelector('.status-indicator');

		// DOM elements - Reconnect Panel
		this.reconnectPanel = document.getElementById('reconnectPanel');
		this.reconnectButton = document.getElementById('reconnectButton');
		this.reconnectLogo = document.getElementById('reconnectLogo');

		// State
		this.isLoading = false;
		this.isDisconnected = false;
		this.currentState = 'idle';

		// Configuration
		this.config = {
			debug: config.debug || false,
			titleCheckInterval: config.titleCheckInterval || 10,
			reconnectHighlightDelay: config.reconnectHighlightDelay || 500,
			messages: config.messages || {
				contacting: 'Contacting service',
				getting: 'Getting page',
				loading: 'Loading',
				default: 'Loading WebTV HD...'
			}
		};

		// Verify DOM elements exist
		if (!this.loadingPanel || !this.loadingMessage || !this.loadingIndicator) {
			console.error('WebTVLoadingPanel: Required loading panel elements not found');
		}
		if (!this.reconnectPanel || !this.reconnectButton) {
			console.error('WebTVLoadingPanel: Required reconnect panel elements not found');
		}

		this.log('Loading panel system initialized');
	}

	/**
	 * Debug logging
	 */
	log(...args) {
		if (this.config.debug) {
			console.debug('[LoadingPanel]', ...args);
		}
	}

	/**
	 * Start loading - show loading panel and indicator
	 * @param {Object} options - Loading options
	 * @param {string} options.message - Custom loading message
	 * @param {HTMLIFrameElement} options.iframe - Iframe to monitor
	 */
	startLoading(options = {}) {
		const { message, iframe } = options;

		this.log('startLoading called');
		this.isLoading = true;
		this.currentState = 'contacting';

		// Show loading panel
		this.loadingPanel.style.visibility = 'visible';
		this.loadingPanel.removeAttribute('aria-hidden');

		// Set initial message
		this.setMessage(message || this.config.messages.contacting);

		// Add loading class to status indicator
		if (this.loadingIndicator) {
			this.loadingIndicator.classList.add('loading');
		}

		// Monitor iframe title if provided
		if (iframe) {
			this.monitorIframeTitle(iframe);
		}

		// Emit event
		this.dispatchEvent(new CustomEvent('loadingStart', {
			detail: { message, state: this.currentState }
		}));
	}

	/**
	 * Stop loading - hide loading panel and indicator
	 */
	stopLoading() {
		this.log('stopLoading called');
		this.isLoading = false;
		this.currentState = 'complete';

		// Hide loading panel
		this.loadingPanel.setAttribute('aria-hidden', 'true');
		this.loadingPanel.style.visibility = 'hidden';

		// Reset message
		this.setMessage('');

		// Remove loading class from status indicator
		if (this.loadingIndicator) {
			this.loadingIndicator.classList.remove('loading');
		}

		// Emit event
		this.dispatchEvent(new CustomEvent('loadingStop'));
	}

	/**
	 * Set loading message
	 * @param {string} message - Message to display
	 */
	setMessage(message) {
		if (this.loadingMessage) {
			this.loadingMessage.textContent = message;
			this.log('Message set:', message);
		}
	}

	/**
	 * Monitor iframe title and update loading message
	 * @param {HTMLIFrameElement} iframe - Iframe to monitor
	 */
	monitorIframeTitle(iframe) {
		const checkTitle = () => {
			if (!this.isLoading) return;

			try {
				// Check if iframe document is accessible
				if (iframe.contentDocument === null) {
					this.currentState = 'getting';
					this.setMessage(this.config.messages.getting);
					return;
				}

				// Get iframe title
				const iframeTitle = iframe.contentDocument.title;
				if (iframeTitle) {
					this.currentState = 'loading';
					this.setMessage(iframeTitle);
				} else {
					// Try again if no title yet
					setTimeout(checkTitle, this.config.titleCheckInterval);
				}
			} catch (error) {
				// Cross-origin iframe - can't access title
				this.log('Cannot access iframe title (cross-origin):', error.message);
				this.setMessage(this.config.messages.loading);
			}
		};

		checkTitle();
	}

	/**
	 * Set progress bar value
	 * @param {number} value - Progress value (0-100)
	 */
	setProgress(value) {
		if (this.loadingProgress) {
			this.loadingProgress.value = Math.max(0, Math.min(100, value));
		}
	}

	/**
	 * Show reconnect panel (hang up simulation)
	 */
	showReconnectPanel() {
		this.log('Showing reconnect panel');
		this.isDisconnected = true;

		// Show reconnect panel
		this.reconnectPanel.style.display = 'flex';
		this.reconnectPanel.showModal();

		// Enable button
		if (this.reconnectButton) {
			this.reconnectButton.classList.remove('noselect');
		}

		// Add disconnected class to status indicator
		if (this.loadingIndicator) {
			this.loadingIndicator.classList.add('disconnected');
		}

		// Highlight button after delay
		setTimeout(() => {
			if (typeof highlightNoScroll === 'function' && this.reconnectButton) {
				highlightNoScroll(this.reconnectButton);
			}
		}, this.config.reconnectHighlightDelay);

		// Emit event
		this.dispatchEvent(new CustomEvent('disconnect'));
	}

	/**
	 * Hide reconnect panel (reconnect simulation)
	 * @param {boolean} playSound - Whether to play modem sound
	 */
	hideReconnectPanel(playSound = true) {
		this.log('Hiding reconnect panel');
		this.isDisconnected = false;

		// Play modem sound if requested
		if (playSound && typeof window.playSound === 'function' && typeof window.modem !== 'undefined') {
			window.playSound(window.modem);
		}

		// Reset selection box if function exists
		if (typeof resetSelectionBox === 'function') {
			resetSelectionBox();
		}

		// Hide reconnect panel
		this.reconnectPanel.style.display = 'none';
		this.reconnectPanel.close();

		// Disable button
		if (this.reconnectButton) {
			this.reconnectButton.classList.add('noselect');
		}

		// Remove disconnected class from status indicator
		if (this.loadingIndicator) {
			this.loadingIndicator.classList.remove('disconnected');
		}

		// Check background music status if function exists
		if (typeof checkBGMusicStatus === 'function') {
			checkBGMusicStatus();
		}

		// Emit event
		this.dispatchEvent(new CustomEvent('reconnect'));
	}

	/**
	 * Set reconnect button text
	 * @param {string} text - Button text
	 */
	setReconnectButtonText(text) {
		if (this.reconnectButton) {
			this.reconnectButton.textContent = text;
		}
	}

	/**
	 * Check if currently loading
	 * @returns {boolean}
	 */
	isCurrentlyLoading() {
		return this.isLoading;
	}

	/**
	 * Check if currently disconnected
	 * @returns {boolean}
	 */
	isCurrentlyDisconnected() {
		return this.isDisconnected;
	}

	/**
	 * Get current loading state
	 * @returns {string}
	 */
	getCurrentState() {
		return this.currentState;
	}
}

// Initialize global instance
const webTVLoadingPanel = new WebTVLoadingPanel({
	debug: false // Set to true for debug logging
});

// Expose global functions for backward compatibility
window.startLoading = function(options) {
	// Support both old and new API
	if (typeof options === 'object') {
		webTVLoadingPanel.startLoading(options);
	} else {
		// Old API: startLoading() with no arguments
		webTVLoadingPanel.startLoading({
			iframe: typeof iframe !== 'undefined' ? iframe : null
		});
	}
};

window.stopLoading = function() {
	webTVLoadingPanel.stopLoading();
};

window.hangUp = function() {
	// Hide options bar if function exists
	if (typeof hideOptionsBarNoSound === 'function') {
		hideOptionsBarNoSound();
	}

	// Reset selection box if function exists
	if (typeof resetSelectionBox === 'function') {
		resetSelectionBox();
	}

	// Stop background music if function exists
	if (typeof stopBGMusic === 'function') {
		stopBGMusic();
	}

	// Close panel if open
	if (typeof panel !== 'undefined' && typeof closePanel === 'function') {
		if (panel.classList.contains('show') || panel.classList.contains('showing')) {
			closePanel();
		}
	}

	webTVLoadingPanel.showReconnectPanel();
};

window.reconnect = function() {
	webTVLoadingPanel.hideReconnectPanel(true);
};

// Auto-attach to iframe load event if iframe exists
if (typeof iframe !== 'undefined') {
	iframe.addEventListener('load', function() {
		webTVLoadingPanel.stopLoading();
	});
}

// Expose the class instance for advanced usage
window.webTVLoadingPanel = webTVLoadingPanel;

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
	module.exports = { WebTVLoadingPanel, webTVLoadingPanel };
}
