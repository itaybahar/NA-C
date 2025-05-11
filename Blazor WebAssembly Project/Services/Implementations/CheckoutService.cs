using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IHttpClientFactory _httpClientFactory;

        // Fix: Add correct API endpoint prefix to ensure requests go to the API
        private const string BaseApiPath = "api/equipmentcheckout"; // Lowercase for consistency with API routes

        public CheckoutService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider,
            IHttpClientFactory httpClientFactory = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _httpClientFactory = httpClientFactory;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private async Task<HttpClient> GetHttpClientAsync()
        {
            // If _httpClientFactory is available, get a fresh client
            if (_httpClientFactory != null)
            {
                return _httpClientFactory.CreateClient("API");
            }

            // Otherwise, use the injected HttpClient
            return _httpClient;
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

                Console.WriteLine($"Current user roles: {string.Join(", ", roles)}");

                // The token should be automatically included if using the HttpClient from the DI container
                // with the correct authentication handler configuration
            }
            else
            {
                Console.WriteLine("Warning: User is not authenticated");
            }
        }

        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var httpClient = await GetHttpClientAsync();

                var response = await httpClient.GetAsync($"{BaseApiPath}/active");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckout>>(_jsonOptions)
                           ?? new List<EquipmentCheckout>();
                }

                await HandleErrorResponse(response, "fetching active checkouts");
                return new List<EquipmentCheckout>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching active checkouts: {ex.Message}");
                return new List<EquipmentCheckout>();
            }
        }

        // In CheckoutService.cs, replace the existing AddAdminHistoryRecordAsync method with this:
        public async Task<bool> AddAdminHistoryRecordAsync(CheckoutRecordDto record)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var httpClient = await GetHttpClientAsync();

                Console.WriteLine($"Creating admin history record: {JsonSerializer.Serialize(record)}");

                // First try the admin-history endpoint 
                var response = await httpClient.PostAsJsonAsync($"{BaseApiPath}/admin-history", record);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Admin history record created successfully");
                    return true;
                }

                Console.WriteLine($"Admin history endpoint failed with status {response.StatusCode}. Trying standard checkout flow.");

                // If that didn't work, try a standard checkout-then-return flow
                var checkoutRequest = new
                {
                    EquipmentID = int.Parse(record.EquipmentId),
                    TeamID = record.TeamId,
                    UserId = record.UserId,
                    Quantity = Math.Abs(record.Quantity), // Always positive for checkout
                    CheckoutDate = record.CheckedOutAt ?? DateTime.UtcNow,
                    ExpectedReturnDate = (record.ReturnedAt ?? DateTime.UtcNow).AddDays(1), // Just needs to be after checkout
                    Notes = record.ItemNotes ?? "System operation"
                };

                Console.WriteLine($"Creating checkout with data: {JsonSerializer.Serialize(checkoutRequest)}");
                response = await httpClient.PostAsJsonAsync($"{BaseApiPath}/checkout", checkoutRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create checkout: {response.StatusCode} - {errorContent}");
                    return false;
                }

                // Extract the checkout ID
                var content = await response.Content.ReadAsStringAsync();
                int checkoutId;

                if (int.TryParse(content, out checkoutId))
                {
                    Console.WriteLine($"Checkout created with ID: {checkoutId}");

                    // For inventory adjustments (not returns), immediately return the item to reflect adjustment
                    if (record.ReturnedAt.HasValue)
                    {
                        var returnRequest = new
                        {
                            Condition = record.ItemCondition ?? "Good",
                            Notes = record.ItemNotes ?? "System operation completed",
                            ReturnDate = record.ReturnedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        };

                        Console.WriteLine($"Marking as returned with data: {JsonSerializer.Serialize(returnRequest)}");
                        var returnResponse = await httpClient.PutAsJsonAsync($"{BaseApiPath}/return/{checkoutId}", returnRequest);

                        if (!returnResponse.IsSuccessStatusCode)
                        {
                            var errorContent = await returnResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"Failed to mark as returned: {returnResponse.StatusCode} - {errorContent}");
                            return false;
                        }

                        Console.WriteLine("Return operation succeeded");
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine($"Could not parse checkout ID from response: {content}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAdminHistoryRecordAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Add this helper method to CheckoutService.cs
        private void LogToConsole(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }


        private async Task<bool> TryAlternateAdminHistoryEndpointAsync(HttpClient httpClient, CheckoutRecordDto record)
        {
            try
            {
                // Try an alternate endpoint if the primary one fails
                var response = await httpClient.PostAsJsonAsync("api/checkout/admin-history", record);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully added admin history record via alternate endpoint");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Failed to add admin history via alternate endpoint: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error with alternate admin history endpoint: {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var httpClient = await GetHttpClientAsync();

                var response = await httpClient.GetAsync($"{BaseApiPath}/overdue");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckout>>(_jsonOptions)
                           ?? new List<EquipmentCheckout>();
                }

                await HandleErrorResponse(response, "fetching overdue checkouts");
                return new List<EquipmentCheckout>();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching overdue checkouts: {ex.Message}");
                return new List<EquipmentCheckout>();
            }
        }

        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var httpClient = await GetHttpClientAsync();

                // Create a return request with exact field names matching the backend model
                var returnRequest = new
                {
                    Condition = "Good",
                    Notes = "Returned via system",
                    ReturnDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                // Use PUT for updating equipment status with a body
                var response = await httpClient.PutAsJsonAsync($"{BaseApiPath}/return/{checkoutId}", returnRequest, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Equipment status updated to 'Returned' for checkout ID {checkoutId}");
                    return true;
                }

                // Get and log error details
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Error returning equipment for checkout ID {checkoutId}: Status: {response.StatusCode}, Content: {errorContent}");

                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error returning equipment for checkout ID {checkoutId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckoutEquipmentAsync(int teamId, int equipmentId, int userId, int quantity)
        {
            try
            {
                // Create the checkout request with all required fields
                var checkoutRequest = new
                {
                    TeamID = teamId,
                    EquipmentID = equipmentId,
                    UserId = userId,
                    Quantity = quantity,
                    ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                    CheckoutDate = DateTime.UtcNow,
                    Notes = "Checked out via system"
                };

                Console.WriteLine($"Sending checkout request: {JsonSerializer.Serialize(checkoutRequest)}");

                var response = await _httpClient.PostAsJsonAsync($"{BaseApiPath}/checkout", checkoutRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error checking out equipment: Status: {response.StatusCode}, Content: {errorContent}");
                    return false;
                }

                Console.WriteLine($"Successfully checked out {quantity} unit(s) of equipment ID {equipmentId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in CheckoutEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        // Helper methods to safely get properties from JsonElement
        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                return property.GetString();
            }
            return null;
        }

        private int GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number)
            {
                return property.GetInt32();
            }
            return 0;
        }

        private decimal? GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number)
            {
                return property.GetDecimal();
            }
            return null;
        }


        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                var httpClient = await GetHttpClientAsync();
                Console.WriteLine($"Requesting checkout history from: {BaseApiPath}/history/detailed");

                var response = await httpClient.GetAsync($"{BaseApiPath}/history/detailed");
                Console.WriteLine($"History request status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // Log the raw response content for debugging
                    var contentString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw response length: {contentString.Length}");

                    var history = await response.Content.ReadFromJsonAsync<List<CheckoutRecordDto>>(_jsonOptions);

                    if (history != null)
                    {
                        // Ensure each record has a Quantity field populated
                        foreach (var record in history.Where(r => r.Quantity <= 0))
                        {
                            record.Quantity = 1;
                            Console.WriteLine($"Defaulting quantity to 1 for record ID: {record.Id}");
                        }

                        Console.WriteLine($"Loaded {history.Count} checkout history records with quantity information");
                        return history;
                    }
                    return new List<CheckoutRecordDto>();
                }
                else
                {
                    Console.WriteLine($"Error fetching checkout history. Status code: {response.StatusCode}");
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error content: {errorContent}");

                    // Try alternate endpoint if the first one fails with 404
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("Trying alternate endpoint for checkout history");
                        return await TryAlternateHistoryEndpointAsync(httpClient);
                    }

                    return new List<CheckoutRecordDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCheckoutHistoryAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return new List<CheckoutRecordDto>();
            }
        }

        private async Task<List<CheckoutRecordDto>> TryAlternateHistoryEndpointAsync(HttpClient httpClient)
        {
            try
            {
                // Try an alternate endpoint if the primary one fails
                var response = await httpClient.GetAsync("api/EquipmentCheckout/history");

                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<List<CheckoutRecordDto>>(_jsonOptions);
                    return history ?? new List<CheckoutRecordDto>();
                }

                return new List<CheckoutRecordDto>();
            }
            catch
            {
                return new List<CheckoutRecordDto>();
            }
        }

        public async Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId)
        {
            try
            {
                var httpClient = await GetHttpClientAsync();
                var response = await httpClient.GetAsync($"{BaseApiPath}/equipment/{equipmentId}/in-use-quantity");
                Console.WriteLine($"In-use quantity request status: {response.StatusCode} for equipment {equipmentId}");

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"In-use quantity raw response for equipment {equipmentId}: {content}");

                    if (int.TryParse(content, out int inUseQuantity))
                    {
                        return inUseQuantity;
                    }

                    return 0;
                }
                else
                {
                    Console.WriteLine($"Error fetching in-use quantity for equipment ID {equipmentId}. Status code: {response.StatusCode}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetInUseQuantityForEquipmentAsync: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity)
        {
            int inUseQuantity = await GetInUseQuantityForEquipmentAsync(equipmentId);
            return Math.Max(0, totalQuantity - inUseQuantity);
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
