// ============================
// THEME TOGGLE JAVASCRIPT
//
// Functionality for the theme toggle component
// with support for light/dark/auto modes and
// integration with themeManager
// ============================

window.themeToggle = {
    // Initialize the theme toggle system
    initialize: function () {
        console.log("Theme toggle initialized");

        // Set initial theme from localStorage or default to auto
        const savedTheme = this.getSavedTheme();
        this.applyTheme(savedTheme);

        // Set up listeners for system preference changes
        this.setupSystemPreferenceListener();
    },

    // Get the current theme from localStorage
    getSavedTheme: function () {
        return localStorage.getItem('app-theme') || 'auto';
    },

    // Save theme preference to localStorage
    saveTheme: function (theme) {
        localStorage.setItem('app-theme', theme);
    },

    // Check if system is in dark mode
    isSystemDarkMode: function () {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    // Get boolean indicating if current theme setting is dark
    getThemePreference: function () {
        const theme = this.getSavedTheme();
        return theme === 'dark' || (theme === 'auto' && this.isSystemDarkMode());
    },

    // Set theme preference (boolean version - for backward compatibility)
    setThemePreference: function (isDark) {
        const newTheme = isDark ? 'dark' : 'light';
        this.saveTheme(newTheme);

        // If themeManager exists, use it, otherwise apply directly
        if (window.themeManager && typeof window.themeManager.setTheme === 'function') {
            window.themeManager.setTheme(newTheme);
        } else {
            this.applyTheme(newTheme);
        }
        return true;
    },

    // Apply theme directly to DOM (fallback if themeManager not available)
    applyTheme: function (theme) {
        const isDarkMode = theme === 'dark' || (theme === 'auto' && this.isSystemDarkMode());

        if (isDarkMode) {
            document.documentElement.classList.add('dark-theme');
            document.documentElement.classList.remove('light-theme');
        } else {
            document.documentElement.classList.add('light-theme');
            document.documentElement.classList.remove('dark-theme');
        }

        // Update UI elements
        this.updateToggleUI(theme);
    },

    // Update UI elements to reflect current theme
    updateToggleUI: function (theme) {
        const toggles = document.querySelectorAll('[data-theme-toggle]');
        toggles.forEach(toggle => {
            const toggleTheme = toggle.getAttribute('data-theme-toggle');
            if (toggleTheme === theme) {
                toggle.classList.add('active');
            } else {
                toggle.classList.remove('active');
            }
        });
    },

    // Set up listener for system preference changes (for auto mode)
    setupSystemPreferenceListener: function () {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

            // Use newer event listener if available
            if (mediaQuery.addEventListener) {
                mediaQuery.addEventListener('change', e => {
                    if (this.getSavedTheme() === 'auto') {
                        this.applyTheme('auto');
                    }
                });
            } else if (mediaQuery.addListener) {
                // Fallback for older browsers
                mediaQuery.addListener(e => {
                    if (this.getSavedTheme() === 'auto') {
                        this.applyTheme('auto');
                    }
                });
            }
        }
    }
};

// Initialize the theme toggle when the document is loaded
document.addEventListener('DOMContentLoaded', function () {
    if (window.themeToggle) {
        window.themeToggle.initialize();
    }
});
