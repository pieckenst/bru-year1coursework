/* WebTV HD Loading Panel System - Configuration */

/**
 * Loading panel configuration
 */
const LOADING_CONFIG = {
	// Loading messages
	messages: {
		contacting: 'Contacting service',
		getting: 'Getting page',
		loading: 'Loading',
		default: 'Loading WebTV HD...'
	},

	// Assets
	assets: {
		globe: 'images/globe.gif',
		webtvLogo: 'images/WebTVShadowInset.svg'
	},

	// Timing
	timing: {
		titleCheckInterval: 10, // milliseconds
		reconnectHighlightDelay: 500 // milliseconds
	},

	// Progress bar settings
	progress: {
		defaultValue: 100,
		maxValue: 100
	},

	// Debug mode
	debug: false
};

/**
 * Reconnect panel configuration
 */
const RECONNECT_CONFIG = {
	// Button text
	buttonText: {
		default: 'Reconnect',
		knockoff: 'un-hang up'
	},

	// Assets
	assets: {
		logo: 'images/WebTVShadowInset.svg',
		modemSound: 'audio/modem.mp3'
	},

	// Status indicator classes
	statusClasses: {
		disconnected: 'disconnected',
		loading: 'loading'
	}
};

/**
 * Loading states
 */
const LOADING_STATES = {
	IDLE: 'idle',
	CONTACTING: 'contacting',
	GETTING: 'getting',
	LOADING: 'loading',
	COMPLETE: 'complete'
};

// Export for use in loading-panel-system.js
if (typeof module !== 'undefined' && module.exports) {
	module.exports = {
		LOADING_CONFIG,
		RECONNECT_CONFIG,
		LOADING_STATES
	};
}
