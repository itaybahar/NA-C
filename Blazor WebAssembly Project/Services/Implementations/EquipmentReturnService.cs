using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentReturnService : IEquipmentReturnService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public EquipmentReturnService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
        }

        // Method with additional parameters (condition and notes)
        public async Task<bool> UpdateReturnedEquipmentAsync(int equipmentId, int checkoutId, int userId, string condition = "Good", string notes = "")
        {
            try
            {
                // Ensure the user is authenticated
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated != true)
                {
                    Console.WriteLine("User is not authenticated. Cannot proceed with the request.");
                    return false;
                }

                // Step 1: Mark the equipment as returned in the checkout history with condition and notes
                var checkoutService = new CheckoutService(_httpClient, _authStateProvider);
                var isReturned = await checkoutService.ReturnEquipmentAsync(checkoutId, condition, notes);

                if (!isReturned)
                {
                    Console.WriteLine($"Failed to mark equipment ID {equipmentId} as returned in checkout history.");
                    return false;
                }

                // Step 2: Update the equipment status to "Available"
                var equipmentService = new EquipmentService(_httpClient);
                var equipment = await equipmentService.GetEquipmentByIdAsync(equipmentId);

                if (equipment == null)
                {
                    Console.WriteLine($"Equipment ID {equipmentId} not found.");
                    return false;
                }

                equipment.Status = "Available"; // Update the status
                var isUpdated = await equipmentService.UpdateEquipmentAsync(equipment);

                if (!isUpdated)
                {
                    Console.WriteLine($"Failed to update equipment ID {equipmentId} status to 'Available'.");
                    return false;
                }

                // Step 3: Log the return in the checkout history
                var checkoutRecord = new CheckoutRecordDto
                {
                    Id = 0.ToString(), // Setting Id to 0 as required (will be assigned by database)
                    EquipmentId = equipmentId.ToString(), // Convert int to string as required
                    UserId = userId,
                    ReturnedAt = DateTime.UtcNow // Set the return date explicitly
                    // Add condition if supported by DTO
                    // Add notes if supported by DTO
                };

                var historyAdded = await AddCheckoutHistoryAsync(checkoutRecord);

                if (!historyAdded)
                {
                    Console.WriteLine($"Failed to add return entry to checkout history for equipment ID {equipmentId}.");
                    return false;
                }

                Console.WriteLine($"Successfully updated returned equipment ID {equipmentId}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateReturnedEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        // Original method for backward compatibility - calls the version with additional parameters
        public async Task<bool> UpdateReturnedEquipmentAsync(int equipmentId, int checkoutId, int userId)
        {
            return await UpdateReturnedEquipmentAsync(equipmentId, checkoutId, userId, "Good", "");
        }

        public async Task<bool> UpdateReturnedEquipmentByTeamAsync(int equipmentId, int teamId, int userId, string condition = "Good", string notes = "")
        {
            try
            {
                // Ensure the user is authenticated
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated != true)
                {
                    Console.WriteLine("User is not authenticated. Cannot proceed with the request.");
                    return false;
                }

                // Step 1: Get the checkout ID based on team and equipment
                var checkoutId = await GetCheckoutIdByTeamAndEquipmentAsync(teamId, equipmentId);
                if (!checkoutId.HasValue)
                {
                    Console.WriteLine($"Failed to retrieve checkout ID for team ID {teamId} and equipment ID {equipmentId}.");
                    return false;
                }

                // Step 2: Mark the equipment as returned in the checkout history with condition and notes
                var checkoutService = new CheckoutService(_httpClient, _authStateProvider);
                var isReturned = await checkoutService.ReturnEquipmentAsync(checkoutId.Value, condition, notes);

                if (!isReturned)
                {
                    Console.WriteLine($"Failed to mark equipment ID {equipmentId} as returned in checkout history.");
                    return false;
                }

                // Step 3: Update the equipment status to "Available"
                var equipmentService = new EquipmentService(_httpClient);
                var equipment = await equipmentService.GetEquipmentByIdAsync(equipmentId);

                if (equipment == null)
                {
                    Console.WriteLine($"Equipment ID {equipmentId} not found.");
                    return false;
                }

                equipment.Status = "Available"; // Update the status
                var isUpdated = await equipmentService.UpdateEquipmentAsync(equipment);

                if (!isUpdated)
                {
                    Console.WriteLine($"Failed to update equipment ID {equipmentId} status to 'Available'.");
                    return false;
                }

                // Step 4: Update the team amount based on the returned equipment
                var isAmountUpdated = await UpdateTeamAmountAsync(teamId, equipmentId);
                if (!isAmountUpdated)
                {
                    Console.WriteLine($"Failed to update amount for team ID {teamId} based on returned equipment ID {equipmentId}.");
                    // Continue with the process even if amount update fails
                }

                // Step 5: Log the return in the checkout history
                var checkoutRecord = new CheckoutRecordDto
                {
                    Id = 0.ToString(), // Setting Id to 0 as required (will be assigned by database)
                    EquipmentId = equipmentId.ToString(), // Convert int to string as required
                    TeamId = teamId,
                    UserId = userId,
                    ReturnedAt = DateTime.UtcNow // Set the return date explicitly
                    // Add condition if supported by DTO
                    // Add notes if supported by DTO
                };

                var historyAdded = await AddCheckoutHistoryAsync(checkoutRecord);

                if (!historyAdded)
                {
                    Console.WriteLine($"Failed to add return entry to checkout history for equipment ID {equipmentId}.");
                    return false;
                }

                Console.WriteLine($"Successfully updated returned equipment ID {equipmentId} for team ID {teamId}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateReturnedEquipmentByTeamAsync: {ex.Message}");
                return false;
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

        public async Task<bool> UpdateTeamAmountAsync(int teamId, int equipmentId)
        {
            try
            {
                // Create an empty object to send in the body
                var emptyContent = new { };

                // Specify the type argument explicitly for PutAsJsonAsync
                var response = await _httpClient.PutAsJsonAsync<object>($"api/team/update-amount?teamId={teamId}&equipmentId={equipmentId}", emptyContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully updated amount for team ID {teamId} based on returned equipment ID {equipmentId}.");
                    return true;
                }

                Console.WriteLine($"Failed to update amount for team ID {teamId}. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateTeamAmountAsync: {ex.Message}");
                return false;
            }
        }

        // Helper method to add an entry to the checkout history
        // Helper method to add an entry to the checkout history
        private async Task<bool> AddCheckoutHistoryAsync(CheckoutRecordDto record)
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

    }
}
