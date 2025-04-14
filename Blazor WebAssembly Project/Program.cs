using Blazor_WebAssembly.Services;
using Blazor_WebAssembly.Services.Implementations;
using Blazor_WebAssembly.Services.Interfaces;
using Blazor_WebAssembly.Utilities.BrowserConsoleLogger;
using Blazor_WebAssembly_Project;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using System.Text;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["API_BaseAddress"] ?? "https://localhost:7230") });
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7235/") });

// Add Blazored.LocalStorage (DO NOT manually register ILocalStorageService)
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

// Register custom services
builder.Services.AddScoped<IEquipmentService, EquipmentService>();
builder.Services.AddScoped<IEquipmentRequestService, EquipmentRequestService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
//builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ITeamService, TeamService>(sp =>
    new TeamService(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<IAuthService>(),
        sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Build the application
//await builder.Build().RunAsync();

// Initialize the BrowserConsoleLogger
var host = builder.Build();
var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
BrowserConsoleLogger.Initialize(jsRuntime);
Console.SetOut(new CustomConsoleWriter(jsRuntime));

await host.RunAsync();
