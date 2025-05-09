// register.js - JavaScript functions for the registration page

// Handle ESC key for password requirements modal
window.addEscKeyListener = function (dotNetRef) {
    // Store the handler function to be able to remove it later
    window.escKeyHandler = function (event) {
        if (event.key === 'Escape') {
            dotNetRef.invokeMethodAsync('ClosePasswordRequirements');
        }
    };

    // Add the event listener
    document.addEventListener('keydown', window.escKeyHandler);
    console.log("ESC key listener added for password requirements");
};

window.removeEscKeyListener = function () {
    if (window.escKeyHandler) {
        document.removeEventListener('keydown', window.escKeyHandler);
        console.log("ESC key listener removed");
    }
};

// Dynamic API discovery and Google authentication redirect
window.handleGoogleAuthRedirect = async function () {
    try {
        // Check if we have cached API URL in localStorage
        let apiBaseUrl = localStorage.getItem('api_baseUrl');

        if (!apiBaseUrl) {
            console.log("No cached API URL found, discovering API...");

            // Try a list of possible API URLs
            const possibleUrls = [
                "https://localhost:5191/",
                "https://localhost:7235/",
                "https://localhost:5001/",
                "https://localhost:5002/",
                // Add more possible URLs if needed
            ];

            // Use the testApiConnectivity function from index.html
            apiBaseUrl = await window.testApiConnectivity(possibleUrls);

            if (!apiBaseUrl) {
                console.warn("API discovery failed, using fallback URL");
                apiBaseUrl = "https://localhost:5191/"; // Fallback URL
            } else {
                console.log(`API discovered at: ${apiBaseUrl}`);
                // Cache the discovered URL
                localStorage.setItem('api_baseUrl', apiBaseUrl);
            }
        } else {
            console.log(`Using cached API URL: ${apiBaseUrl}`);
        }

        // Store current location as return URL
        localStorage.setItem('returnUrl', window.location.href);

        // Build and navigate to Google auth URL
        const googleAuthUrl = `${apiBaseUrl.trim().replace(/\/$/, '')}/auth/login-google`;
        console.log(`Redirecting to Google auth: ${googleAuthUrl}`);

        // Redirect to the Google authentication endpoint
        window.location.href = googleAuthUrl;
        return true;
    } catch (error) {
        console.error("Error in Google authentication redirect:", error);
        return { error: error.message };
    }
};

// Enhanced form validation helpers
window.formValidation = {
    validateEmail: function (email) {
        const regex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
        return regex.test(email);
    },

    validatePassword: function (password) {
        return {
            hasMinLength: password && password.length >= 6,
            hasUpperCase: /[A-Z]/.test(password),
            hasNumber: /\d/.test(password),
            hasSpecialChar: /[^A-Za-z0-9]/.test(password)
        };
    },

    isUsernameAvailable: async function (username, apiUrl) {
        try {
            if (!username || username.length < 3) return false;

            // Use the cached API URL or fallback
            const baseUrl = apiUrl || localStorage.getItem('api_baseUrl') || "https://localhost:5191/";

            // Check username availability via API
            const response = await fetch(`${baseUrl.trim().replace(/\/$/, '')}/api/user/check-username?username=${encodeURIComponent(username)}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) return false;

            const result = await response.json();
            return result.available === true;
        } catch (error) {
            console.error("Error checking username availability:", error);
            return false;
        }
    }
};
