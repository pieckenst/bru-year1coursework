/* WebTV HD Alert Dialog System - Modular Component */

/**
 * WebTVAlert - A modular alert dialog system for WebTV HD
 * Handles all alert dialogs with customizable icons, messages, buttons, and sounds
 */
class WebTVAlert extends EventTarget {
	constructor(config = {}) {
		super();
		
		// DOM elements
		this.dialog = document.getElementById('dialog');
		this.dialogLogo = document.getElementById('dialogLogo');
		this.dialogMessage = document.getElementById('dialogMessage');
		this.dialogButton = document.getElementById('dialogButton');
		
		// State
		this.alertSound = null;
		this.currentAction = null;
		this.isOpen = false;
		
		// Configuration
		this.config = {
			animationDelay: config.animationDelay || 2,
			closeDelay: config.closeDelay || 2,
			defaultIcon: config.defaultIcon || 'images/WebTVShadowInset.svg',
			defaultSound: config.defaultSound || 'audio/error.mp3',
			defaultButtonText: config.defaultButtonText || 'Continue'
		};
		
		// Verify DOM elements exist
		if (!this.dialog || !this.dialogLogo || !this.dialogMessage || !this.dialogButton) {
			console.error('WebTVAlert: Required DOM elements not found');
		}
	}

	/**
	 * Sanitize HTML to prevent XSS attacks
	 * Removes scripts and event handlers from user input
	 */
	sanitizeHTML(html) {
		const temp = document.createElement('div');
		temp.innerHTML = html;
		
		// Remove all script tags
		const scripts = temp.getElementsByTagName('script');
		for (let i = scripts.length - 1; i >= 0; i--) {
			scripts[i].parentNode.removeChild(scripts[i]);
		}
		
		// Remove all event handler attributes
		const eventAttrs = [
			'onclick', 'ondblclick', 'onload', 'onmouseover', 'onmouseout',
			'onkeydown', 'onkeyup', 'onkeypress', 'onchange', 'onsubmit',
			'onblur', 'onfocus', 'onabort', 'onerror', 'onresize', 'onscroll'
		];
		
		const elementsWithEvents = temp.querySelectorAll('*');
		for (let j = 0; j < elementsWithEvents.length; j++) {
			const element = elementsWithEvents[j];
			eventAttrs.forEach(attr => element.removeAttribute(attr));
		}
		
		return temp.innerHTML;
	}

	/**
	 * Core show method - displays alert with custom configuration
	 * @param {Object} config - Alert configuration
	 * @param {string} config.message - Alert message (can include HTML)
	 * @param {string} config.icon - Icon image URL
	 * @param {string} config.buttonText - Button label text
	 * @param {string} config.sound - Sound file URL
	 * @param {Function} config.onButtonClick - Callback when button is clicked
	 */
	show(config = {}) {
		const {
			message = '',
			icon = this.config.defaultIcon,
			buttonText = this.config.defaultButtonText,
			sound = this.config.defaultSound,
			onButtonClick = null
		} = config;

		// Set dialog content
		this.dialogLogo.style.backgroundImage = `url(${icon})`;
		this.dialogMessage.innerHTML = this.sanitizeHTML(message);
		this.dialogButton.textContent = buttonText;
		
		// Set sound
		if (sound) {
			this.alertSound = new Audio(sound);
		}
		
		// Set button action
		if (onButtonClick) {
			this.currentAction = onButtonClick;
			this.dialogButton.addEventListener('click', this.currentAction, { once: true });
		}

		// Emit show event
		this.dispatchEvent(new CustomEvent('alertShow', {
			detail: { message, icon, buttonText, sound }
		}));

		// Open the dialog
		this.open();
	}

	/**
	 * Standard JavaScript alert - warning triangle icon
	 * @param {string} text - Alert message
	 */
	alert(text) {
		this.show({
			message: text,
			icon: 'images/JSAlert.svg',
			buttonText: 'OK',
			sound: 'audio/error.mp3'
		});
	}

	/**
	 * WebTV service alert - WebTV logo icon
	 * @param {string} text - Alert message
	 */
	showAlert(text) {
		this.show({
			message: text,
			icon: 'images/WebTVShadowInset.svg',
			buttonText: 'Continue',
			sound: 'audio/error.mp3'
		});
	}

	/**
	 * Custom alert with full control over appearance and behavior
	 * @param {string} text - Alert message (can include HTML)
	 * @param {string} image - Icon image URL or 'none'
	 * @param {string} label - Button label or 'none'
	 * @param {string|Function} action - JavaScript code to execute or function, or 'none'
	 */
	showCustomAlert(text, image, label, action) {
		// Validate parameters
		if (!text || text === 'none') {
			this.showAlert('Usage: showCustomAlert(\'Alert text\', \'Image URL\', \'Button Label\', \'Button Action Code\'); Use \'none\' if you don\'t want to specify part of a dialog.');
			return;
		}

		const config = {
			message: text,
			icon: (image && image !== 'none') ? image : 'images/WebTVShadowInset.svg',
			buttonText: (label && label !== 'none') ? label : 'Continue',
			sound: 'audio/error.mp3'
		};

		// Handle action parameter
		if (action && action !== 'none' && action !== null) {
			if (typeof action === 'function') {
				config.onButtonClick = action;
			} else if (typeof action === 'string') {
				// Store action for iframe execution
				window.tempAction = action;
				config.onButtonClick = () => {
					// Post message to iframe to execute action
					if (typeof iframe !== 'undefined' && iframe.contentWindow) {
						iframe.contentWindow.postMessage({ type: 'doAlertAction' }, '*');
					}
				};
			}
		}

		this.show(config);
	}

	/**
	 * Open dialog with animation
	 */
	open() {
		if (this.isOpen) return;
		
		this.isOpen = true;
		
		// Reset selection box in iframe if function exists
		if (typeof resetSelectionBoxIframe === 'function') {
			resetSelectionBoxIframe();
		}
		
		setTimeout(() => {
			// Play sound
			if (this.alertSound) {
				this.alertSound.play().catch(err => {
					console.warn('Could not play alert sound:', err);
				});
			}
			
			// Show dialog
			this.dialog.classList.remove('hidden');
			this.dialog.classList.add('shown');
			this.dialog.setAttribute('aria-hidden', 'false');
			this.dialog.showModal();
			this.dialogButton.classList.remove('noselect');
			
			// Reset selection box if function exists
			if (typeof resetSelectionBox === 'function') {
				resetSelectionBox();
			}
			
			// Highlight button if function exists
			if (typeof highlightNoScroll === 'function') {
				highlightNoScroll(this.dialogButton);
			}
		}, this.config.animationDelay);
	}

	/**
	 * Close dialog with animation and cleanup
	 */
	close() {
		if (!this.isOpen) return;
		
		setTimeout(() => {
			// Hide dialog
			this.dialog.classList.remove('shown');
			this.dialog.classList.add('hidden');
			this.dialog.close();
			this.dialog.setAttribute('aria-hidden', 'true');
			this.dialogButton.classList.add('noselect');
			
			// Reset selection box if function exists
			if (typeof resetSelectionBox === 'function') {
				resetSelectionBox();
			}
			
			// Clean up
			this.dialogMessage.textContent = '';
			this.dialogButton.textContent = this.config.defaultButtonText;
			this.dialogLogo.style.backgroundImage = `url(${this.config.defaultIcon})`;
			
			// Remove event listener if exists
			if (this.currentAction) {
				this.dialogButton.removeEventListener('click', this.currentAction);
				this.currentAction = null;
			}
			
			// Clear temp action
			if (window.tempAction) {
				window.tempAction = '';
			}
			
			// Focus iframe if exists
			if (typeof iframe !== 'undefined') {
				iframe.focus();
			}
			
			this.isOpen = false;
			
			// Emit close event
			this.dispatchEvent(new CustomEvent('alertClose'));
		}, this.config.closeDelay);
	}

	/**
	 * Utility methods for advanced customization
	 */
	setIcon(iconUrl) {
		this.dialogLogo.style.backgroundImage = `url(${iconUrl})`;
	}

	setText(text) {
		this.dialogMessage.innerHTML = this.sanitizeHTML(text);
	}

	setButtonText(text) {
		this.dialogButton.textContent = text;
	}

	setSound(soundUrl) {
		this.alertSound = new Audio(soundUrl);
	}

	/**
	 * Check if dialog is currently open
	 */
	isDialogOpen() {
		return this.isOpen;
	}
}

// Initialize global instance
const webTVAlert = new WebTVAlert();

// Override native alert and expose global functions
window.alert = function(text) {
	webTVAlert.alert(text);
};

window.showAlert = function(text) {
	webTVAlert.showAlert(text);
};

window.showCustomAlert = function(text, image, label, action) {
	webTVAlert.showCustomAlert(text, image, label, action);
};

window.closeDialog = function() {
	webTVAlert.close();
};

// Expose the class instance for advanced usage
window.webTVAlert = webTVAlert;

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
	module.exports = { WebTVAlert, webTVAlert };
}
