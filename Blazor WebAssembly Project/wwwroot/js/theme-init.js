// Initial theme setup
document.addEventListener('DOMContentLoaded', function () {
    const savedTheme = localStorage.getItem('app-theme') || 'auto';
    if (window.themeManager && typeof window.themeManager.setTheme === 'function') {
        window.themeManager.setTheme(savedTheme);
    }
});
