// File: wwwroot/js/apiConnection-component.js
(function () {
    // Ensure the apiConnection object exists
    window.apiConnection = window.apiConnection || {};

    // Define the initializeComponent function
    window.apiConnection.initializeComponent = function (componentId, options) {
        console.log(`[apiConnection] Initializing component: ${componentId}`, options);

        try {
            // Get the component element
            const component = document.getElementById(componentId);
            if (!component) {
                console.warn(`[apiConnection] Component not found: ${componentId}`);
                return { success: false, message: 'Component element not found' };
            }

            // Initialize the component
            component.classList.add('api-initialized');

            // Apply any options
            if (options && typeof options === 'object') {
                // Store options as data attribute
                component.dataset.apiOptions = JSON.stringify(options);

                // Apply styling options if provided
                if (options.styling) {
                    Object.entries(options.styling).forEach(([key, value]) => {
                        component.style[key] = value;
                    });
                }
            }

            // Return success
            return {
                success: true,
                message: 'Component initialized successfully',
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

    console.log('[apiConnection] Component initialization module loaded');
})();
