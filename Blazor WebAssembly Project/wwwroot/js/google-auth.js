// Function to handle Google Sign-In response
window.handleGoogleCredentialResponse = function(response) {
    if (window._dotNetHelper) {
        window._dotNetHelper.invokeMethodAsync('OnGoogleLogin', response.credential);
    }
};

// Function to initialize Google Sign-In
window.initializeGoogleSignIn = function(dotNetHelper) {
    window._dotNetHelper = dotNetHelper;
    
    // Initialize Google Sign-In
    google.accounts.id.initialize({
        client_id: "157978226290-21smsb9rka7244tf6jbe5k7bceaicfp6.apps.googleusercontent.com",
        callback: handleGoogleCredentialResponse,
        auto_select: false,
        cancel_on_tap_outside: true
    });

    // Render the button
    google.accounts.id.renderButton(
        document.getElementById("g_id_signin"), {
            type: "standard",
            theme: "outline",
            size: "large",
            text: "signin_with",
            shape: "rectangular",
            logo_alignment: "left",
            width: 300
        }
    );
};

window.googleAuthInterop = {
    initialize: function (clientId, callbackName) {
        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                console.log("Google credential response:", response);
                if (window._dotNetHelper) {
                    window._dotNetHelper.invokeMethodAsync(callbackName, response.credential);
                }
            },
            ux_mode: 'popup',
            context: 'signin',
            auto_select: false,
            cancel_on_tap_outside: true
        });
    },
    prompt: function () {
        window.google.accounts.id.prompt((notification) => {
            if (notification.isNotDisplayed() || notification.isSkippedMoment()) {
                console.log('Google One Tap was not displayed:', notification.getNotDisplayedReason());
            }
        });
    },
    renderButton: function (elementId) {
        window.google.accounts.id.renderButton(
            document.getElementById(elementId),
            { 
                theme: "outline", 
                size: "large",
                type: "standard",
                shape: "rectangular",
                text: "signin_with",
                logo_alignment: "left"
            }
        );
    },
    setDotNetHelper: function(dotNetHelper) {
        window._dotNetHelper = dotNetHelper;
    }
};
