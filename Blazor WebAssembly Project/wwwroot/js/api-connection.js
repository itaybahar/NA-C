// API connection helper
window.checkApiConnection = async function (dotNetRef) {
    // List of possible API URLs
    const urls = [
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

    console.log("Starting API connection check...");

    // First try cached URL if available
    const cachedUrl = localStorage.getItem('api_baseUrl');

    if (cachedUrl) {
        try {
            console.log("Checking cached API URL: " + cachedUrl);
            const normalizedUrl = cachedUrl.endsWith('/') ? cachedUrl : cachedUrl + '/';

            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000);

            const response = await fetch(normalizedUrl + 'health', {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                mode: 'cors',
                cache: 'no-cache',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                console.log("Cached API URL is valid: " + cachedUrl);
                await dotNetRef.invokeMethodAsync('OnApiConnectionSuccess');
                return;
            }
        } catch (error) {
            console.warn("Cached API URL failed, trying alternatives:", error);
        }
    }

    // Try all possible URLs
    for (const baseUrl of urls) {
        try {
            console.log("Testing API URL: " + baseUrl);
            const normalizedUrl = baseUrl.endsWith('/') ? baseUrl : baseUrl + '/';

            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 3000);

            const response = await fetch(normalizedUrl + 'health', {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                mode: 'cors',
                cache: 'no-cache',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                console.log("API found at: " + baseUrl);
                localStorage.setItem('api_baseUrl', baseUrl);
                await dotNetRef.invokeMethodAsync('OnApiConnectionSuccess');
                return;
            }
        } catch (error) {
            console.log("Failed to connect to " + baseUrl, error.name === "AbortError" ? "timeout" : error.message);
        }
    }

    // If we get here, all connection attempts failed
    console.error("Failed to connect to API after trying all URLs");
    await dotNetRef.invokeMethodAsync('OnApiConnectionError');
};

// Monitor network status
window.addEventListener('online', function () {
    console.log("Browser went online - checking API connection");
    const mainLayout = document.querySelector('app');
    if (mainLayout && window.DotNet) {
        window.checkApiConnection(window.DotNet.getDotNetObject('MainLayout'));
    }
});
