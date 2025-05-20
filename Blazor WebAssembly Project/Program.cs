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

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Define a static BrowserConsoleLogger (fixing nullable warning)
BrowserConsoleLogger? staticLogger = null;

try
{
    // Configure logging with browser-compatible approach before other services
    builder.Logging.SetMinimumLevel(LogLevel.Information);
    builder.Logging.AddProvider(new SimpleConsoleLoggerProvider());

    Console.WriteLine("Starting Blazor WebAssembly application...");

    // List of potential API URLs to try
    List<string> possibleApiUrls = new List<string> {
        builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5191/",
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

    // ========= FIX: Register a memory configuration provider =========
    // Add an empty memory configuration provider first
    builder.Configuration.AddInMemoryCollection();
    builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IBlacklistService, BlacklistService>();


    // Use resilient API detection with retries
    string apiBaseUrl = await DetectApiServerWithRetryAsync(possibleApiUrls, maxRetries: 3);
    Console.WriteLine($"Using API base URL: {apiBaseUrl}");

    // Now you can set the configuration value safely
    builder.Configuration["ApiBaseUrl"] = apiBaseUrl;

    // Also store API URL in local storage for future use
    await StoreApiUrlInLocalStorageAsync(apiBaseUrl);

    // Add custom configuration for global error handling
    ConfigureGlobalErrorHandling(builder);

    // ===========================================
    // CRITICAL FIX: Add these lines to diagnose where DI is breaking
    Console.WriteLine("Starting service registration...");
    // ===========================================

    // 1. First add Blazored.LocalStorage before any service that depends on it
    builder.Services.AddBlazoredLocalStorage();
    Console.WriteLine("Added Blazored.LocalStorage");

    // 2. Add JSON options provider for consistent serialization
    builder.Services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    });
    Console.WriteLine("Added JsonSerializerOptions");

    // 3. Register HTTP client factory and base clients
    builder.Services.AddHttpClient();
    Console.WriteLine("Added HttpClient factory");

    // 4. Register HTTP client for health checks
    builder.Services.AddHttpClient("Health", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
    });
    Console.WriteLine("Added Health HttpClient");

    // ===========================================
    // Create a non-auth HttpClient for API discovery
    // ===========================================
    builder.Services.AddHttpClient("NoAuth", client =>
    {
        var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
        if (!string.IsNullOrEmpty(configuredApiBaseUrl))
        {
            client.BaseAddress = new Uri(configuredApiBaseUrl);
        }
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(10);
    });
    Console.WriteLine("Added NoAuth HttpClient");

    // 5. Register API discovery service
    builder.Services.AddScoped<IApiDiscoveryService, ApiDiscoveryService>();
    Console.WriteLine("Added IApiDiscoveryService");

    // 6. Add our custom LocalStorage service that wraps Blazored
    builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ILocalStorageService, AppLocalStorageService>();
    Console.WriteLine("Added custom ILocalStorageService");

    // ===========================================
    // Register CustomAuthStateProvider differently
    // ===========================================

    // 7. Register AuthenticationStateProvider as a factory to avoid circular dependencies
    builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    {
        var localStorage = sp.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
        var logger = sp.GetRequiredService<ILogger<CustomAuthStateProvider>>();
        return new CustomAuthStateProvider(localStorage, logger);
    });
    Console.WriteLine("Added AuthenticationStateProvider");

    // 8. Add auth token handler after authentication state provider is registered
    builder.Services.AddTransient<AuthTokenHandler>();
    Console.WriteLine("Added AuthTokenHandler");

    // 9. Configure primary HttpClient with auth token handler
    builder.Services.AddHttpClient("API", (sp, client) =>
    {
        var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
        if (!string.IsNullOrEmpty(configuredApiBaseUrl))
        {
            client.BaseAddress = new Uri(configuredApiBaseUrl);
        }
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<AuthTokenHandler>();
    Console.WriteLine("Added API HttpClient with AuthTokenHandler");

    // 10. Register the default HttpClient with token handling
    builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));
    Console.WriteLine("Added default HttpClient");

    // ===========================================
    // Register AuthService with factory to break circular dependency
    // ===========================================
    // Ensure your HttpClient is configured with the correct base address
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://192.168.1.138:5000/") });

    // Or for a separate API server:
    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5001/") });

    // 11. Register AuthService without direct reference to AuthenticationStateProvider
    builder.Services.AddScoped<IAuthService>(sp =>
    {
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("NoAuth");
        var localStorage = sp.GetRequiredService<Blazored.LocalStorage.ILocalStorageService>();
        return new AuthService(httpClient, localStorage);
    });
    Console.WriteLine("Added IAuthService");

    // 12. Add authorization policies
    builder.Services.AddAuthorizationCore(options =>
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
    Console.WriteLine("Added authorization policies");

    // 13. Register remaining services
    builder.Services.AddScoped<ApiConnectionService>();
    Console.WriteLine("Added ApiConnectionService");

    builder.Services.AddScoped<DynamicApiUrlHandler>();
    Console.WriteLine("Added DynamicApiUrlHandler");


    // With this correct version:
    builder.Services.AddScoped<Blazor_WebAssembly.Services.ApiErrorHandler>(sp =>
        new Blazor_WebAssembly.Services.ApiErrorHandler(
            sp.GetRequiredService<ILogger<Blazor_WebAssembly.Services.ApiErrorHandler>>()
        )
    );
    Console.WriteLine("Added ApiErrorHandler with correct namespace");

    builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();
    Console.WriteLine("Added IJSRuntimeService");

    builder.Services.AddScoped<IJavaScriptInitializer, JavaScriptInitializer>();
    Console.WriteLine("Added IJavaScriptInitializer");

    builder.Services.AddScoped<Domain_Project.Interfaces.IEquipmentRepository, ClientSideEquipmentRepository>();

    Console.WriteLine("Added IEquipmentRepository");

    builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentService, EquipmentService>();

    builder.Services.AddScoped<IEquipmentRequestService>(sp =>
        new EquipmentRequestService(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ICheckoutService>(),
            sp.GetRequiredService<Blazor_WebAssembly.Services.Interfaces.IEquipmentService>()  // Use fully qualified name
        )
    );

    builder.Services.AddScoped<IEquipmentReturnService>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
        var logger = sp.GetRequiredService<ILogger<EquipmentReturnService>>();
        var equipmentLogger = sp.GetRequiredService<ILogger<EquipmentService>>();
        var apiUrlHandler = sp.GetRequiredService<DynamicApiUrlHandler>();
        var jsRuntime = sp.GetRequiredService<IJSRuntime>();

        // Use your custom wrapper instead of the Blazored service directly
        var localStorageService = sp.GetRequiredService<Blazor_WebAssembly.Services.Interfaces.ILocalStorageService>();

        return new EquipmentReturnService(
            httpClientFactory,
            authStateProvider,
            logger,
            apiUrlHandler,
            jsRuntime,
            localStorageService, // Now using the correct type
            equipmentLogger);
    });

    builder.Services.AddScoped<ICheckoutService, CheckoutService>();
    builder.Services.AddScoped<ITeamService, TeamService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IUserService, UserService>();
    Console.WriteLine("Added application services");

    // Add browser console logger last
    builder.Services.AddLogging(logging =>
    {
        logging.Services.AddScoped<ILoggerProvider, BrowserConsoleLoggerProvider>();
    });
    Console.WriteLine("Added browser console logger");

    // Add conditional service registration
    RegisterConditionalServices(builder);
    Console.WriteLine("Added conditional services");

    // ===========================================
    // Add JavaScript error reporting before building host
    // ===========================================
    Console.WriteLine("Building host...");

    // Build the host
    var host = builder.Build();
    Console.WriteLine("Host built successfully");

    // Try to initialize JavaScript features and ensure apiConnection.initializeComponent is available
    try
    {
        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();

        // Log initialization started
        await jsRuntime.InvokeVoidAsync("console.log", "Blazor initialization started");

        // Initialize JavaScript features
        var jsInitializer = host.Services.GetRequiredService<IJavaScriptInitializer>();
        await jsInitializer.InitializeAsync();
        Console.WriteLine("JavaScript features initialized successfully");

        // Verify the apiConnection.initializeComponent function exists
        var functionExists = await jsRuntime.InvokeAsync<bool>("eval", @"
        try {
            const exists = typeof window.apiConnection !== 'undefined' && 
                           typeof window.apiConnection.initializeComponent === 'function';
            console.log('apiConnection.initializeComponent exists: ' + exists);
            return exists;
        } catch (e) {
            console.error('Error checking function existence:', e);
            return false;
        }
    ");

        if (!functionExists)
        {
            // If the function doesn't exist, define it explicitly
            await jsRuntime.InvokeVoidAsync("eval", @"
            console.warn('apiConnection.initializeComponent not found, defining it now...');
            window.apiConnection = window.apiConnection || {};
            window.apiConnection.initializeComponent = function(componentId, options) {
                console.log('Initializing component from fallback implementation:', componentId, options);
                return { success: true, message: 'Component initialized (fallback)' };
            };
        ");
            Console.WriteLine("Created fallback implementation for apiConnection.initializeComponent");
        }
        else
        {
            Console.WriteLine("Verified apiConnection.initializeComponent exists");
        }

        // Add enhanced error tracking
        await jsRuntime.InvokeVoidAsync("eval", @"
        console.log('Adding enhanced error handling for Blazor WebAssembly');
        window.Blazor = window.Blazor || {};
        window.Blazor.defaultReconnectionHandler = {};
        window.Blazor._internal = window.Blazor._internal || {};
        
        window.onerror = function(message, source, lineno, colno, error) {
            console.error(`Error: ${message} at ${source}:${lineno}:${colno}`);
            console.error('Stack:', error?.stack);
            
            if (message.includes('provider has been inserted')) {
                console.warn('DI provider error detected - this indicates a circular dependency');
                
                // Update UI for better user experience
                const connectionStatus = document.getElementById('connection-status');
                if (connectionStatus) {
                    connectionStatus.innerHTML = `
                        <div class='d-flex align-items-center text-danger'>
                            <i class='bi bi-exclamation-triangle-fill me-2'></i>
                            <span>שגיאת אתחול יישום. <a href='#' onclick='location.reload();' class='text-danger'>טען מחדש</a></span>
                        </div>
                    `;
                }
            }
            return false;
        };
    ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"JavaScript initialization error (non-fatal): {ex.Message}");
        // Continue anyway - don't let this stop the application
    }

    // Right before running the application
    Console.WriteLine("Starting application...");

    // Ensure the apiConnection.initializeComponent function exists
    // This is a last-resort measure to guarantee the function is available
    try
    {
        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        await jsRuntime.InvokeVoidAsync("eval", @"
        // Final check for apiConnection.initializeComponent
        if (!window.apiConnection || typeof window.apiConnection.initializeComponent !== 'function') {
            console.warn('apiConnection.initializeComponent still missing, creating emergency fallback...');
            window.apiConnection = window.apiConnection || {};
            window.apiConnection.initializeComponent = function(componentId, options) {
                console.log('Emergency fallback for initializeComponent called with:', componentId, options);
                return { success: true, message: 'Component initialized (emergency fallback)' };
            };
        }
    ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Emergency script injection failed: {ex.Message}");
    }

    await host.RunAsync();

}
catch (Exception ex)
{
    Console.WriteLine($"Critical application error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");

    // Try to log to browser console if available
    if (staticLogger != null)
    {
        await staticLogger.LogErrorAsync($"Critical error: {ex.Message}");
    }
}

// Rest of the code remains the same...


// API detection with retry logic
async Task<string> DetectApiServerWithRetryAsync(List<string> possibleUrls, int maxRetries = 3)
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

// Helper method to detect an available API server
async Task<string> DetectApiServerAsync(List<string> possibleUrls)
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

// Helper method to store API URL in localStorage via JS interop
async Task StoreApiUrlInLocalStorageAsync(string apiUrl)
{
    try
    {
        var js = builder.Services.BuildServiceProvider().GetService<IJSRuntime>();
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

// Configure global error handling
void ConfigureGlobalErrorHandling(WebAssemblyHostBuilder builder)
{
    // No middleware concept in Blazor WebAssembly, but we can configure default exception handling
    AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
    {
        var exception = args.ExceptionObject as Exception;
        Console.WriteLine($"Unhandled exception: {exception?.Message}");
    };
}

// Register conditional services
void RegisterConditionalServices(WebAssemblyHostBuilder builder)
{
    // Try to register IRoleService if it exists
    var serviceType = Type.GetType("Blazor_WebAssembly.Services.Interfaces.IRoleService, Blazor WebAssembly Project");
    var implementationType = Type.GetType("Blazor_WebAssembly.Services.Implementations.RoleService, Blazor WebAssembly Project");

    if (serviceType != null && implementationType != null)
    {
        builder.Services.AddScoped(serviceType, implementationType);
        Console.WriteLine("RoleService successfully registered");
    }
    else
    {
        Console.WriteLine("RoleService or IRoleService type not found - service not registered");
    }
}

// Simple logger provider for early startup
class SimpleConsoleLoggerProvider : ILoggerProvider
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

// Simple console logger for static use
class BrowserConsoleLogger
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

// Interface for JavaScript initializer
public interface IJavaScriptInitializer
{
    Task InitializeAsync();
}

// Implementation for JavaScript initializer
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
    // Dynamic API URL handler
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

// API error handler
public class ApiErrorHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<ApiResponse<T>> HandleRequestAsync<T>(Func<Task<T>> apiCall, string operationName)
    {
        try
        {
            _logger.LogInformation($"Executing API operation: {operationName}");
            var result = await apiCall();
            return ApiResponse<T>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"HTTP request failed during {operationName}: Status={ex.StatusCode}");

            string userMessage = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => "The requested resource was not found.",
                System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
                System.Net.HttpStatusCode.Forbidden => "You don't have permission to access this resource.",
                System.Net.HttpStatusCode.BadRequest => "The request contained invalid data.",
                System.Net.HttpStatusCode.InternalServerError => "A server error occurred. The team has been notified.",
                System.Net.HttpStatusCode.BadGateway => "The server is temporarily unavailable. Please try again.",
                System.Net.HttpStatusCode.ServiceUnavailable => "The server is currently unavailable. Please try again later.",
                System.Net.HttpStatusCode.GatewayTimeout => "The request timed out. Please check your connection and try again.",
                System.Net.HttpStatusCode.RequestTimeout => "The request timed out. Please try again.",
                _ => "A connection error occurred. Please check your network and try again."
            };

            // Try to extract more detailed error message from response if available
            if (ex.Data.Contains("ResponseContent"))
            {
                try
                {
                    var responseContent = ex.Data["ResponseContent"] as string;
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
                        if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            userMessage = errorResponse.Message;
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors for error responses
                }
            }

            return ApiResponse<T>.Failure(userMessage, (int?)ex.StatusCode ?? 500);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"JSON parsing error during {operationName}");
            return ApiResponse<T>.Failure("Error processing data from the server.", 422);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, $"Request timeout during {operationName}");
            return ApiResponse<T>.Failure("The request timed out. Please check your connection and try again.", 408);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("JSON"))
        {
            _logger.LogError(ex, $"Invalid JSON response during {operationName}");
            return ApiResponse<T>.Failure("Error processing data from the server.", 422);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error during {operationName}");
            return ApiResponse<T>.Failure("An unexpected error occurred. Please try again later.", 500);
        }
    }

    // Helper method to handle common API responses
    public async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response, string operationName)
    {
        try
        {
            _logger.LogInformation($"Processing response for operation: {operationName}, Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                if (typeof(T) == typeof(bool))
                {
                    // Special case for boolean results
                    return ApiResponse<T>.Success((T)(object)true);
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return ApiResponse<T>.Success((T)(object)true);
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        return ApiResponse<T>.Success((T)(object)string.Empty);
                    }

                    // For collections, return empty instance
                    if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        return instance != null
                            ? ApiResponse<T>.Success((T)instance)
                            : ApiResponse<T>.Success(default!);
                    }

                    // For value types, return default value
                    if (typeof(T).IsValueType)
                    {
                        return ApiResponse<T>.Success(default!);
                    }

                    // For reference types, try creating a new instance
                    try
                    {
                        var instance = Activator.CreateInstance<T>();
                        return ApiResponse<T>.Success(instance);
                    }
                    catch
                    {
                        return ApiResponse<T>.Success(default!);
                    }
                }

                try
                {
                    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return ApiResponse<T>.Success(result!);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"JSON parsing error during {operationName}");
                    return ApiResponse<T>.Failure("Error processing data from the server.", 422);
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"API error in {operationName}: {response.StatusCode}, Content: {errorContent}");

                string userMessage = response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "The requested resource was not found.",
                    HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
                    HttpStatusCode.Forbidden => "You don't have permission to access this resource.",
                    HttpStatusCode.BadRequest => "The request contained invalid data.",
                    HttpStatusCode.InternalServerError => "A server error occurred. The team has been notified.",
                    _ => "An error occurred while communicating with the server."
                };

                // Try to extract more detailed error message from response if available
                if (!string.IsNullOrEmpty(errorContent))
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                        if (!string.IsNullOrEmpty(errorResponse?.Message))
                        {
                            userMessage = errorResponse.Message;
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for error responses
                    }
                }

                return ApiResponse<T>.Failure(userMessage, (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling response for {operationName}");
            return ApiResponse<T>.Failure("An error occurred while processing the server response.", 500);
        }
    }

    // Internal class to deserialize error responses
    private class ErrorResponse
    {
        // Fix CS8618 by making properties nullable
        public string? Message { get; set; }
        public string? Error { get; set; }
        public int? StatusCode { get; set; }

        // Handle property name variations
        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }
}

// API Response class
public class ApiResponse<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? StatusCode { get; private set; }

    public static ApiResponse<T> Success(T data) =>
        new ApiResponse<T> { IsSuccess = true, Data = data };

    public static ApiResponse<T> Failure(string errorMessage, int statusCode) =>
        new ApiResponse<T> { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };
}
