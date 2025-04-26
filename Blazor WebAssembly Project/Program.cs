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
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5191/";
Console.WriteLine($"Using API base URL: {apiBaseUrl}");

// Configure HttpClient with auth token handling
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    return httpClient;
});

builder.Services.AddBlazoredLocalStorage();

// Add HTTP client factory with named client that includes auth token handler
builder.Services.AddHttpClient("AuthenticatedClient", async (sp, client) =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // Get the token from local storage and attach to request
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var token = await localStorage.GetItemAsStringAsync("authToken");

    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
});
// Replace the problematic EquipmentAPI client registration with this code:
builder.Services.AddHttpClient("EquipmentAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:5191/");
})
// Add a reference to the AuthTokenHandler
.AddHttpMessageHandler<AuthTokenHandler>();

// Register the AuthTokenHandler
builder.Services.AddTransient<AuthTokenHandler>();

// Register this as a transient service to avoid sharing state between components
builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("EquipmentAPI"));

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add authorization with more specific policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("WarehouseManagerPolicy", policy =>
        policy.RequireRole("WarehouseManager", "Admin"));

    options.AddPolicy("WarehouseOperativePolicy", policy =>
        policy.RequireRole("WarehouseOperative", "WarehouseManager", "Admin"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// Add client-side implementation of IEquipmentRepository
builder.Services.AddScoped<Domain_Project.Interfaces.IEquipmentRepository, ClientSideEquipmentRepository>();

// Add authentication handling services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Configure service for handling JSON Web Tokens
builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();

// Configure logging with browser-compatible approach
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Services.AddLogging(logging =>
{
    // Register our custom browser console logger
    logging.Services.AddScoped<ILoggerProvider, BrowserConsoleLoggerProvider>();

    // Don't use AddConsole() for WebAssembly - it's not supported in browser environments
});

// Register custom services
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentService,
    Blazor_WebAssembly.Services.Implementations.EquipmentService>(sp =>
    new EquipmentService(sp.GetRequiredService<HttpClient>()));

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentRequestService,
    EquipmentRequestService>();

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ICheckoutService,
    CheckoutService>(sp => new CheckoutService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<AuthenticationStateProvider>()));

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ITeamService,
    TeamService>(sp => new TeamService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IAuthService>(),
        sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IAuditLogService,
    AuditLogService>();

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IUserService,
    UserService>();

// Add custom handler for automatic token refresh (if needed)
builder.Services.AddScoped<AuthorizationMessageHandler>();

// Build the host
var host = builder.Build();

// Safe initialization of JS interop features
try
{
    var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
    var authService = host.Services.GetRequiredService<IAuthService>();

    // Initialize browser console logger with a safer approach
    try
    {
        // Check if the blazorConsoleLog object exists before invoking methods
        bool jsLoggerExists = await jsRuntime.InvokeAsync<bool>("eval",
            "typeof window !== 'undefined' && typeof window.blazorConsoleLog !== 'undefined'");

        if (jsLoggerExists)
        {
            await jsRuntime.InvokeVoidAsync("blazorConsoleLog.init");

            // Log authentication state via JS interop
            var isAuthenticated = await authService.IsAuthenticatedAsync();
            await jsRuntime.InvokeVoidAsync("blazorConsoleLog.log",
                $"Initial authentication state: {(isAuthenticated ? "Authenticated" : "Not authenticated")}");
        }
        else
        {
            // Fallback to basic console if custom logger isn't available
            await jsRuntime.InvokeVoidAsync("console.log", "Custom logger not available, using standard console");


            var isAuthenticated = await authService.IsAuthenticatedAsync();
            await jsRuntime.InvokeVoidAsync("console.log",
                $"Initial authentication state: {(isAuthenticated ? "Authenticated" : "Not authenticated")}");
        }
    }
    catch (JSException jsEx)
    {
        Console.WriteLine($"JavaScript initialization failed: {jsEx.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred during initialization: {ex.Message}");
}

// Run the application
await host.RunAsync();
