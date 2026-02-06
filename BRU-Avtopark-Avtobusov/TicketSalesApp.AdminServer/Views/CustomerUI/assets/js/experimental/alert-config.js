/* WebTV HD Alert Dialog System - Configuration */

/**
 * Alert preset configurations
 * Each preset defines default icon, sound, and button text
 */
const ALERT_PRESETS = {
	// JavaScript-style alert with warning triangle
	jsAlert: {
		icon: 'images/JSAlert.svg',
		sound: 'audio/error.mp3',
		buttonText: 'OK',
		description: 'Standard JavaScript alert with warning icon'
	},

	// WebTV service-style alert with logo
	serviceAlert: {
		icon: 'images/WebTVShadowInset.svg',
		sound: 'audio/error.mp3',
		buttonText: 'Continue',
		description: 'WebTV service alert with logo'
	},

	// SurfWatch blocked content alert
	surfWatch: {
		icon: 'images/SurfWatch.svg',
		sound: 'audio/error.mp3',
		buttonText: 'Continue',
		description: 'SurfWatch content blocking alert'
	},

	// Error alert with alternative sound
	error: {
		icon: 'images/JSAlert.svg',
		sound: 'audio/doh.mp3',
		buttonText: 'OK',
		description: 'Error alert with "doh" sound'
	},

	// Custom alert (user-defined)
	custom: {
		icon: null,
		sound: null,
		buttonText: 'Continue',
		description: 'Custom alert with user-defined properties'
	}
};

/**
 * Default alert configuration
 */
const ALERT_DEFAULTS = {
	icon: 'images/WebTVShadowInset.svg',
	sound: 'audio/error.mp3',
	buttonText: 'Continue',
	animationDelay: 2, // milliseconds
	closeDelay: 2 // milliseconds
};

/**
 * Alert sound paths
 */
const ALERT_SOUNDS = {
	error: 'audio/error.mp3',
	doh: 'audio/doh.mp3',
	bonk: 'audio/bonk.mp3'
};

/**
 * Alert icon paths
 */
const ALERT_ICONS = {
	jsAlert: 'images/JSAlert.svg',
	webtvLogo: 'images/WebTVShadowInset.svg',
	surfWatch: 'images/SurfWatch.svg'
};

// Export for use in alert-system.js
if (typeof module !== 'undefined' && module.exports) {
	module.exports = {
		ALERT_PRESETS,
		ALERT_DEFAULTS,
		ALERT_SOUNDS,
		ALERT_ICONS
	};
}
