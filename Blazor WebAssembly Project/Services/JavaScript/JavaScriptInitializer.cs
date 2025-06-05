using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.JavaScript
{
    public class JavaScriptInitializer : IJavaScriptInitializer
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<JavaScriptInitializer> _logger;

        public JavaScriptInitializer(IJSRuntime jsRuntime, ILogger<JavaScriptInitializer> logger)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("eval", GetJavaScriptInitialization());
                _logger.LogInformation("JavaScript features initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize JavaScript features");
                throw;
            }
        }

        private static string GetJavaScriptInitialization()
        {
            return @"
    window.apiConnection = window.apiConnection || {};
    
    if (typeof window.apiConnection.initializeComponent !== 'function') {
        console.log('Adding apiConnection.initializeComponent function');
        window.apiConnection.initializeComponent = function(componentId, options) {
            console.log('Initializing component with ID:', componentId, 'Options:', options);
            return {
                success: true,
                message: 'Component initialized successfully'
            };
        };
    }
    
    if (typeof window.apiConnection.getBaseUrl !== 'function') {
        window.apiConnection.getBaseUrl = function() {
            return localStorage.getItem('api_baseUrl') || 'https://localhost:5191/';
        };
    }
    
    if (typeof window.apiConnection.discoverApi !== 'function') {
        window.apiConnection.discoverApi = async function() {
            console.log('Starting API discovery process');
            const possibleBaseUrls = [
                window.location.origin,
                'https://localhost:5191/',
                'https://localhost:7235/',
                'https://localhost:5001/',
                'https://localhost:5002/',
                'https://localhost:7001/',
                'https://localhost:7002/',
                'https://localhost:7176/',
                'https://localhost:7177/',
                'https://localhost:7178/',
                'https://localhost:7179/',
                'https://localhost:7180/'
            ];
            
            const cachedUrl = localStorage.getItem('api_baseUrl');
            if (cachedUrl) {
                try {
                    const response = await fetch(cachedUrl + 'health', {
                        method: 'GET',
                        headers: { 'Accept': 'application/json' },
                        mode: 'cors',
                        cache: 'no-cache'
                    });
                    
                    if (response.ok) {
                        console.log('Using cached API URL: ' + cachedUrl);
                        return cachedUrl;
                    }
                } catch (e) {
                    console.log('Cached API URL failed, trying alternatives');
                }
            }
            
            for (const baseUrl of possibleBaseUrls) {
                try {
                    const normalizedUrl = baseUrl.endsWith('/') ? baseUrl : baseUrl + '/';
                    const response = await fetch(normalizedUrl + 'health', {
                        method: 'GET',
                        headers: { 'Accept': 'application/json' },
                        mode: 'cors',
                        cache: 'no-cache'
                    });
                    
                    if (response.ok) {
                        console.log('API found at: ' + normalizedUrl);
                        localStorage.setItem('api_baseUrl', normalizedUrl);
                        return normalizedUrl;
                    }
                } catch (e) {
                    console.log('Failed to connect to ' + baseUrl);
                }
            }
            
            console.error('API discovery failed - no working endpoints found');
            return null;
        };
    }

    window.apiErrorHandler = window.apiErrorHandler || {
        handleError: function(error, componentId) {
            console.error('API Error:', error);
            const component = document.getElementById(componentId);
            if (component) {
                component.classList.add('api-error');
                let errorMsg = component.querySelector('.api-error-message');
                if (!errorMsg) {
                    errorMsg = document.createElement('div');
                    errorMsg.className = 'api-error-message';
                    component.appendChild(errorMsg);
                }
                errorMsg.textContent = error.message || 'An error occurred while communicating with the server';
            }
        },
        clearError: function(componentId) {
            const component = document.getElementById(componentId);
            if (component) {
                component.classList.remove('api-error');
                const errorMsg = component.querySelector('.api-error-message');
                if (errorMsg) {
                    errorMsg.remove();
                }
            }
        }
    };
    
    if (!document.querySelector('#api-error-styles')) {
        const style = document.createElement('style');
        style.id = 'api-error-styles';
        style.textContent = `
            .api-error {
                position: relative;
                border: 1px solid #dc3545;
                border-radius: 4px;
                padding: 10px;
                margin-bottom: 1rem;
                background-color: rgba(220, 53, 69, 0.1);
            }
            
            .api-error-message {
                color: #dc3545;
                padding: 8px;
                margin-top: 10px;
                background-color: rgba(220, 53, 69, 0.05);
                border-left: 3px solid #dc3545;
                font-weight: 500;
            }
            
            .api-loading::after {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background-color: rgba(0, 0, 0, 0.3);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 1000;
            }
            
            .api-initialized {
                position: relative;
            }
        `;
        document.head.appendChild(style);
    }
    
    console.log('API connection and error handling initialized');";
        }
    }
} 