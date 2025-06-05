// ============================
// THEME TOGGLE - UI Component Layer
//
// Provides UI components and interactions for
// theme switching with theme.js and themeManager.js
// integration
// ============================

window.themeToggle = {
    // Initialize the theme toggle system
    initialize: function() {
        // Set initial state
        const savedTheme = this.getCurrentTheme();
        this.updateToggleUI(savedTheme);
        
        // Set up system preference listener
        this.setupSystemPreferenceListener();
        
        // Set up theme change listener
        if (window.theme) {
            window.theme.addThemeChangeListener(() => {
                this.updateToggleUI(this.getCurrentTheme());
            });
        }

        // Add thin sidebar toggle if it doesn't exist
        if (!document.querySelector('.thin-sidebar-theme-toggle')) {
            const sidebarToggle = document.createElement('button');
            sidebarToggle.className = 'thin-sidebar-button thin-sidebar-theme-toggle';
            sidebarToggle.innerHTML = `<i class="${savedTheme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill'}"></i>`;
            sidebarToggle.setAttribute('title', `Switch to ${savedTheme === 'dark' ? 'light' : 'dark'} theme`);
            sidebarToggle.onclick = () => this.toggleTheme();

            const sidebar = document.querySelector('.thin-sidebar');
            if (sidebar) {
                sidebar.insertBefore(sidebarToggle, sidebar.firstChild);
            }
        }
    },

    // Get current theme (delegates to theme system)
    getCurrentTheme: function() {
        if (window.themeManager) {
            return window.themeManager.getCurrentTheme();
        }
        if (window.theme) {
            return window.theme.getCurrentTheme();
        }
        return localStorage.getItem('app-theme') || 'auto';
    },

    // Set theme (delegates to theme system)
    setTheme: function(theme) {
        if (window.themeManager) {
            window.themeManager.setTheme(theme);
        } else if (window.theme) {
            window.theme.setTheme(theme);
        } else {
            localStorage.setItem('app-theme', theme);
            this.applyTheme(theme);
        }
        this.updateToggleUI(theme);
    },

    // Update UI to reflect current theme
    updateToggleUI: function(theme) {
        // Create theme overlay if it doesn't exist
        let overlay = document.querySelector('.theme-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'theme-overlay';
            document.body.appendChild(overlay);
        }

        // Get toggle button position for the animation origin
        const toggleButton = document.querySelector('.theme-toggle-button');
        if (toggleButton) {
            const rect = toggleButton.getBoundingClientRect();
            document.documentElement.style.setProperty('--theme-toggle-x', rect.left + rect.width / 2 + 'px');
            document.documentElement.style.setProperty('--theme-toggle-y', rect.top + rect.height / 2 + 'px');
        }

        // Set overlay color based on theme
        const overlayColor = theme === 'dark' ? 'rgba(0, 0, 0, 0.8)' : 'rgba(255, 255, 255, 0.8)';
        document.documentElement.style.setProperty('--theme-overlay-color', overlayColor);

        // Add transition class
        document.documentElement.classList.add('theme-switching');

        // Update toggle buttons with smooth icon rotation
        const toggles = document.querySelectorAll('[data-theme-toggle]');
        toggles.forEach(toggle => {
            const toggleTheme = toggle.getAttribute('data-theme-toggle');
            const isActive = toggleTheme === theme;
            
            // Update button state
            toggle.classList.toggle('active', isActive);
            toggle.setAttribute('aria-pressed', isActive);
            toggle.setAttribute('aria-label', `Switch to ${toggleTheme} theme`);

            // Rotate icon smoothly
            const icon = toggle.querySelector('i, svg');
            if (icon) {
                icon.style.transform = `rotate(${isActive ? '360deg' : '0deg'})`;
                icon.style.transition = 'transform 0.5s cubic-bezier(0.4, 0, 0.2, 1)';
            }
        });

        // Update theme indicators with fade transition
        const indicators = document.querySelectorAll('.theme-indicator');
        indicators.forEach(indicator => {
            indicator.style.opacity = '0';
            setTimeout(() => {
                indicator.textContent = theme.charAt(0).toUpperCase() + theme.slice(1);
                indicator.style.opacity = '1';
            }, 250);
        });

        // Update toggle switches with smooth transition
        const switches = document.querySelectorAll('.theme-switch input');
        switches.forEach(switchEl => {
            switchEl.checked = theme === 'dark';
        });

        // Update thin sidebar toggle
        const sidebarToggle = document.querySelector('.thin-sidebar-theme-toggle');
        if (sidebarToggle) {
            const icon = sidebarToggle.querySelector('i');
            if (icon) {
                icon.className = theme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
                icon.style.transform = `rotate(${theme === 'dark' ? '360deg' : '0deg'})`;
            }
            sidebarToggle.setAttribute('title', `Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`);
        }

        // Remove transition class after animation
        setTimeout(() => {
            document.documentElement.classList.remove('theme-switching');
        }, 500);
    },

    // Handle toggle switch change
    handleToggleChange: function(event) {
        const isDark = event.target.checked;
        this.setTheme(isDark ? 'dark' : 'light');
    },

    // Handle theme button click with animation
    handleThemeButtonClick: function(theme) {
        const button = event.currentTarget;
        const icon = button.querySelector('i, svg');
        
        // Add click animation
        if (icon) {
            icon.style.transform = 'scale(0.8)';
            setTimeout(() => {
                icon.style.transform = 'scale(1)';
            }, 200);
        }

        this.setTheme(theme);
    },

    // Toggle between light and dark with smooth transition
    toggleTheme: function() {
        const currentTheme = this.getCurrentTheme();
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        // Add transition class to body
        document.body.classList.add('theme-transition');
        
        // Set theme with animation
        this.setTheme(newTheme);
        
        // Remove transition class after animation
        setTimeout(() => {
            document.body.classList.remove('theme-transition');
        }, 500);
    },

    // System preference detection and handling
    setupSystemPreferenceListener: function() {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            const handleChange = () => {
                if (this.getCurrentTheme() === 'auto') {
                    this.updateToggleUI('auto');
                }
            };

            if (mediaQuery.addEventListener) {
                mediaQuery.addEventListener('change', handleChange);
            } else if (mediaQuery.addListener) {
                mediaQuery.addListener(handleChange);
            }
        }
    },

    // Fallback theme application (when theme.js/themeManager.js not available)
    applyTheme: function(theme) {
        const prefersDark = window.matchMedia && 
            window.matchMedia('(prefers-color-scheme: dark)').matches;
        const isDark = theme === 'dark' || (theme === 'auto' && prefersDark);

        document.documentElement.classList.remove('light-theme', 'dark-theme');
        document.documentElement.classList.add(isDark ? 'dark-theme' : 'light-theme');
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    },

    // Blazor component integration
    getToggleState: function() {
        return {
            theme: this.getCurrentTheme(),
            isDark: this.getCurrentTheme() === 'dark',
            isAuto: this.getCurrentTheme() === 'auto'
        };
    },

    // Enhanced UI features
    addAnimationClass: function(element, className, duration = 1000) {
        if (!element) return;
        element.classList.add(className);
        setTimeout(() => element.classList.remove(className), duration);
    },

    // Accessibility enhancements
    announceThemeChange: function(theme) {
        const announcement = document.createElement('div');
        announcement.setAttribute('role', 'status');
        announcement.setAttribute('aria-live', 'polite');
        announcement.className = 'sr-only';
        announcement.textContent = `Theme changed to ${theme}`;
        document.body.appendChild(announcement);
        setTimeout(() => document.body.removeChild(announcement), 3000);
    }
};

// Initialize when the document is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.themeToggle.initialize();
    });
} else {
    window.themeToggle.initialize();
}
