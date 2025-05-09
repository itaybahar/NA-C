using Blazor_WebAssembly.Auth;
using Blazor_WebAssembly.Repositories;
using Blazor_WebAssembly.Services;
using Blazor_WebAssembly.Services.Implementations;
using Blazor_WebAssembly.Services.Interfaces;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Blazor_WebAssembly.Configuration
{
    public static class ServiceRegistration
    {
        public static void ConfigureServices(WebAssemblyHostBuilder builder)
        {
            // Add Blazored LocalStorage first
            builder.Services.AddBlazoredLocalStorage();

            // Add API discovery service
            builder.Services.AddScoped<IApiDiscoveryService, ApiDiscoveryService>();

            // FIXED: Use the fully qualified namespace for ILocalStorageService and removed reference to AppLocalStorageService
            // There's no need to wrap Blazored.LocalStorage since your services are already using it directly

            // HTTP Client Factory Configuration
            builder.Services.AddHttpClient("API", async (sp, client) =>
            {
                var apiDiscovery = sp.GetRequiredService<IApiDiscoveryService>();
                var apiBaseUrl = await apiDiscovery.DiscoverApiUrl();

                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                // Allow self-signed certificates in development
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
            })
            .AddHttpMessageHandler<AuthTokenHandler>();

            // Default HttpClient
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

            // Auth Token Handler
            builder.Services.AddTransient<AuthTokenHandler>();

            // Authentication and Authorization
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

            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Configure service for handling JSON Web Tokens
            builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();

            // Domain and application services
            builder.Services.AddScoped<Domain_Project.Interfaces.IEquipmentRepository, ClientSideEquipmentRepository>();
            builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IEquipmentRequestService, EquipmentRequestService>();
            builder.Services.AddScoped<IEquipmentReturnService, EquipmentReturnService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<Blazor_WebAssembly.Services.Interfaces.IUserService, UserService>();
        }
    }
}
