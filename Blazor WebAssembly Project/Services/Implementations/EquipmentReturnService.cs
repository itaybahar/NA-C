using Blazor_WebAssembly.Auth;
using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs;
using Domain_Project.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentReturnService : IEquipmentReturnService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<EquipmentReturnService> _logger;
        private readonly DynamicApiUrlHandler _apiUrlHandler;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<EquipmentService> _equipmentServiceLogger;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorageService;

        public EquipmentReturnService(
            IHttpClientFactory httpClientFactory,
            AuthenticationStateProvider authStateProvider,
            ILogger<EquipmentReturnService> logger = null,
            DynamicApiUrlHandler apiUrlHandler = null,
            IJSRuntime jsRuntime = null,
            ILocalStorageService localStorageService = null,
            ILogger<EquipmentService> equipmentServiceLogger = null)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
            _logger = logger;
            _apiUrlHandler = apiUrlHandler;
            _jsRuntime = jsRuntime;
            _localStorageService = localStorageService;
            _equipmentServiceLogger = equipmentServiceLogger;

            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private async Task<HttpClient> GetHttpClientAsync()
        {
            // If dynamic API URL handler is available, use it to get a client with the discovered API URL
            if (_apiUrlHandler != null)
            {
                try
                {
                    var client = await _apiUrlHandler.GetClientWithDiscoveredApiAsync();
                    if (client.BaseAddress != null)
                    {
                        LogMessage($"Retrieved HTTP client with base address: {client.BaseAddress}", LogLevel.Debug);
                        return client;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to get client from API URL handler: {ex.Message}", LogLevel.Warning);
                }
            }

            // Fall back to creating a new client from the factory
            try
            {
                var client = _httpClientFactory.CreateClient("API");
                LogMessage($"Created HTTP client with base address: {client.BaseAddress}", LogLevel.Debug);
                return client;
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to create API client: {ex.Message}", LogLevel.Error);
                // Last resort: create a basic client
                return new HttpClient { BaseAddress = new Uri("https://localhost:5191/") };
            }
        }
        public async Task<bool> UpdateReturnedEquipmentAsync(int equipmentId, int checkoutId, int userId, int quantity, string condition = "Good", string notes = "")
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated != true)
                {
                    LogMessage("User is not authenticated. Cannot proceed with the request.", LogLevel.Warning);
                    return false;
                }

                // Always get the checkout record to ensure we have the correct quantity and status
                var checkout = await GetCheckoutAsync(checkoutId);
                if (checkout == null)
                {
                    LogMessage($"Failed to retrieve checkout record for ID {checkoutId}.", LogLevel.Warning);
                    return false;
                }

                // If quantity is not specified or <= 0, use the checkout's quantity (or 1 as fallback)
                if (quantity <= 0)
                {
                    quantity = checkout.Quantity > 0 ? checkout.Quantity : 1;
                }

                // Prepare the return request payload
                var returnRequest = new
                {
                    Condition = condition,
                    Notes = notes,
                    ReturnDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Quantity = quantity
                };

                // Mark the checkout as returned via API
                var response = await httpClient.PutAsJsonAsync($"api/equipmentcheckout/return/{checkoutId}", returnRequest, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"Failed to mark checkout as returned. Status: {response.StatusCode}. Content: {errorContent}", LogLevel.Warning);
                    return false;
                }

                // Only update equipment quantity if quantity > 0
                if (quantity > 0)
                {
                    var equipmentService = CreateEquipmentService(httpClient);
                    var equipment = await equipmentService.GetEquipmentByIdAsync(equipmentId);

                    if (equipment == null)
                    {
                        LogMessage($"Equipment ID {equipmentId} not found.", LogLevel.Warning);
                        return false;
                    }

                    equipment.Quantity += quantity;

                    if (equipment.Quantity > 0)
                    {
                        equipment.Status = "Available";
                    }

                    var isUpdated = await equipmentService.UpdateEquipmentAsync(equipment);

                    if (!isUpdated)
                    {
                        LogMessage($"Failed to update equipment ID {equipmentId} status and quantity.", LogLevel.Warning);
                        return false;
                    }
                }
                else
                {
                    LogMessage($"Return quantity is 0 for equipment ID {equipmentId}, skipping inventory update.", LogLevel.Information);
                }

                // Add to checkout history (optional, depending on your business logic)
                var checkoutRecord = new CheckoutRecordDto
                {
                    Id = checkoutId.ToString(),
                    EquipmentId = equipmentId.ToString(),
                    UserId = userId,
                    ReturnedAt = DateTime.UtcNow,
                    Quantity = quantity,
                    ItemCondition = condition,
                    ItemNotes = notes
                };

                var historyAdded = await AddCheckoutHistoryAsync(checkoutRecord, notes);

                if (!historyAdded)
                {
                    LogMessage($"Failed to add return entry to checkout history for equipment ID {equipmentId}.", LogLevel.Warning);
                    // Not a hard failure, so continue
                }

                LogMessage($"Successfully updated returned equipment ID {equipmentId} with {quantity} units.", LogLevel.Information);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in UpdateReturnedEquipmentAsync: {ex.Message}", LogLevel.Error, ex);
                return false;
            }
        }


        public async Task<bool> UpdateReturnedEquipmentByTeamAsync(int equipmentId, int teamId, int userId, int quantity, string condition = "Good", string notes = "")
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated != true)
                {
                    LogMessage("User is not authenticated. Cannot proceed with the request.", LogLevel.Warning);
                    return false;
                }

                var checkoutId = await GetCheckoutIdByTeamAndEquipmentAsync(teamId, equipmentId);
                if (!checkoutId.HasValue)
                {
                    LogMessage($"Failed to retrieve checkout ID for team ID {teamId} and equipment ID {equipmentId}.", LogLevel.Warning);
                    return false;
                }

                if (quantity <= 0)
                {
                    var checkout = await GetCheckoutAsync(checkoutId.Value);
                    if (checkout == null)
                    {
                        LogMessage($"Failed to retrieve checkout record for ID {checkoutId}.", LogLevel.Warning);
                        return false;
                    }
                    quantity = checkout.Quantity > 0 ? checkout.Quantity : 1;
                }

                var checkoutService = new CheckoutService(httpClient, _authStateProvider, _httpClientFactory);
                var isReturned = await checkoutService.ReturnEquipmentAsync(checkoutId.Value);

                if (!isReturned)
                {
                    LogMessage($"Failed to mark equipment ID {equipmentId} as returned in checkout history.", LogLevel.Warning);
                    return false;
                }

                // Create EquipmentService with all required parameters
                var equipmentService = CreateEquipmentService(httpClient);
                var equipment = await equipmentService.GetEquipmentByIdAsync(equipmentId);

                if (equipment == null)
                {
                    LogMessage($"Equipment ID {equipmentId} not found.", LogLevel.Warning);
                    return false;
                }

                equipment.Quantity += quantity;

                if (equipment.Quantity > 0)
                {
                    equipment.Status = "Available";
                }

                var isUpdated = await equipmentService.UpdateEquipmentAsync(equipment);

                if (!isUpdated)
                {
                    LogMessage($"Failed to update equipment ID {equipmentId} status and quantity.", LogLevel.Warning);
                    return false;
                }

                var isAmountUpdated = await UpdateTeamAmountAsync(teamId, equipmentId, quantity);
                if (!isAmountUpdated)
                {
                    LogMessage($"Failed to update amount for team ID {teamId} based on returned equipment ID {equipmentId}.", LogLevel.Warning);
                }

                var checkoutRecord = new CheckoutRecordDto
                {
                    Id = checkoutId.Value.ToString(),
                    EquipmentId = equipmentId.ToString(),
                    TeamId = teamId,
                    UserId = userId,
                    ReturnedAt = DateTime.UtcNow,
                    Quantity = quantity,
                    ItemCondition = condition,
                    ItemNotes = notes
                };

                var historyAdded = await AddCheckoutHistoryAsync(checkoutRecord, notes);

                if (!historyAdded)
                {
                    LogMessage($"Failed to add return entry to checkout history for equipment ID {equipmentId}.", LogLevel.Warning);
                    return false;
                }

                LogMessage($"Successfully updated returned equipment ID {equipmentId} with {quantity} units for team ID {teamId}.", LogLevel.Information);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in UpdateReturnedEquipmentByTeamAsync: {ex.Message}", LogLevel.Error, ex);
                return false;
            }
        }

        // Helper method to create properly initialized EquipmentService
        private EquipmentService CreateEquipmentService(HttpClient httpClient)
        {
            return new EquipmentService(
                httpClient,
                _equipmentServiceLogger,
                _jsRuntime,
                _apiUrlHandler,
                _localStorageService);
        }

        public async Task<bool> UpdateReturnedEquipmentByTeamAsync(int equipmentId, int teamId, int userId, string condition = "Good", string notes = "")
        {
            return await UpdateReturnedEquipmentByTeamAsync(equipmentId, teamId, userId, 0, condition, notes);
        }

        private async Task<EquipmentCheckout?> GetCheckoutAsync(int checkoutId)
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                // Add the API path prefix and consistent endpoint format
                string endpoint = $"api/equipmentcheckout/{checkoutId}";

                // Improved error handling with retry logic
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(endpoint);
                        if (response.IsSuccessStatusCode)
                        {
                            LogMessage($"Successfully retrieved checkout {checkoutId}", LogLevel.Debug);
                            return await response.Content.ReadFromJsonAsync<EquipmentCheckout>(_jsonOptions);
                        }

                        LogMessage($"Failed to retrieve checkout record. Status: {response.StatusCode}", LogLevel.Warning);

                        // If not found, don't retry
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return null;

                        // If we're on the last attempt, don't wait
                        if (attempt < 2)
                            await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                    }
                    catch (Exception ex)
                    {
                        // Only log on final attempt
                        if (attempt == 2)
                            LogMessage($"Exception in GetCheckoutAsync: {ex.Message}", LogLevel.Error, ex);

                        // If we're on the last attempt, don't wait
                        if (attempt < 2)
                            await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in GetCheckoutAsync: {ex.Message}", LogLevel.Error, ex);
                return null;
            }
        }

        public async Task<int?> GetCheckoutIdByTeamAndEquipmentAsync(int teamId, int equipmentId)
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                var response = await httpClient.GetAsync($"api/equipmentcheckout/get-checkout-id?teamId={teamId}&equipmentId={equipmentId}");

                if (response.IsSuccessStatusCode)
                {
                    var checkoutId = await response.Content.ReadFromJsonAsync<int>(_jsonOptions);
                    LogMessage($"Retrieved checkout ID {checkoutId} for team ID {teamId} and equipment ID {equipmentId}.", LogLevel.Information);
                    return checkoutId;
                }

                LogMessage($"Failed to retrieve checkout ID. Status: {response.StatusCode}", LogLevel.Warning);
                // Try to parse error content for more details
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogMessage($"Error content: {errorContent}", LogLevel.Warning);
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in GetCheckoutIdByTeamAndEquipmentAsync: {ex.Message}", LogLevel.Error, ex);
                return null;
            }
        }

        public async Task<bool> UpdateTeamAmountAsync(int teamId, int equipmentId, int quantity)
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                var requestContent = new { Quantity = quantity };

                var response = await httpClient.PutAsJsonAsync($"api/teams/update-amount?teamId={teamId}&equipmentId={equipmentId}&quantity={quantity}", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    LogMessage($"Successfully updated amount for team ID {teamId} based on returned equipment ID {equipmentId} with quantity {quantity}.", LogLevel.Information);
                    return true;
                }

                LogMessage($"Failed to update amount for team ID {teamId}. Status: {response.StatusCode}", LogLevel.Warning);

                // Log response content for debugging
                var errorContent = await response.Content.ReadAsStringAsync();
                LogMessage($"Error content: {errorContent}", LogLevel.Warning);

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in UpdateTeamAmountAsync: {ex.Message}", LogLevel.Error, ex);
                return false;
            }
        }

        public async Task<int> GetInUseQuantityForEquipmentAsync(int equipmentId)
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                var response = await httpClient.GetAsync($"api/equipmentcheckout/equipment/{equipmentId}/in-use-quantity");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    LogMessage($"In-use quantity response for equipment {equipmentId}: {content}", LogLevel.Debug);

                    if (int.TryParse(content, out int inUseQuantity))
                    {
                        return inUseQuantity;
                    }
                    return 0;
                }

                LogMessage($"Failed to get in-use quantity for equipment ID {equipmentId}. Status: {response.StatusCode}", LogLevel.Warning);
                return 0;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in GetInUseQuantityForEquipmentAsync: {ex.Message}", LogLevel.Error, ex);
                return 0;
            }
        }

        public async Task<int> GetAvailableQuantityForEquipmentAsync(int equipmentId, int totalQuantity)
        {
            try
            {
                var inUseQuantity = await GetInUseQuantityForEquipmentAsync(equipmentId);
                return Math.Max(0, totalQuantity - inUseQuantity);
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in GetAvailableQuantityForEquipmentAsync: {ex.Message}", LogLevel.Error, ex);
                return 0;
            }
        }

        private async Task<bool> AddCheckoutHistoryAsync(CheckoutRecordDto record, string notes = "")
        {
            try
            {
                var httpClient = await GetHttpClientAsync();

                LogMessage($"Attempting to add checkout history: EquipmentId={record.EquipmentId}, UserId={record.UserId}, ReturnedAt={record.ReturnedAt}, Quantity={record.Quantity}", LogLevel.Information);

                int checkoutId = int.Parse(record.Id != "0" ? record.Id : "0");
                if (checkoutId == 0)
                {
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
                    var returnRequest = new
                    {
                        Condition = record.ItemCondition,
                        Notes = notes,
                        ReturnDate = record.ReturnedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        Quantity = record.Quantity
                    };

                    LogMessage($"Sending return request: {JsonSerializer.Serialize(returnRequest, _jsonOptions)}", LogLevel.Debug);

                    // Implement retry logic for more resilience
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            var response = await httpClient.PutAsJsonAsync($"api/equipmentcheckout/return/{checkoutId}", returnRequest, _jsonOptions);

                            if (response.IsSuccessStatusCode)
                            {
                                LogMessage($"Successfully updated checkout ID {checkoutId} as returned with quantity {record.Quantity}.", LogLevel.Information);
                                return true;
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                LogMessage($"Failed to update checkout history. Status: {response.StatusCode}", LogLevel.Warning);
                                LogMessage($"Response content: {errorContent}", LogLevel.Warning);

                                // Don't retry client errors except for 429 (Too Many Requests)
                                if ((int)response.StatusCode < 500 && response.StatusCode != HttpStatusCode.TooManyRequests)
                                    return false;

                                // On the last attempt, don't wait
                                if (attempt < 2)
                                    await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                            }
                        }
                        catch (Exception ex)
                        {
                            // Only log on final attempt to reduce noise
                            if (attempt == 2)
                                LogMessage($"Exception during attempt {attempt + 1} in AddCheckoutHistoryAsync: {ex.Message}", LogLevel.Error, ex);

                            // On the last attempt, don't wait
                            if (attempt < 2)
                                await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                        }
                    }

                    return false;
                }
                else
                {
                    LogMessage("Cannot add checkout history: no valid checkout ID", LogLevel.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Exception in AddCheckoutHistoryAsync: {ex.Message}", LogLevel.Error, ex);
                return false;
            }
        }

        private void LogMessage(string message, LogLevel logLevel, Exception exception = null)
        {
            if (_logger != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        _logger.LogDebug(exception, message);
                        break;
                    case LogLevel.Information:
                        _logger.LogInformation(exception, message);
                        break;
                    case LogLevel.Warning:
                        _logger.LogWarning(exception, message);
                        break;
                    case LogLevel.Error:
                        _logger.LogError(exception, message);
                        break;
                    default:
                        _logger.LogInformation(exception, message);
                        break;
                }
            }
            else
            {
                // Fall back to Console if logger is not available
                if (exception != null)
                    Console.WriteLine($"[{logLevel}] {message} - Exception: {exception.Message}");
                else
                    Console.WriteLine($"[{logLevel}] {message}");
            }
        }
    }
}
