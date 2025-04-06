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

            // Root components
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Configure HttpClient with API base address
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5191/") // Change to your API address as needed
            });

            // Local storage (required for authentication)
            builder.Services.AddBlazoredLocalStorage();

            // JS interop
            builder.Services.AddScoped<IJSRuntimeService, JSRuntimeService>();

            // Authentication services
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddAuthorizationCore();

            // Custom services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEquipmentService, EquipmentService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IEquipmentRequestService, EquipmentRequestService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();

            await builder.Build().RunAsync();
        }
    }
}
