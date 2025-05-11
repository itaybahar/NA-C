// File: wwwroot/js/apiConnection-component.js
(function () {
    // Ensure the apiConnection object exists
    window.apiConnection = window.apiConnection || {};

    // Store API connection state
    const state = {
        baseUrl: null,
        connectionAttempts: 0,
        maxAttempts: 5,
        isConnecting: false,
        isConnected: false,
        dotNetRefs: {}
    };

    // Common API endpoints to try
    const commonEndpoints = [
        window.location.origin + "/",
        "https://localhost:5191/",
        "https://localhost:7235/",
        "https://localhost:5001/",
        "https://localhost:7001/",
        "https://localhost:7202/"
    ];

    // Initialize the component and check API connection
    window.apiConnection.initializeComponent = function (componentId, dotNetRef) {
        console.log(`[apiConnection] Initializing component: ${componentId}`);

        try {
            // Get the component element
            const component = document.getElementById(componentId);
            if (!component) {
                console.warn(`[apiConnection] Component not found: ${componentId}`);
                return { success: false, message: 'Component element not found' };
            }

            // Initialize the component
            component.classList.add('api-initialized');

            // Store the dotNetRef for this component
            if (dotNetRef) {
                state.dotNetRefs[componentId] = dotNetRef;
            }

            // Check if we're already connected or connecting
            if (state.isConnected) {
                console.log(`[apiConnection] Already connected to API at: ${state.baseUrl}`);
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnApiConnected', state.baseUrl);
                }
                return {
                    success: true,
                    message: 'Already connected to API',
                    baseUrl: state.baseUrl
                };
            } else if (state.isConnecting) {
                console.log(`[apiConnection] Already attempting to connect to API`);
                return {
                    success: true,
                    message: 'Connection attempt in progress',
                };
            }

            // Start connection process
            state.isConnecting = true;

            // First try to get from localStorage
            const cachedUrl = localStorage.getItem('api_baseUrl');
            if (cachedUrl) {
                console.log(`[apiConnection] Trying cached API URL: ${cachedUrl}`);
                checkApiEndpoint(cachedUrl, dotNetRef);
            } else {
                // Try common development endpoints
                console.log(`[apiConnection] No cached URL found. Trying common endpoints.`);
                tryNextEndpoint(0, dotNetRef);
            }

            // Return success for initialization (connection result will come later)
            return {
                success: true,
                message: 'Component initialized and connection process started',
                componentId: componentId
            };
        } catch (error) {
            console.error(`[apiConnection] Error initializing component: ${error.message}`);
            return {
                success: false,
                message: `Error: ${error.message}`,
                componentId: componentId
            };
        }
    };

    // Try endpoints one by one
    function tryNextEndpoint(index, dotNetRef) {
        if (index >= commonEndpoints.length) {
            console.error(`[apiConnection] All API endpoints failed after ${state.connectionAttempts} attempts`);
            state.isConnecting = false;

            // Notify all components about the connection failure
            Object.values(state.dotNetRefs).forEach(ref => {
                try {
                    ref.invokeMethodAsync('OnApiConnectionFailed');
                } catch (e) {
                    console.error(`[apiConnection] Error notifying component of failure: ${e.message}`);
                }
            });

            return;
        }

        const endpoint = commonEndpoints[index];
        console.log(`[apiConnection] Trying endpoint ${index + 1}/${commonEndpoints.length}: ${endpoint}`);

        checkApiEndpoint(endpoint, dotNetRef, () => {
            // On failure, try the next endpoint
            tryNextEndpoint(index + 1, dotNetRef);
        });
    }

    // Check if an API endpoint is responsive
    function checkApiEndpoint(url, dotNetRef, onFailCallback) {
        state.connectionAttempts++;
        console.log(`[apiConnection] Checking API endpoint (Attempt ${state.connectionAttempts}): ${url}`);

        // Create a test request with a timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout

        // First try the health endpoint
        fetch(url + "health", {
            method: 'GET',
            signal: controller.signal,
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache',
                'Expires': '0'
            }
        })
            .then(response => {
                clearTimeout(timeoutId);
                if (response.ok) {
                    // API is connected via health endpoint
                    handleSuccessfulConnection(url, dotNetRef);
                    return;
                }

                // Try alternative endpoint
                tryAlternativeEndpoint(url, dotNetRef, onFailCallback);
            })
            .catch(error => {
                clearTimeout(timeoutId);
                console.warn(`[apiConnection] Health endpoint check failed for ${url}: ${error.message}`);

                // Try alternative endpoint
                tryAlternativeEndpoint(url, dotNetRef, onFailCallback);
            });
    }

    // Try an alternative endpoint if the health check fails
    function tryAlternativeEndpoint(baseUrl, dotNetRef, onFailCallback) {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);

        // Try the equipment API endpoint
        fetch(baseUrl + "api/equipment", {
            method: 'GET',
            signal: controller.signal,
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache',
                'Expires': '0'
            }
        })
            .then(response => {
                clearTimeout(timeoutId);
                if (response.ok) {
                    // API is connected via equipment endpoint
                    handleSuccessfulConnection(baseUrl, dotNetRef);
                    return;
                }

                // Try server-info endpoint as final attempt
                tryServerInfoEndpoint(baseUrl, dotNetRef, onFailCallback);
            })
            .catch(error => {
                clearTimeout(timeoutId);
                console.warn(`[apiConnection] Equipment API check failed for ${baseUrl}: ${error.message}`);

                // Try server-info endpoint as final attempt
                tryServerInfoEndpoint(baseUrl, dotNetRef, onFailCallback);
            });
    }

    // Try the server-info endpoint as a final attempt
    function tryServerInfoEndpoint(baseUrl, dotNetRef, onFailCallback) {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);

        fetch(baseUrl + "api/server-info/ports", {
            method: 'GET',
            signal: controller.signal,
            headers: {
                'Cache-Control': 'no-cache, no-store, must-revalidate',
                'Pragma': 'no-cache',
                'Expires': '0'
            }
        })
            .then(response => {
                clearTimeout(timeoutId);
                if (response.ok) {
                    // API is connected via server-info endpoint
                    handleSuccessfulConnection(baseUrl, dotNetRef);
                    return;
                }

                // All attempts for this endpoint failed
                console.warn(`[apiConnection] All endpoints failed for base URL: ${baseUrl}`);
                if (onFailCallback) onFailCallback();
            })
            .catch(error => {
                clearTimeout(timeoutId);
                console.warn(`[apiConnection] Server info check failed for ${baseUrl}: ${error.message}`);

                // All attempts for this endpoint failed
                if (onFailCallback) onFailCallback();
            });
    }

    // Handle successful API connection
    function handleSuccessfulConnection(url, dotNetRef) {
        // Ensure the URL ends with a slash
        if (!url.endsWith('/')) {
            url += '/';
        }

        state.baseUrl = url;
        state.isConnected = true;
        state.isConnecting = false;

        // Store in localStorage for future use
        localStorage.setItem('api_baseUrl', url);
        console.log(`[apiConnection] API connected successfully at: ${url}`);

        // Notify all registered components about the connection
        Object.values(state.dotNetRefs).forEach(ref => {
            try {
                ref.invokeMethodAsync('OnApiConnected', url);
            } catch (e) {
                console.error(`[apiConnection] Error notifying component of success: ${e.message}`);
            }
        });
    }

    // Get the API base URL for direct access
    window.apiConnection.getApiBaseUrl = function () {
        return state.baseUrl;
    };

    // Check if API is connected
    window.apiConnection.isConnected = function () {
        return state.isConnected;
    };

    // Reset the connection state and try again
    window.apiConnection.resetConnection = function (dotNetRef) {
        console.log(`[apiConnection] Resetting connection state`);
        state.isConnected = false;
        state.isConnecting = false;
        state.connectionAttempts = 0;

        // Start connection process again
        window.apiConnection.initializeComponent('equipmentListContainer', dotNetRef);
    };

    console.log('[apiConnection] Component initialization module loaded');
})();
