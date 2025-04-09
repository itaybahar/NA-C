using Blazored.LocalStorage;
using Blazor_WebAssembly;
using Blazor_WebAssembly.Services;
using Blazor_WebAssembly.Services.Implementations;
using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace Blazor_WebAssembly_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            // Configure root components
            ConfigureRootComponents(builder);

            // Configure services
            ConfigureServices(builder);

            await builder.Build().RunAsync();
        }

        private static void ConfigureRootComponents(WebAssemblyHostBuilder builder)
        {
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
        }

        private static void ConfigureServices(WebAssemblyHostBuilder builder)
        {
            // HTTP Client
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7176/") // Update to match your API base address
            });

            // Local storage
            builder.Services.AddBlazoredLocalStorage();

            // JS interop
            builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();

            // Authentication
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddAuthorizationCore();

            // Custom services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IEquipmentRequestService, EquipmentRequestService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
        }
    }
}
