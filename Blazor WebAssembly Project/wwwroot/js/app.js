// Enhanced logging system for debugging page loading issues
window.debugLogger = {
    enabled: true,  // Set to false to disable all logs when not debugging
    timestamps: true,

    // Set logging options
    init: function (options = {}) {
        if (options.hasOwnProperty('enabled')) this.enabled = options.enabled;
        if (options.hasOwnProperty('timestamps')) this.timestamps = options.timestamps;
        this.log('Debug logger initialized', 'init');
    },

    // Main logging method
    log: function (message, category = 'general', level = 'info') {
        if (!this.enabled) return;

        const timestamp = this.timestamps ? `[${new Date().toISOString().substring(11, 23)}] ` : '';
        const prefix = `${timestamp}[${category.toUpperCase()}] `;

        switch (level) {
            case 'error':
                console.error(prefix + message);
                break;
            case 'warn':
                console.warn(prefix + message);
                break;
            case 'success':
                console.log(`%c${prefix}${message}`, 'color: green; font-weight: bold');
                break;
            case 'info':
            default:
                console.log(prefix + message);
        }
    },

    // Specialized methods
    pageLoad: function (stage) {
        this.log(`Page load stage: ${stage}`, 'page', 'info');
    },

    auth: function (message, level = 'info') {
        this.log(message, 'auth', level);
    },

    api: function (message, level = 'info') {
        this.log(message, 'api', level);
    },

    // Group logging for structured data
    startGroup: function (name) {
        if (!this.enabled) return;
        console.group(`📁 ${name}`);
    },

    endGroup: function () {
        if (!this.enabled) return;
        console.groupEnd();
    },

    // Object inspection
    inspect: function (obj, label = 'Object inspection') {
        if (!this.enabled) return;
        this.startGroup(label);
        console.dir(obj);
        this.endGroup();
    }
};

// Initialize page load tracking
(function () {
    // Initialize immediately
    window.debugLogger.init();
    window.debugLogger.pageLoad('Script initialization');

    // Track page load state
    const readyState = document.readyState;
    window.debugLogger.pageLoad(`Initial document state: ${readyState}`);

    if (readyState === 'loading') {
        window.debugLogger.pageLoad('Document still loading, adding listeners');

        // Track DOM content loaded
        document.addEventListener('DOMContentLoaded', () => {
            window.debugLogger.pageLoad('DOM content loaded');
        });

        // Track window load complete
        window.addEventListener('load', () => {
            window.debugLogger.pageLoad('Window fully loaded');
        });
    } else {
        window.debugLogger.pageLoad('Document already loaded');
    }

    // Track Blazor loading stages
    const originalBlazorStart = window._blazorStart;
    if (originalBlazorStart) {
        window.debugLogger.pageLoad('Monkey-patching Blazor startup');
        window._blazorStart = function () {
            window.debugLogger.pageLoad('Blazor starting');
            return originalBlazorStart.apply(this, arguments);
        };
    }
})();

// Track API connection attempts
const originalApiConnection = window.apiConnection || {};
window.apiConnection = {
    ...originalApiConnection,

    // Override discoverApi method with logging
    discoverApi: async function () {
        window.debugLogger.startGroup('API Discovery Process');
        window.debugLogger.api('Starting API discovery');

        try {
            // Get possible API URLs
            const possibleBaseUrls = [
                window.location.origin,
                "https://localhost:5191/",
                "https://localhost:7235/",
                "https://localhost:5001/",
                "https://localhost:5002/",
                "https://localhost:7001/",
                "https://localhost:7002/",
                "https://localhost:7176/",
                "https://localhost:7177/",
                "https://localhost:7178/",
                "https://localhost:7179/",
                "https://localhost:7180/"
            ];

            window.debugLogger.api(`Checking ${possibleBaseUrls.length} possible API URLs`);

            // Try cached URL first
            const cachedUrl = localStorage.getItem('api_baseUrl');
            if (cachedUrl) {
                window.debugLogger.api(`Testing cached API URL: ${cachedUrl}`);
                try {
                    const normalizedUrl = cachedUrl.endsWith('/') ? cachedUrl : cachedUrl + '/';
                    window.debugLogger.api(`Normalized cached URL: ${normalizedUrl}`);

                    const startTime = performance.now();
                    const response = await this.testEndpoint(normalizedUrl + 'health');
                    const duration = (performance.now() - startTime).toFixed(2);

                    window.debugLogger.api(`Cached URL response in ${duration}ms - Status: ${response.status} ${response.ok ? '✓' : '✗'}`);

                    if (response.ok) {
                        window.debugLogger.api(`Using cached API URL: ${cachedUrl}`, 'success');
                        window.debugLogger.endGroup();
                        return cachedUrl;
                    }
                } catch (e) {
                    window.debugLogger.api(`Error testing cached URL: ${e.message}`, 'error');
                }
            } else {
                window.debugLogger.api('No cached API URL found');
            }

            // Try each possible URL with multiple endpoints
            for (const baseUrl of possibleBaseUrls) {
                window.debugLogger.startGroup(`Testing ${baseUrl}`);
                try {
                    const normalizedUrl = baseUrl.endsWith('/') ? baseUrl : baseUrl + '/';
                    window.debugLogger.api(`Normalized URL: ${normalizedUrl}`);

                    // Try health endpoint
                    window.debugLogger.api(`Testing health endpoint...`);
                    try {
                        const startTime = performance.now();
                        let response = await this.testEndpoint(normalizedUrl + 'health');
                        const duration = (performance.now() - startTime).toFixed(2);

                        window.debugLogger.api(`Health endpoint response in ${duration}ms - Status: ${response.status}`);

                        if (response.ok) {
                            window.debugLogger.api(`API found at: ${normalizedUrl} (health endpoint)`, 'success');
                            localStorage.setItem('api_baseUrl', normalizedUrl);
                            window.debugLogger.endGroup();
                            window.debugLogger.endGroup();
                            return normalizedUrl;
                        }
                    } catch (healthErr) {
                        window.debugLogger.api(`Health endpoint error: ${healthErr.message}`, 'warn');
                    }

                    // Try server-info endpoint
                    window.debugLogger.api(`Testing server-info endpoint...`);
                    try {
                        const startTime = performance.now();
                        let response = await this.testEndpoint(normalizedUrl + 'api/server-info/ports');
                        const duration = (performance.now() - startTime).toFixed(2);

                        window.debugLogger.api(`Server-info endpoint response in ${duration}ms - Status: ${response.status}`);

                        if (response.ok) {
                            window.debugLogger.api(`API found at: ${normalizedUrl} (server-info endpoint)`, 'success');
                            localStorage.setItem('api_baseUrl', normalizedUrl);
                            window.debugLogger.endGroup();
                            window.debugLogger.endGroup();
                            return normalizedUrl;
                        }
                    } catch (infoErr) {
                        window.debugLogger.api(`Server-info endpoint error: ${infoErr.message}`, 'warn');
                    }

                    // Try equipment endpoint
                    window.debugLogger.api(`Testing equipment endpoint...`);
                    try {
                        const startTime = performance.now();
                        let response = await this.testEndpoint(normalizedUrl + 'api/equipment');
                        const duration = (performance.now() - startTime).toFixed(2);

                        window.debugLogger.api(`Equipment endpoint response in ${duration}ms - Status: ${response.status}`);

                        if (response.ok) {
                            window.debugLogger.api(`API found at: ${normalizedUrl} (equipment endpoint)`, 'success');
                            localStorage.setItem('api_baseUrl', normalizedUrl);
                            window.debugLogger.endGroup();
                            window.debugLogger.endGroup();
                            return normalizedUrl;
                        }
                    } catch (equipErr) {
                        window.debugLogger.api(`Equipment endpoint error: ${equipErr.message}`, 'warn');
                    }

                    window.debugLogger.endGroup();
                } catch (e) {
                    window.debugLogger.api(`Error testing ${baseUrl}: ${e.message}`, 'error');
                    window.debugLogger.endGroup();
                }
            }

            window.debugLogger.api('API discovery failed - no working endpoints found', 'error');
            window.debugLogger.endGroup();
            return null;

        } catch (e) {
            window.debugLogger.api(`Unexpected error in API discovery: ${e.message}`, 'error');
            window.debugLogger.endGroup();
            return null;
        }
    },

    // Helper to test an endpoint with timeout and logging
    testEndpoint: async function (url) {
        try {
            window.debugLogger.api(`Fetching ${url}`);
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 3000);

            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                mode: 'cors',
                cache: 'no-cache',
                signal: controller.signal
            });

            clearTimeout(timeoutId);
            return response;
        } catch (error) {
            if (error.name === 'AbortError') {
                window.debugLogger.api(`Request to ${url} timed out after 3000ms`, 'warn');
            } else {
                window.debugLogger.api(`Fetch error for ${url}: ${error.message}`, 'error');
            }
            throw error;
        }
    }
};

// Track window.testApiConnectivity usage
const originalTestApiConnectivity = window.testApiConnectivity;
window.testApiConnectivity = async function (urls) {
    window.debugLogger.startGroup('Testing API Connectivity');
    window.debugLogger.api(`Called testApiConnectivity with URLs: ${JSON.stringify(urls)}`);

    try {
        // Delegate to loginFunctions if it exists
        if (window.loginFunctions && typeof window.loginFunctions.testApiConnectivity === 'function') {
            window.debugLogger.api('Delegating to loginFunctions.testApiConnectivity');
            const result = await window.loginFunctions.testApiConnectivity(urls);
            window.debugLogger.api(`Result from loginFunctions: ${result || 'null'}`);
            window.debugLogger.endGroup();
            return result;
        }

        // If we got here, we're using the original function or fallback
        window.debugLogger.api('Using original testApiConnectivity implementation');
        const result = await originalTestApiConnectivity(urls);
        window.debugLogger.api(`Result: ${result || 'null'}`);
        window.debugLogger.endGroup();
        return result;
    } catch (error) {
        window.debugLogger.api(`Error in testApiConnectivity: ${error.message}`, 'error');
        window.debugLogger.endGroup();
        throw error;
    }
};

// Log Blazor-related events
document.addEventListener('blazor:enhanced', () => {
    window.debugLogger.pageLoad('Blazor enhanced event fired');
});

document.addEventListener('blazor:navigated', () => {
    window.debugLogger.pageLoad('Blazor navigation completed');
});

document.addEventListener('blazor:error', (event) => {
    window.debugLogger.pageLoad(`Blazor error: ${event.detail?.error?.message || 'Unknown error'}`, 'error');
});

// Add diagnostic button to loading screen after a delay
setTimeout(() => {
    const loadingProgress = document.querySelector('.loading-progress');
    if (loadingProgress) {
        window.debugLogger.pageLoad('Still on loading screen after timeout, adding diagnostic button');

        const diagButton = document.createElement('button');
        diagButton.className = 'btn btn-warning mt-3';
        diagButton.textContent = 'Run Diagnostics';
        diagButton.onclick = runLoadingDiagnostics;

        const connectionStatus = document.getElementById('connection-status');
        if (connectionStatus) {
            connectionStatus.parentNode.insertBefore(diagButton, connectionStatus.nextSibling);
        } else {
            loadingProgress.appendChild(diagButton);
        }
    }
}, 5000);

// Diagnostic function for loading issues
function runLoadingDiagnostics() {
    window.debugLogger.startGroup('Loading Diagnostics');

    // Check if Blazor script loaded
    const blazorScript = document.querySelector('script[src*="blazor.webassembly.js"]');
    window.debugLogger.log(`Blazor script found: ${blazorScript !== null}`, 'diagnostics');

    // Check if any network requests are pending
    const pendingFetches = window.performance
        .getEntriesByType('resource')
        .filter(r => !r.responseEnd && r.startTime);

    window.debugLogger.log(`Pending network requests: ${pendingFetches.length}`, 'diagnostics');

    // Check localStorage
    try {
        window.debugLogger.log(`LocalStorage available: ${!!window.localStorage}`, 'diagnostics');
        const apiUrl = localStorage.getItem('api_baseUrl');
        window.debugLogger.log(`Cached API URL: ${apiUrl || 'none'}`, 'diagnostics');
    } catch (e) {
        window.debugLogger.log(`LocalStorage error: ${e.message}`, 'diagnostics', 'error');
    }

    // Force API discovery
    window.debugLogger.log('Forcing API discovery...', 'diagnostics');
    window.apiConnection.discoverApi().then(url => {
        window.debugLogger.log(`API discovery result: ${url || 'failed'}`, 'diagnostics', url ? 'success' : 'error');
    }).catch(err => {
        window.debugLogger.log(`API discovery error: ${err.message}`, 'diagnostics', 'error');
    });

    window.debugLogger.endGroup();
}

window.debugLogger.pageLoad('app.js loaded completely');
