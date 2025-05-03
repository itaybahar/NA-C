using Blazor_WebAssembly.Models.Checkout;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
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

        public async Task<bool> ReturnEquipmentAsync(int checkoutId, string condition = "Good", string notes = "")
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();

                // Create a return request with additional data
                var returnRequest = new
                {
                    Condition = condition,
                    Notes = notes,
                    ReturnDate = DateTime.UtcNow
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(returnRequest),
                    Encoding.UTF8,
                    "application/json");

                // Use PUT for updating equipment status with a body
                var response = await _httpClient.PutAsync($"{BaseApiPath}/return/{checkoutId}", content);

                if (response.IsSuccessStatusCode)
                {
                    // Log the successful status change
                    Console.WriteLine($"Equipment status updated to 'Returned' for checkout ID {checkoutId}");

                    // Update the history status too
                    await UpdateCheckoutHistoryStatusAsync(checkoutId, "Returned", DateTime.UtcNow);

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

        // Simple version for the interface implementation
        public async Task<bool> ReturnEquipmentAsync(int checkoutId)
        {
            // Call the more detailed version with default parameters
            return await ReturnEquipmentAsync(checkoutId, "Good", "");
        }

        // Add a new method to explicitly update the history status
        private async Task<bool> UpdateCheckoutHistoryStatusAsync(int checkoutId, string status, DateTime returnDate)
        {
            try
            {
                var updateRequest = new
                {
                    CheckoutId = checkoutId,
                    Status = status,
                    ReturnDate = returnDate
                };

                var response = await _httpClient.PutAsJsonAsync($"{BaseApiPath}/history/update-status", updateRequest);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully updated checkout history status to '{status}' for checkout ID {checkoutId}");
                    return true;
                }

                Console.WriteLine($"Failed to update checkout history status. Status code: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateCheckoutHistoryStatusAsync: {ex.Message}");
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

        public async Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/equipmentcheckout/get-checkout-id?teamId={teamId}&equipmentId={equipmentId}");

                if (response.IsSuccessStatusCode)
                {
                    var checkoutId = await response.Content.ReadFromJsonAsync<int>();
                    Console.WriteLine($"Retrieved checkout ID {checkoutId} for team ID {teamId} and equipment ID {equipmentId}.");
                    return checkoutId;
                }

                Console.WriteLine($"Failed to retrieve checkout ID. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCheckoutIdByTeamAndEquipmentAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AddCheckoutHistoryAsync(CheckoutRecordDto record)
        {
            try
            {
                // Debug the record being sent
                Console.WriteLine($"Attempting to add checkout history: EquipmentId={record.EquipmentId}, UserId={record.UserId}, ReturnedAt={record.ReturnedAt}");

                // Instead of using a non-existent API endpoint, let's directly update the existing checkout
                // Use the ReturnEquipment API endpoint which already exists and will mark the checkout as returned

                // Try using the equipment checkout endpoint - it already updates the status in the database
                int checkoutId = int.Parse(record.Id != "0" ? record.Id : "0");
                if (checkoutId == 0)
                {
                    // If we don't have a valid checkout ID, we need to find it
                    if (int.TryParse(record.EquipmentId, out int equipmentId) && record.TeamId > 0)
                    {
                        var foundCheckoutId = await GetCheckoutIdByTeamAndEquipmentAsync(record.TeamId, equipmentId);
                        if (foundCheckoutId.HasValue)
                        {
                            checkoutId = foundCheckoutId.Value;
                        }
                    }
                }

                if (checkoutId > 0)
                {
                    // Create a return request
                    var returnRequest = new
                    {
                        ReturnDate = record.ReturnedAt ?? DateTime.UtcNow,
                        Condition = "Returned", // Use default value or add to record
                        Notes = "Returned via system" // Use default value or add to record
                    };

                    // Send the update request
                    var response = await _httpClient.PutAsJsonAsync($"api/equipmentcheckout/return/{checkoutId}", returnRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Successfully updated checkout ID {checkoutId} as returned.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Failed to update checkout history. Status: {response.StatusCode}");
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response content: {content}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Cannot add checkout history: no valid checkout ID");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddCheckoutHistoryAsync: {ex.Message}");
                return false;
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
