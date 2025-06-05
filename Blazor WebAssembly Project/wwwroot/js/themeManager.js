// ============================
// THEME MANAGER - Enhanced Theme Management
//
// Provides advanced theme management features and
// integration with theme.js core system
// ============================

window.themeManager = {
    // Core theme operations (delegates to theme.js)
    getCurrentTheme: function() {
        return window.theme ? window.theme.getCurrentTheme() : 'light';
    },

    setTheme: function(theme) {
        if (window.theme) {
            return window.theme.setTheme(theme);
        }
        return theme;
    },

    isDarkMode: function() {
        return window.theme ? window.theme.isDarkMode() : false;
    },

    // Enhanced theme management features
    isSystemDarkMode: function() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    getEffectiveTheme: function() {
        return this.isDarkMode() ? 'dark' : 'light';
    },

    // Theme preference management
    getThemePreference: function() {
        const theme = this.getCurrentTheme();
        return theme === 'dark' || (theme === 'auto' && this.isSystemDarkMode());
    },

    setThemePreference: function(isDark) {
        return this.setTheme(isDark ? 'dark' : 'light');
    },

    // Auto theme management
    setAutoTheme: function() {
        return this.setTheme('auto');
    },

    // Theme change notifications
    addThemeChangeListener: function(callback) {
        if (window.theme) {
            return window.theme.addThemeChangeListener(callback);
        }
        document.addEventListener('themeChanged', callback);
        return () => document.removeEventListener('themeChanged', callback);
    },

    removeThemeChangeListener: function(callback) {
        document.removeEventListener('themeChanged', callback);
    },

    // Theme refresh and sync
    refreshTheme: function() {
        const currentTheme = this.getCurrentTheme();
        if (window.theme) {
            window.theme.setTheme(currentTheme);
        }
        if (window.themeToggle) {
            window.themeToggle.updateToggleUI(currentTheme);
        }
    },

    // Theme transition management
    enableTransitions: function() {
        document.body.classList.remove('disable-transitions');
    },

    disableTransitions: function() {
        document.body.classList.add('disable-transitions');
    },

    // Theme class management
    updateThemeClass: function(element, isDark) {
        if (!element) return;
        
        element.classList.remove('light-theme', 'dark-theme');
        element.classList.add(isDark ? 'dark-theme' : 'light-theme');
        element.setAttribute('data-theme', isDark ? 'dark' : 'light');
    },

    // Theme UI management
    updateAllThemeToggles: function(theme) {
        const toggles = document.querySelectorAll('[data-theme-toggle]');
        toggles.forEach(toggle => {
            const targetTheme = toggle.getAttribute('data-theme-toggle');
            toggle.classList.toggle('active', targetTheme === theme);
        });
    },

    // Blazor interop helpers
    getBlazorTheme: function() {
        return {
            current: this.getCurrentTheme(),
            isDark: this.isDarkMode(),
            isAuto: this.getCurrentTheme() === 'auto',
            systemPreference: this.isSystemDarkMode() ? 'dark' : 'light'
        };
    },

    // Initialize theme manager
    initialize: function() {
        // Set up system preference change listener
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        const handleChange = () => {
            if (this.getCurrentTheme() === 'auto') {
                this.refreshTheme();
            }
        };

        if (mediaQuery.addEventListener) {
            mediaQuery.addEventListener('change', handleChange);
        } else if (mediaQuery.addListener) {
            mediaQuery.addListener(handleChange);
        }

        // Initial theme application
        this.refreshTheme();

        // Set up visibility change handler
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                this.refreshTheme();
            }
        });

        // Handle Blazor navigation
        document.addEventListener('blazorNavigated', () => {
            this.refreshTheme();
        });
    }
};

// Initialize when the document is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.themeManager.initialize();
    });
} else {
    window.themeManager.initialize();
} 