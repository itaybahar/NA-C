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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Linq;

namespace Blazor_WebAssembly_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Apply the static web assets manifest workaround FIRST, before anything else

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // ... rest of your code ...


            // Add proper tracing for debugging
            Console.WriteLine("Blazor WebAssembly application starting...");
            Console.WriteLine($"Base Directory: {AppContext.BaseDirectory}");

            // Workaround for .NET 9 static web assets issue
            // Add custom configuration to handle the staticwebassets.endpoints.json file absence
            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.WebAssembly.Hosting", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.StaticAssets", LogLevel.Warning);
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddConsole();


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

            // Configure EquipmentAPI client with improved error handling
            builder.Services.AddHttpClient("EquipmentAPI", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // Set a reasonable timeout
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>();

            // Register the AuthTokenHandler
            builder.Services.AddTransient<AuthTokenHandler>();

            // Register this as a transient service to avoid sharing state between components
            builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("EquipmentAPI"));

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

            // Configure application settings
            builder.Configuration["ApplicationName"] = "Blazor WebAssembly Project";

            // Create the static assets manifest exactly as needed by .NET 9
            try
            {
                // Use the exact project name as expected by the runtime - with spaces
                var projectName = "Blazor WebAssembly Project";
                var manifestDir = AppContext.BaseDirectory;
                var manifestPath = Path.Combine(manifestDir, $"{projectName}.staticwebassets.endpoints.json");

                Console.WriteLine($"Looking for static web assets manifest at: {manifestPath}");


                if (!File.Exists(manifestPath))
                {
                    Console.WriteLine("Static web assets manifest not found, creating one...");

                    // Check if we have a source manifest in the project directory that we can copy
                    var sourceManifestPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "..", "..", "..", "staticwebassets.endpoints.json");

                    // Normalize slashes for better path handling
                    sourceManifestPath = Path.GetFullPath(sourceManifestPath);
                    Console.WriteLine($"Looking for source manifest at: {sourceManifestPath}");

                    if (File.Exists(sourceManifestPath))
                    {
                        Console.WriteLine($"Found source manifest, copying to: {manifestPath}");
                        // Read the content of the source file
                        var jsonContent = File.ReadAllText(sourceManifestPath);

                        // Write to the target location
                        File.WriteAllText(manifestPath, jsonContent);
                        Console.WriteLine("Successfully copied static web assets manifest");
                    }
                    else
                    {
                        Console.WriteLine("Source manifest not found, creating a default one");
                        // Create a minimal manifest file with required structure
                        var minimalManifest = new
                        {
                            Version = "1.0",
                            Endpoints = new[]
                            {
                                new
                                {
                                    Path = "_framework/blazor.webassembly.js",
                                    Source = "wwwroot/_framework/blazor.webassembly.js"
                                },
                                new
                                {
                                    Path = "_framework/dotnet.wasm",
                                    Source = "wwwroot/_framework/dotnet.wasm"
                                }
                            },
                            IsBuildManifest = true
                        };

                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };

                        var json = JsonSerializer.Serialize(minimalManifest, options);
                        File.WriteAllText(manifestPath, json);
                        Console.WriteLine($"Created minimal static web assets manifest at {manifestPath}");
                    }
                }
                else
                {
                    Console.WriteLine("Static web assets manifest already exists");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Unable to create static web assets manifest: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            // Build the host
            var host = builder.Build();
            Console.WriteLine("Blazor WebAssembly host built successfully");

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
            Console.WriteLine("Starting Blazor WebAssembly application...");
            await host.RunAsync();
        }
    }
}
