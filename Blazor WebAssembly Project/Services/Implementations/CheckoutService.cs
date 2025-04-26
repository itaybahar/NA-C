using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private const string BaseApiPath = "api/EquipmentCheckout";

        public CheckoutService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        }

        // Ensure the auth token is attached to each request
        private async Task EnsureAuthorizationHeaderAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                // Get user claims to check roles
                var roles = authState.User.Claims
                    .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList();

                // Log roles for debugging
                Console.WriteLine($"Current user roles: {string.Join(", ", roles)}");

                // The token should be automatically included if using the HttpClient from the DI container
                // with the correct authentication handler configuration
            }
            else
            {
                Console.WriteLine("Warning: User is not authenticated");
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetAllCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync(BaseApiPath);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckoutModel>>()
                           ?? new List<EquipmentCheckoutModel>();
                }

                await HandleErrorResponse(response, "fetching all checkouts");
                return new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching all checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<EquipmentCheckoutModel> GetCheckoutByIdAsync(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync($"{BaseApiPath}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EquipmentCheckoutModel>()
                           ?? new EquipmentCheckoutModel();
                }

                await HandleErrorResponse(response, $"fetching checkout by ID {id}");
                return new EquipmentCheckoutModel();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching checkout by ID {id}: {ex.Message}");
                return new EquipmentCheckoutModel();
            }
        }

        public async Task<bool> CreateCheckoutAsync(EquipmentCheckoutModel checkout)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.PostAsJsonAsync(BaseApiPath, checkout);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await HandleErrorResponse(response, "creating checkout");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating checkout: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.PostAsync($"{BaseApiPath}/return/{checkoutId}", null);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await HandleErrorResponse(response, $"returning equipment for checkout ID {checkoutId}");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error returning equipment for checkout ID {checkoutId}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetActiveCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync($"{BaseApiPath}/active");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckoutModel>>()
                           ?? new List<EquipmentCheckoutModel>();
                }

                await HandleErrorResponse(response, "fetching active checkouts");
                return new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching active checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task<List<EquipmentCheckoutModel>> GetOverdueCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync($"{BaseApiPath}/overdue");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckoutModel>>()
                           ?? new List<EquipmentCheckoutModel>();
                }

                await HandleErrorResponse(response, "fetching overdue checkouts");
                return new List<EquipmentCheckoutModel>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching overdue checkouts: {ex.Message}");
                return new List<EquipmentCheckoutModel>();
            }
        }

        public async Task CheckoutEquipmentAsync(int teamId, int equipmentId, int userId)
        {
            try
            {
                // Check if token is available
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (!authState.User.Identity?.IsAuthenticated == true)
                {
                    Console.WriteLine("Warning: Making API request without authentication");
                }

                var payload = JsonSerializer.Serialize(new
                {
                    TeamID = teamId,
                    EquipmentID = equipmentId,
                    UserID = userId
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Use HttpClient with error handling
                var response = await _httpClient.PostAsync($"api/equipmentcheckout/checkout", content);

                // Better error handling for token issues
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException("Authentication failed. Please log in again.");
                }

                response.EnsureSuccessStatusCode();
            }
            catch (UnauthorizedAccessException)
            {
                throw; // Let the component handle auth errors
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking out equipment: {ex.Message}");
                throw;
            }
        }


        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/equipmentcheckout/history/detailed");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        IgnoreReadOnlyProperties = true
                    };

                    var history = await response.Content.ReadFromJsonAsync<List<CheckoutRecordDto>>(options);
                    return history ?? new List<CheckoutRecordDto>();
                }
                else
                {
                    Console.WriteLine($"Error fetching checkout history. Status code: {response.StatusCode}");
                    return new List<CheckoutRecordDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCheckoutHistoryAsync: {ex.Message}");
                return new List<CheckoutRecordDto>();
            }
        }


        // Helper method to handle error responses with detailed information
        private async Task HandleErrorResponse(HttpResponseMessage response, string operationDescription)
        {
            string errorContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.Error.WriteLine($"403 Forbidden: You don't have permission for {operationDescription}. " +
                    $"Required role is likely 'WarehouseOperative' or 'WarehouseManager'. Error: {errorContent}");

                // Check authorization state to diagnose the issue
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    var roles = string.Join(", ", authState.User.Claims
                        .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                        .Select(c => c.Value));

                    Console.Error.WriteLine($"Current user authenticated as: {authState.User.Identity.Name}");
                    Console.Error.WriteLine($"User roles: {roles}");
                }
                else
                {
                    Console.Error.WriteLine("User is not authenticated!");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error {(int)response.StatusCode} {response.StatusCode} when {operationDescription}: {errorContent}");
            }
        }
    }
}
