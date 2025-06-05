// ============================
// THEME INITIALIZATION
//
// Initializes the theme system components in the
// correct order with proper dependencies
// ============================

document.addEventListener('DOMContentLoaded', function() {
    // Get saved theme or default to auto
    const savedTheme = localStorage.getItem('app-theme') || 'auto';

    // Initialize core theme system first
    if (window.theme) {
        window.theme.setTheme(savedTheme);
    }

    // Initialize theme manager if available
    if (window.themeManager) {
        window.themeManager.initialize();
    }

    // Initialize theme toggle components
    if (window.themeToggle) {
        window.themeToggle.initialize();
    }

    // Set up Blazor interop
    window.getTheme = function() {
        if (window.themeManager) {
            return window.themeManager.getCurrentTheme();
        }
        if (window.theme) {
            return window.theme.getCurrentTheme();
        }
        return savedTheme;
    };

    window.setTheme = function(theme) {
        if (window.themeManager) {
            return window.themeManager.setTheme(theme);
        }
        if (window.theme) {
            return window.theme.setTheme(theme);
        }
        localStorage.setItem('app-theme', theme);
        return theme;
    };
});
