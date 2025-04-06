using Blazor_WebAssembly.Services;
using Blazor_WebAssembly.Services.Implementations;
using Blazor_WebAssembly.Services.Interfaces;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Blazor_WebAssembly.Configuration
{
    public static class ServiceRegistration
    {
        public static void ConfigureServices(WebAssemblyHostBuilder builder)
        {
            // HTTP Client Configuration
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7235/")
            });

            // Local Storage
            builder.Services.AddBlazoredLocalStorage();

            // Authentication and Authorization
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

            // Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IEquipmentRequestService, EquipmentRequestService>();

            // Navigation
            builder.Services.AddScoped<NavigationManager>();
        }
    }
}