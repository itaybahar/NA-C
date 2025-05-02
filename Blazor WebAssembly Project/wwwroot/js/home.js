// home.js - Safe home page animations for Blazor WebAssembly

// Initialize resources needed for home page
window.loadHomePageResources = function () {
    // Check if Font Awesome is loaded
    if (!document.getElementById('font-awesome')) {
        loadCss('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css', 'font-awesome');
    }

    // Add simple animations without large libraries
    addSimpleAnimations();
};

// Function to add CSS safely
function loadCss(url, id) {
    return new Promise((resolve, reject) => {
        try {
            if (!document.getElementById(id)) {
                const link = document.createElement('link');
                link.id = id;
                link.rel = 'stylesheet';
                link.href = url;
                link.onload = () => resolve();
                link.onerror = () => reject(new Error(`Failed to load CSS: ${url}`));
                document.head.appendChild(link);
            } else {
                resolve();
            }
        } catch (error) {
            console.error("Error loading CSS:", error);
            reject(error);
        }
    });
}

// Add simple CSS animations to replace Animate.css
function addSimpleAnimations() {
    if (!document.getElementById('simple-animations')) {
        const style = document.createElement('style');
        style.id = 'simple-animations';
        style.textContent = `
            .animated { animation-duration: 1s; animation-fill-mode: both; }
            .fadeInDown { animation-name: fadeInDown; }
            .fadeInUp { animation-name: fadeInUp; }
            .zoomIn { animation-name: zoomIn; }
            .delay-1s { animation-delay: 1s; }
            .delay-2s { animation-delay: 2s; }
            
            @keyframes fadeInDown {
                from { opacity: 0; transform: translate3d(0, -30px, 0); }
                to { opacity: 1; transform: translate3d(0, 0, 0); }
            }
            
            @keyframes fadeInUp {
                from { opacity: 0; transform: translate3d(0, 30px, 0); }
                to { opacity: 1; transform: translate3d(0, 0, 0); }
            }
            
            @keyframes zoomIn {
                from { opacity: 0; transform: scale3d(0.3, 0.3, 0.3); }
                50% { opacity: 1; }
            }
            
            .animate-fade-in {
                opacity: 0;
                animation: fadeIn 1s forwards;
            }
            
            @keyframes fadeIn {
                to { opacity: 1; }
            }
        `;
        document.head.appendChild(style);
    }
}

// Safe method to set fallback image instead of using eval
window.setFallbackImage = function (element, fallbackSrc) {
    try {
        if (element) {
            element.onerror = null;
            element.src = fallbackSrc;
        }
    } catch (error) {
        console.error("Error setting fallback image:", error);
    }
};
