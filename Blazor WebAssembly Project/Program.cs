using Blazor_WebAssembly.Auth;
using Blazor_WebAssembly.Repositories;
using Blazor_WebAssembly.Services;
using Blazor_WebAssembly.Services.Implementations;
using Blazor_WebAssembly.Services.Interfaces;
using Blazor_WebAssembly.Utilities.BrowserConsoleLogger;
using Blazor_WebAssembly_Project;
using Blazored.LocalStorage;
using Domain_Project.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor_WebAssembly_Project
{
    public class Program
    {
        private static WebAssemblyHostBuilder _builder;
        private static BrowserConsoleLogger _staticLogger;

        public static async Task Main(string[] args)
        {
            _builder = WebAssemblyHostBuilder.CreateDefault(args);
            _builder.RootComponents.Add<App>("#app");
            _builder.RootComponents.Add<HeadOutlet>("head::after");

            try
            {
                await ConfigureServices();
                await _builder.Build().RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical application error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (_staticLogger != null)
                {
                    await _staticLogger.LogErrorAsync($"Critical error: {ex.Message}");
                }
            }
        }

        private static async Task ConfigureServices()
        {
            // Configure logging with browser-compatible approach before other services
            _builder.Logging.SetMinimumLevel(LogLevel.Information);
            _builder.Logging.AddProvider(new SimpleConsoleLoggerProvider());

            Console.WriteLine("Starting Blazor WebAssembly application...");

            // List of potential API URLs to try
            List<string> possibleApiUrls = new List<string> {
                _builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5191/",
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
            };

            // Add an empty memory configuration provider first
            _builder.Configuration.AddInMemoryCollection();
            _builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IBlacklistService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                return new Blazor_WebAssembly.Services.Implementations.BlacklistService(httpClient);
            });

            // Use resilient API detection with retries
            string apiBaseUrl = await DetectApiServerWithRetryAsync(possibleApiUrls);
            _builder.Configuration["ApiBaseUrl"] = apiBaseUrl;

            // Store API URL in local storage for future use
            await StoreApiUrlInLocalStorageAsync(apiBaseUrl);

            // Configure services
            ConfigureAuthServices();
            ConfigureHttpClients(apiBaseUrl);
            ConfigureApplicationServices();
            ConfigureLogging();
        }

        private static void ConfigureAuthServices()
        {
            _builder.Services.AddBlazoredLocalStorage();
            _builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            {
                var localStorage = sp.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
                var logger = sp.GetRequiredService<ILogger<CustomAuthStateProvider>>();
                return new CustomAuthStateProvider(localStorage, logger);
            });
            _builder.Services.AddTransient<AuthTokenHandler>();
            _builder.Services.AddAuthorizationCore(options =>
            {
                options.AddPolicy("WarehouseManagerPolicy", policy =>
                    policy.RequireRole("WarehouseManager", "Admin"));
                options.AddPolicy("WarehouseOperativePolicy", policy =>
                    policy.RequireRole("WarehouseOperative", "WarehouseManager", "Admin"));
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                options.AddPolicy("UserPolicy", policy =>
                    policy.RequireRole("User", "WarehouseOperative", "WarehouseManager", "Admin"));
            });
        }

        private static void ConfigureHttpClients(string apiBaseUrl)
        {
            _builder.Services.AddHttpClient();
            _builder.Services.AddHttpClient("Health", client => client.Timeout = TimeSpan.FromSeconds(5));
            _builder.Services.AddHttpClient("NoAuth", client =>
            {
                if (!string.IsNullOrEmpty(apiBaseUrl))
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                }
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            _builder.Services.AddHttpClient("API", (sp, client) =>
            {
                if (!string.IsNullOrEmpty(apiBaseUrl))
                {
                    client.BaseAddress = new Uri(apiBaseUrl);
                }
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            }).AddHttpMessageHandler<AuthTokenHandler>();
        }

        private static void ConfigureApplicationServices()
        {
            _builder.Services.AddScoped<IApiDiscoveryService, ApiDiscoveryService>();
            _builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ILocalStorageService, Blazor_WebAssembly.Services.Implementations.AppLocalStorageService>();
            _builder.Services.AddScoped<IAuthService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("NoAuth");
                var localStorage = sp.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
                return new Blazor_WebAssembly.Services.Implementations.AuthService(httpClient, localStorage);
            });
            _builder.Services.AddScoped<Domain_Project.Interfaces.IEquipmentRepository, ClientSideEquipmentRepository>();
            _builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentService, Blazor_WebAssembly.Services.Implementations.EquipmentService>();
            _builder.Services.AddScoped<IEquipmentRequestService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var checkoutService = sp.GetRequiredService<ICheckoutService>();
                var equipmentService = sp.GetRequiredService<Blazor_WebAssembly.Services.Interfaces.IEquipmentService>();
                return new EquipmentRequestService(httpClient, checkoutService, equipmentService);
            });
            _builder.Services.AddScoped<IEquipmentReturnService, EquipmentReturnService>();
            _builder.Services.AddScoped<ICheckoutService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                return new CheckoutService(httpClient, authStateProvider, httpClientFactory);
            });
            _builder.Services.AddScoped<ITeamService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var authService = sp.GetRequiredService<IAuthService>();
                var jsRuntime = sp.GetRequiredService<IJSRuntime>();
                return new TeamService(httpClient, authService, jsRuntime);
            });
            _builder.Services.AddScoped<IAuditLogService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var logger = sp.GetRequiredService<ILogger<AuditLogService>>();
                return new AuditLogService(httpClient, logger);
            });
            _builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IUserService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                return new Blazor_WebAssembly.Services.UserService(httpClient);
            });
            _builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();
            _builder.Services.AddScoped<IJavaScriptInitializer, JavaScriptInitializer>();
            
            // Add missing ApiErrorHandler service
            _builder.Services.AddScoped<Blazor_WebAssembly.Services.Api.ApiErrorHandler>();
        }

        private static void ConfigureLogging()
        {
            _builder.Services.AddLogging(logging =>
            {
                logging.Services.AddScoped<ILoggerProvider, BrowserConsoleLoggerProvider>();
            });
        }

        private static async Task<string> DetectApiServerWithRetryAsync(List<string> possibleUrls, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var result = await DetectApiServerAsync(possibleUrls);

                    // Verify the API is actually responding
                    using var httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(5),
                        BaseAddress = new Uri(result)
                    };

                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    var healthResponse = await httpClient.GetAsync("health");
                    if (healthResponse.IsSuccessStatusCode)
                    {
                        return result;
                    }

                    Console.WriteLine($"API detected but health check failed (attempt {attempt + 1}): {healthResponse.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API detection failed (attempt {attempt + 1}): {ex.Message}");

                    // Add delay between retries with exponential backoff
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay((int)Math.Pow(2, attempt) * 500);
                    }
                }
            }

            // If we exhausted all retries, return the first URL as fallback
            Console.WriteLine($"API detection failed after {maxRetries} attempts. Using default fallback.");
            return possibleUrls[0];
        }

        private static async Task<string> DetectApiServerAsync(List<string> possibleUrls)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(3);

            // Allow self-signed certificates for development
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Try health endpoint first for each URL
            foreach (var url in possibleUrls)
            {
                try
                {
                    Console.WriteLine($"Trying API URL: {url}");

                    // Add a query string parameter to avoid caching
                    var testUrl = url.TrimEnd('/') + "/health?timestamp=" + DateTime.Now.Ticks;
                    var response = await httpClient.GetAsync(testUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"API server found at: {url}");
                        return url;
                    }

                    Console.WriteLine($"API server at {url} returned {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not connect to {url}: {ex.Message}");
                }
            }

            // If health endpoint failed, try server-info endpoint
            foreach (var url in possibleUrls)
            {
                try
                {
                    var testUrl = url.TrimEnd('/') + "/api/server-info/ports?timestamp=" + DateTime.Now.Ticks;
                    var response = await httpClient.GetAsync(testUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"API server found via server-info at: {url}");
                        return url;
                    }
                }
                catch
                {
                    // Ignore errors and continue to the next URL
                }
            }

            // If no server responded successfully, return the first URL as fallback
            Console.WriteLine($"No API server detected, using fallback: {possibleUrls[0]}");
            return possibleUrls[0];
        }

        private static async Task StoreApiUrlInLocalStorageAsync(string apiUrl)
        {
            try
            {
                var js = _builder.Services.BuildServiceProvider().GetService<IJSRuntime>();
                if (js != null)
                {
                    await js.InvokeVoidAsync("localStorage.setItem", "api_baseUrl", apiUrl);
                    Console.WriteLine("Stored API URL in localStorage");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to store API URL in localStorage: {ex.Message}");
            }
        }
    }

    // Move all your classes here
    public class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleConsoleLogger(categoryName);
        }

        public void Dispose() { }

        private class SimpleConsoleLogger : ILogger
        {
            private readonly string _categoryName;

            public SimpleConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            // Fix CS8633 by using explicit interface implementation
            IDisposable ILogger.BeginScope<TState>(TState state) => default!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Console.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            }
        }
    }

    public class BrowserConsoleLogger
    {
        private readonly IJSRuntime _jsRuntime;

        public BrowserConsoleLogger(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task LogAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.log", message);
            }
            catch { /* Ignore errors */ }
        }

        public async Task LogWarningAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.warn", message);
            }
            catch { /* Ignore errors */ }
        }

        public async Task LogErrorAsync(string message)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("blazorConsoleLog.error", message);
            }
            catch { /* Ignore errors */ }
        }
    }

    public interface IJavaScriptInitializer
    {
        Task InitializeAsync();
    }

    public class JavaScriptInitializer : IJavaScriptInitializer
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<JavaScriptInitializer> _logger;

        public JavaScriptInitializer(IJSRuntime jsRuntime, ILogger<JavaScriptInitializer> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Initialize API connection handler and other JS functions
                await _jsRuntime.InvokeVoidAsync("eval", GetJavaScriptInitialization());
                _logger.LogInformation("JavaScript features initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize JavaScript features");
            }
        }

        private string GetJavaScriptInitialization()
        {
            return @"
    // Ensure apiConnection object exists
    window.apiConnection = window.apiConnection || {};
    
    // Only define initializeComponent if it doesn't already exist
    if (typeof window.apiConnection.initializeComponent !== 'function') {
        console.log('Adding apiConnection.initializeComponent function');
        window.apiConnection.initializeComponent = function(componentId, options) {
            console.log('Initializing component with ID:', componentId, 'Options:', options);
            
            // Return a success result to the component
            return {
                success: true,
                message: 'Component initialized successfully'
            };
        };
    }
    
    // Ensure the getBaseUrl function exists
    if (typeof window.apiConnection.getBaseUrl !== 'function') {
        window.apiConnection.getBaseUrl = function() {
            return localStorage.getItem('api_baseUrl') || 'https://localhost:5191/';
        };
    }
    
    // Ensure the discoverApi function exists
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
            
            // Try cached URL first
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
            
            // Try each possible URL
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

    // Initialize API error handling
    window.apiErrorHandler = window.apiErrorHandler || {
        handleError: function(error, componentId) {
            console.error('API Error:', error);
            const component = document.getElementById(componentId);
            if (component) {
                // Add error UI indicator
                component.classList.add('api-error');
                
                // Create error message element if it doesn't exist
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
    
    // Add CSS for error states
    const style = document.createElement('style');
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
    
    console.log('API connection and error handling initialized');
    ";
        }
    }

    public class DynamicApiUrlHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<DynamicApiUrlHandler> _logger;
        private string _discoveredApiUrl = string.Empty; // Fix CS8618 by initializing

        public DynamicApiUrlHandler(
            IHttpClientFactory httpClientFactory,
            IJSRuntime jsRuntime,
            ILogger<DynamicApiUrlHandler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task<HttpClient> GetClientWithDiscoveredApiAsync()
        {
            if (string.IsNullOrEmpty(_discoveredApiUrl))
            {
                try
                {
                    // Try to get API URL from JS
                    _discoveredApiUrl = await _jsRuntime.InvokeAsync<string>("apiConnection.getBaseUrl");
                    _logger.LogInformation($"Retrieved API URL from JS: {_discoveredApiUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to get API URL from JS: {ex.Message}");
                    _discoveredApiUrl = "https://localhost:5191/"; // Fallback
                }
            }

            var client = _httpClientFactory.CreateClient("API");

            // Ensure base address is set
            if (client.BaseAddress == null || client.BaseAddress.ToString() != _discoveredApiUrl)
            {
                client.BaseAddress = new Uri(_discoveredApiUrl);
                _logger.LogInformation($"Updated client base address to: {_discoveredApiUrl}");
            }

            return client;
        }
    }
}
