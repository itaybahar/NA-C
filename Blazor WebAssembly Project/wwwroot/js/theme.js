// theme.js - Core theme system for Blazor WebAssembly

// Theme management constants
const themeKey = 'app-theme';
const defaultTheme = 'light';
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
    const handleChange = () => {
        if (getSavedTheme() === THEME_AUTO) {
            applyTheme(THEME_AUTO, false);
        }
    };

    // Use proper method based on browser support
    if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', handleChange);
    } else if (mediaQuery.addListener) {
        mediaQuery.addListener(handleChange);
    }

    // Notify themeManager and themeToggle if they exist
    if (window.themeManager) {
        window.themeManager.refreshTheme();
    }
    if (window.themeToggle) {
        window.themeToggle.initialize();
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
        // Notify other theme components
        if (window.themeManager) {
            window.themeManager.refreshTheme();
        }
        if (window.themeToggle) {
            window.themeToggle.updateToggleUI(theme);
        }
    } catch (e) {
        console.warn("Could not save theme to localStorage", e);
    }
}

// Apply the theme to the document with smooth transitions
function applyTheme(theme, isInitialLoad = false) {
    if (isApplyingTheme) {
        return;
    }

    try {
        isApplyingTheme = true;

        const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDarkMode = theme === THEME_DARK || (theme === THEME_AUTO && prefersDark);
        const effectiveTheme = isDarkMode ? THEME_DARK : THEME_LIGHT;

        // Add transition class for smooth theme changes
        if (!isInitialLoad) {
            document.documentElement.classList.add('theme-transition-root');
            document.body.classList.add('theme-transition');
        }

        // Create or update the transition overlay
        let overlay = document.querySelector('.theme-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'theme-overlay';
            document.body.appendChild(overlay);
        }

        // Set the overlay color based on the theme
        const overlayColor = isDarkMode ? 'rgba(0, 0, 0, 0.8)' : 'rgba(255, 255, 255, 0.8)';
        document.documentElement.style.setProperty('--theme-overlay-color', overlayColor);

        // Add switching class to trigger overlay animation
        document.documentElement.classList.add('theme-switching');

        // Update theme classes with a slight delay for smooth transition
        setTimeout(() => {
            // Update theme classes
            document.documentElement.classList.remove(DARK_THEME_CLASS, LIGHT_THEME_CLASS);
            document.body.classList.remove(DARK_THEME_CLASS, LIGHT_THEME_CLASS);
            
            const themeClass = isDarkMode ? DARK_THEME_CLASS : LIGHT_THEME_CLASS;
            document.documentElement.classList.add(themeClass);
            document.body.classList.add(themeClass);

            // Update data attribute for CSS targeting
            document.documentElement.setAttribute('data-theme', effectiveTheme);

            // Dispatch theme change event
            if (!isInitialLoad && !document.hidden) {
                const event = new CustomEvent(themeChangeEvent, {
                    detail: { 
                        theme: effectiveTheme, 
                        isDark: isDarkMode,
                        isAnimated: true 
                    },
                    bubbles: true
                });
                document.dispatchEvent(event);
            }
        }, 50);

        // Remove transition classes after animation
        setTimeout(() => {
            document.documentElement.classList.remove('theme-switching');
            if (!isInitialLoad) {
                document.documentElement.classList.remove('theme-transition-root');
                document.body.classList.remove('theme-transition');
            }
        }, 500);

    } finally {
        isApplyingTheme = false;
    }
}

// Public API
window.theme = {
    getCurrentTheme: getSavedTheme,
    setTheme: function(theme) {
        const validTheme = [THEME_LIGHT, THEME_DARK, THEME_AUTO].includes(theme)
            ? theme
            : defaultTheme;
        saveTheme(validTheme);
        applyTheme(validTheme);
        return validTheme;
    },
    isDarkMode: function() {
        return document.documentElement.classList.contains(DARK_THEME_CLASS);
    },
    toggleTheme: function() {
        const currentTheme = getSavedTheme();
        const newTheme = currentTheme === THEME_DARK ? THEME_LIGHT : THEME_DARK;
        return this.setTheme(newTheme);
    },
    addThemeChangeListener: function(callback) {
        document.addEventListener(themeChangeEvent, callback);
        return () => document.removeEventListener(themeChangeEvent, callback);
    }
};

// Handle visibility and navigation events
document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
        setTimeout(() => applyTheme(getSavedTheme(), false), 100);
    }
});

document.addEventListener('blazorNavigated', () => {
    setTimeout(() => applyTheme(getSavedTheme(), false), 100);
});
