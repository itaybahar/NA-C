// Define the blazorConsoleLog object
window.blazorConsoleLog = {
    // Initialize the logger
    init: function () {
        console.log("blazorConsoleLog initialized successfully.");
    },

    // Log a message to the console
    log: function (message) {
        console.log("Blazor Log: " + message);
    },

    // Log a warning to the console
    warn: function (message) {
        console.warn("Blazor Warning: " + message);
    },

    // Log an error to the console
    error: function (message) {
        console.error("Blazor Error: " + message);
    },

    // Log a message to a remote server
    logToServer: function (message, logLevel = "Info") {
        const logEntry = {
            timestamp: new Date().toISOString(),
            level: logLevel,
            message: message
        };

        // Replace '/api/logs' with your server's logging endpoint
        fetch('/api/logs', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(logEntry)
        })
            .then(response => {
                if (response.ok) {
                    console.log("Log sent to server successfully.");
                } else {
                    console.warn("Failed to send log to server. Status: " + response.status);
                }
            })
            .catch(error => {
                console.error("Error sending log to server: " + error);
            });
    }
};
