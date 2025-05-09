// Enhanced loginFunctions with detailed logging
window.loginFunctions = {
    testApiConnectivity: async function (urls) {
        if (window.debugLogger) {
            window.debugLogger.startGroup('Login.js API Connectivity Test');
            window.debugLogger.api(`Testing API connectivity to: ${JSON.stringify(urls)}`);
        } else {
            console.log('Testing API connectivity to:', urls);
        }

        // Handle invalid input
        if (!urls || !Array.isArray(urls)) {
            const errorMsg = 'Invalid URLs parameter';
            if (window.debugLogger) {
                window.debugLogger.api(errorMsg, 'error');
                window.debugLogger.endGroup();
            } else {
                console.error(errorMsg);
            }
            return '';
        }

        for (let i = 0; i < urls.length; i++) {
            try {
                const url = urls[i].endsWith('/') ? urls[i] + 'health' : urls[i] + '/health';
                if (window.debugLogger) {
                    window.debugLogger.api(`Testing URL (${i + 1}/${urls.length}): ${url}`);
                } else {
                    console.log(`Testing URL: ${url}`);
                }

                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 3000);

                const startTime = performance.now();
                const response = await fetch(url, {
                    method: 'GET',
                    headers: { 'Accept': 'application/json' },
                    mode: 'cors',
                    cache: 'no-cache',
                    signal: controller.signal
                });
                const duration = (performance.now() - startTime).toFixed(2);

                clearTimeout(timeoutId);

                if (window.debugLogger) {
                    window.debugLogger.api(`Response for ${url} received in ${duration}ms - Status: ${response.status} ${response.statusText}`);
                }

                if (response.ok) {
                    if (window.debugLogger) {
                        window.debugLogger.api(`API available at: ${urls[i]}`, 'success');
                        window.debugLogger.endGroup();
                    } else {
                        console.log(`API available at: ${urls[i]}`);
                    }
                    return urls[i];
                } else {
                    if (window.debugLogger) {
                        window.debugLogger.api(`API returned error status: ${response.status} ${response.statusText}`, 'warn');
                    }
                }
            } catch (error) {
                if (error.name === 'AbortError') {
                    if (window.debugLogger) {
                        window.debugLogger.api(`API connection timeout: ${urls[i]}`, 'warn');
                    } else {
                        console.warn(`API connection timeout: ${urls[i]}`);
                    }
                } else {
                    if (window.debugLogger) {
                        window.debugLogger.api(`Failed to connect to ${urls[i]}: ${error.name} - ${error.message}`, 'error');
                    } else {
                        console.warn(`Failed to connect to ${urls[i]}:`, error);
                    }
                }
            }
        }

        const errorMsg = 'No working API endpoints found';
        if (window.debugLogger) {
            window.debugLogger.api(errorMsg, 'error');
            window.debugLogger.endGroup();
        } else {
            console.warn(errorMsg);
        }
        return '';
    }
};

// Log successful loading of this script
if (window.debugLogger) {
    window.debugLogger.pageLoad('login.js loaded successfully');
} else {
    console.log('login.js loaded successfully');
}

// Add diagnostic function specific to login
window.loginDiagnostics = {
    runTests: async function () {
        if (window.debugLogger) {
            window.debugLogger.startGroup('Login Component Diagnostics');
        } else {
            console.group('Login Component Diagnostics');
        }

        const log = (msg, level) => {
            if (window.debugLogger) {
                window.debugLogger.log(msg, 'login-diag', level);
            } else {
                console.log(msg);
            }
        };

        // 1. Check if the login page is rendered
        const loginCard = document.querySelector('.login-card');
        log(`Login card rendered: ${!!loginCard}`, loginCard ? 'info' : 'warn');

        // 2. Check API URLs
        const urls = [
            "https://localhost:5191/",
            "https://localhost:7235/",
            "https://localhost:5001/",
            "https://localhost:5002/"
        ];

        log(`Testing ${urls.length} API URLs`);

        for (const url of urls) {
            try {
                log(`Testing direct fetch to ${url}health...`);
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 3000);

                const response = await fetch(`${url}health`, {
                    method: 'GET',
                    signal: controller.signal
                });

                clearTimeout(timeoutId);

                log(`URL ${url} response: ${response.status} ${response.statusText}`,
                    response.ok ? 'success' : 'warn');

                if (response.ok) {
                    // Try to read response body
                    try {
                        const text = await response.text();
                        log(`Response body: ${text.substring(0, 100)}${text.length > 100 ? '...' : ''}`);
                    } catch (bodyErr) {
                        log(`Could not read response body: ${bodyErr.message}`, 'warn');
                    }
                }
            } catch (err) {
                log(`Error testing ${url}: ${err.name} - ${err.message}`, 'error');
            }
        }

        // 3. Check localStorage access
        try {
            log('Testing localStorage access...');
            localStorage.setItem('login-test', 'test-value');
            const testValue = localStorage.getItem('login-test');
            log(`localStorage test: ${testValue === 'test-value' ? 'passed' : 'failed'}`);
            localStorage.removeItem('login-test');
        } catch (storageErr) {
            log(`localStorage error: ${storageErr.message}`, 'error');
        }

        // 4. Check if Blazor is initialized
        log(`Blazor initialized: ${typeof window.Blazor !== 'undefined'}`);

        if (window.debugLogger) {
            window.debugLogger.endGroup();
        } else {
            console.groupEnd();
        }

        return "Diagnostics complete - check browser console for results";
    }
};
