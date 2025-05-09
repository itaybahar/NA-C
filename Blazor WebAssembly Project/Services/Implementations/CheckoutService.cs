using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class CheckoutService : ICheckoutService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        // Fix: Add correct API endpoint prefix to ensure requests go to the API
        private const string BaseApiPath = "api/EquipmentCheckout"; // Added "api/" prefix

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

        public async Task<List<EquipmentCheckout>> GetActiveCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync($"{BaseApiPath}/active");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckout>>()
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

        public async Task<List<EquipmentCheckout>> GetOverdueCheckoutsAsync()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                var response = await _httpClient.GetAsync($"{BaseApiPath}/overdue");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EquipmentCheckout>>()
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

                // Create a return request with exact field names matching the database
                var returnRequest = new
                {
                    ItemCondition = "Good",
                    ItemNotes = "Returned by system",
                    ReturnDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                // Use PUT for updating equipment status with a body
                var response = await _httpClient.PutAsJsonAsync($"{BaseApiPath}/return/{checkoutId}", returnRequest);

                if (response.IsSuccessStatusCode)
                {
                    // Log the successful status change
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
                // Fetch the equipment details to check the available quantity
                var equipmentResponse = await _httpClient.GetAsync($"api/equipment/{equipmentId}");
                if (!equipmentResponse.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine($"Error fetching equipment details for ID {equipmentId}: {equipmentResponse.StatusCode}");
                    return false;
                }

                var equipment = await equipmentResponse.Content.ReadFromJsonAsync<Equipment>();
                if (equipment == null)
                {
                    Console.Error.WriteLine($"Equipment with ID {equipmentId} not found.");
                    return false;
                }

                // Check if the requested quantity is available
                if (equipment.Quantity < quantity)
                {
                    Console.Error.WriteLine($"Requested quantity ({quantity}) exceeds available quantity ({equipment.Quantity}) for equipment ID {equipmentId}.");
                    return false;
                }

                // Create the checkout request
                var checkoutRequest = new
                {
                    TeamID = teamId,
                    EquipmentID = equipmentId,
                    UserID = userId,
                    Quantity = quantity,
                    ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                    CheckoutDate = DateTime.UtcNow,
                    Notes = "Checked out via system"
                };

                var response = await _httpClient.PostAsJsonAsync($"{BaseApiPath}/checkout", checkoutRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.Error.WriteLine($"Error checking out equipment: Status: {response.StatusCode}, Content: {errorContent}");
                    return false;
                }

                // Update the equipment quantity
                equipment.Quantity -= quantity;
                if (equipment.Quantity == 0)
                {
                    equipment.Status = "Unavailable";
                }

                var updateResponse = await _httpClient.PutAsJsonAsync($"api/equipment/{equipmentId}", equipment);
                if (!updateResponse.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine($"Error updating equipment quantity for ID {equipmentId}: {updateResponse.StatusCode}");
                    return false;
                }

                Console.WriteLine($"Successfully checked out {quantity} unit(s) of equipment ID {equipmentId}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in CheckoutEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<CheckoutRecordDto>> GetCheckoutHistoryAsync()
        {
            try
            {
                // Add more detailed logging to diagnose the issue
                Console.WriteLine($"Requesting checkout history from: {BaseApiPath}/history/detailed");

                // FIX: Use the correct BaseApiPath variable for a consistent URL pattern
                var response = await _httpClient.GetAsync($"{BaseApiPath}/history/detailed");

                // Log the response status
                Console.WriteLine($"History request status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        IgnoreReadOnlyProperties = true
                    };

                    // Log the raw response content for debugging
                    var contentString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw response: {contentString}");

                    var history = await response.Content.ReadFromJsonAsync<List<CheckoutRecordDto>>(options);

                    if (history != null)
                    {
                        // Ensure each record has a Quantity field populated
                        foreach (var record in history)
                        {
                            // If quantity is not set or is 0, default to 1 for backward compatibility
                            if (record.Quantity <= 0)
                            {
                                record.Quantity = 1;
                                Console.WriteLine($"Defaulting quantity to 1 for record ID: {record.Id}");
                            }
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

                    // Try to determine if it's a routing issue
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("The endpoint may not exist. Check the controller route configuration.");
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


        // Add this method to the CheckoutService class
        public async Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseApiPath}/equipment/{equipmentId}/in-use-quantity");
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

        // Add this method to calculate available quantity
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
