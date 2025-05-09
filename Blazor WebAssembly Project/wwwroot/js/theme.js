// theme.js - Improved theme management for Blazor WebAssembly

// Theme management constants
const themeKey = 'app-theme';
const defaultTheme = 'light'; // Changed default to 'light' instead of 'auto'
const THEME_DARK = 'dark';
const THEME_LIGHT = 'light';
const THEME_AUTO = 'auto';
const DARK_THEME_CLASS = 'dark-theme';
const LIGHT_THEME_CLASS = 'light-theme';

// Theme change event for cross-component communication
const themeChangeEvent = 'themeChanged';

// Track if we're currently applying a theme to prevent event recursion
let isApplyingTheme = false;

// Initialize the theme on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        initializeTheme();
    });
} else {
    // DOM already loaded, initialize immediately
    initializeTheme();
}

function initializeTheme() {
    const savedTheme = getSavedTheme();
    applyTheme(savedTheme, true);

    // Listen for system preference changes
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    // Use proper method based on browser support
    if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', () => {
            // Only update if using auto theme
            if (getSavedTheme() === THEME_AUTO) {
                applyTheme(THEME_AUTO, false);
            }
        });
    } else if (mediaQuery.addListener) {
        // Fallback for older browsers
        mediaQuery.addListener(() => {
            // Only update if using auto theme
            if (getSavedTheme() === THEME_AUTO) {
                applyTheme(THEME_AUTO, false);
            }
        });
    }
}

// Get the user's saved theme preference or use default
function getSavedTheme() {
    try {
        return localStorage.getItem(themeKey) || defaultTheme;
    } catch (e) {
        console.warn("Could not access localStorage for theme", e);
        return defaultTheme;
    }
}

// Save theme preference to local storage
function saveTheme(theme) {
    try {
        localStorage.setItem(themeKey, theme);
    } catch (e) {
        console.warn("Could not save theme to localStorage", e);
    }
}

// Apply the theme to the document
function applyTheme(theme, isInitialLoad = false) {
    // Prevent recursive calls and event dispatches
    if (isApplyingTheme) {
        console.warn("Already applying theme, skipping recursive call");
        return;
    }

    try {
        isApplyingTheme = true;

        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDarkMode = theme === THEME_DARK || (theme === THEME_AUTO && prefersDark);
        const effectiveTheme = isDarkMode ? THEME_DARK : THEME_LIGHT;

        // Update data attribute first
        document.documentElement.setAttribute('data-theme', effectiveTheme);

        // Apply to root element (affects all tabs/pages)
        document.documentElement.classList.remove(DARK_THEME_CLASS, LIGHT_THEME_CLASS);
        document.documentElement.classList.add(isDarkMode ? DARK_THEME_CLASS : LIGHT_THEME_CLASS);

        // Apply to body as well for components that might depend on body classes
        document.body.classList.remove(DARK_THEME_CLASS, LIGHT_THEME_CLASS);
        document.body.classList.add(isDarkMode ? DARK_THEME_CLASS : LIGHT_THEME_CLASS);

        // Update any theme toggle UI elements
        updateAllToggleUI(theme);

        // Update all theme toggle buttons
        updateAllThemeToggles(effectiveTheme);

        // Dispatch custom event for any components that need to react to theme changes
        if (!isInitialLoad && !document.hidden) {
            // Create a new event instead of reusing a global one
            try {
                const newEvent = new CustomEvent(themeChangeEvent, { bubbles: true });
                document.dispatchEvent(newEvent);
            } catch (e) {
                console.warn("Could not dispatch theme change event", e);
            }
        }
    } finally {
        isApplyingTheme = false;
    }
}

// Update all theme toggle UI elements across the application
function updateAllToggleUI(theme) {
    try {
        const themeToggles = document.querySelectorAll('[data-theme-toggle]');
        themeToggles.forEach(toggle => {
            const targetTheme = toggle.getAttribute('data-theme-toggle');
            if (targetTheme === theme) {
                toggle.classList.add('active');
            } else {
                toggle.classList.remove('active');
            }
        });
    } catch (e) {
        console.warn("Error updating theme toggle UI", e);
    }
}

// Update the visual state of all theme toggle containers
function updateAllThemeToggles(effectiveTheme) {
    try {
        const themeToggles = document.querySelectorAll('.theme-toggle');
        themeToggles.forEach(toggle => {
            toggle.setAttribute('data-theme', effectiveTheme);
        });
    } catch (e) {
        console.warn("Error updating theme toggles", e);
    }
}

// Force reapplication of theme throughout the app
function forceThemeReapplication() {
    const currentTheme = getSavedTheme();
    // Add a small delay to ensure DOM is ready
    setTimeout(() => applyTheme(currentTheme, false), 0);
}

// Public theme management API
window.themeManager = {
    getCurrentTheme: getSavedTheme,

    setTheme: function (theme) {
        // Validate theme
        const validTheme = [THEME_LIGHT, THEME_DARK, THEME_AUTO].includes(theme)
            ? theme
            : defaultTheme;

        saveTheme(validTheme);

        // Add transition class for smooth animations
        document.body.classList.add('theme-transition');

        // Apply the theme
        applyTheme(validTheme);

        // Remove transition class after animation completes
        setTimeout(() => {
            document.body.classList.remove('theme-transition');
        }, 1000);

        return validTheme;
    },

    // Force refresh theme state (useful after routing/navigation)
    refreshTheme: function () {
        setTimeout(() => forceThemeReapplication(), 0);
    },

    // Check if currently in dark mode (regardless of theme setting)
    isDarkMode: function () {
        return document.documentElement.classList.contains(DARK_THEME_CLASS);
    }
};

// Auto-refresh theme when page visibility changes (for tab switching)
document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
        setTimeout(() => forceThemeReapplication(), 100);
    }
});

// Handle Blazor navigation events
document.addEventListener('blazorNavigated', () => {
    setTimeout(() => forceThemeReapplication(), 100);
});

// Initialize when loaded as a module
if (typeof module !== 'undefined' && module.exports) {
    module.exports = window.themeManager;
}
