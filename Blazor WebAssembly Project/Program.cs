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
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Blazor_WebAssembly.Repositories; // Add this for client repository implementation

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
// In Program.cs, ensure the correct base address is configured
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5191/")
});


// Add a client-side implementation of IEquipmentRepository
builder.Services.AddScoped<Domain_Project.Interfaces.IEquipmentRepository, ClientSideEquipmentRepository>();

// Register services with explicit namespaces to avoid ambiguity
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentService, Blazor_WebAssembly.Services.Implementations.EquipmentService>(sp =>
    new EquipmentService(
        sp.GetRequiredService<HttpClient>()));

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add authorization and authentication services
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("WarehouseManagerPolicy", policy =>
        policy.RequireRole("WarehouseManager", "Admin"));
});

// Add IJSRuntimeService registration
builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();

// Add Authentication State Provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register custom services with fully qualified names to avoid ambiguity
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentRequestService, EquipmentRequestService>();
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ICheckoutService, CheckoutService>();
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.ITeamService, TeamService>(sp =>
    new TeamService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IAuthService>(),
        sp.GetRequiredService<IJSRuntime>()));

builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IAuditLogService, AuditLogService>();
builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IUserService, UserService>();

// Initialize the BrowserConsoleLogger
var host = builder.Build();

try
{
    var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();

    // Attempt to initialize blazorConsoleLog
    Console.WriteLine("Initializing blazorConsoleLog...");
    await jsRuntime.InvokeVoidAsync("blazorConsoleLog.init");
    Console.WriteLine("blazorConsoleLog initialized successfully.");
}
catch (JSException jsEx)
{
    Console.WriteLine($"JavaScript initialization failed: {jsEx.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred during initialization: {ex.Message}");
}

await host.RunAsync();
