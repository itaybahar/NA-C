using Blazor_WebAssembly.Auth;
using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentService : IEquipmentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint = "api/equipment";
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<EquipmentService> _logger;
        private readonly IJSRuntime _jsRuntime;
        private readonly DynamicApiUrlHandler? _apiUrlHandler;
        private readonly ILocalStorageService? _localStorage;
        private bool _isApiAddressInitialized = false;

        public EquipmentService(
            HttpClient httpClient,
            ILogger<EquipmentService> logger,
            IJSRuntime jsRuntime = null,
            DynamicApiUrlHandler apiUrlHandler = null,
            ILocalStorageService localStorage = null)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsRuntime = jsRuntime;
            _apiUrlHandler = apiUrlHandler;
            _localStorage = localStorage;

            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        private async Task EnsureApiAddressAsync()
        {
            if (_isApiAddressInitialized)
                return;

            try
            {
                // First, try to use DynamicApiUrlHandler if available
                if (_apiUrlHandler != null)
                {
                    var client = await _apiUrlHandler.GetClientWithDiscoveredApiAsync();
                    if (client.BaseAddress != null)
                    {
                        _httpClient.BaseAddress = client.BaseAddress;
                        _logger.LogInformation($"Set HTTP client base address via DynamicApiUrlHandler to: {_httpClient.BaseAddress}");
                        _isApiAddressInitialized = true;
                        return;
                    }
                }

                // Second, try using localStorage if available
                if (_localStorage != null)
                {
                    var baseUrl = await _localStorage.GetItemAsync<string>("api_baseUrl");
                    if (!string.IsNullOrEmpty(baseUrl))
                    {
                        _httpClient.BaseAddress = new Uri(baseUrl);
                        _logger.LogInformation($"Set HTTP client base address via localStorage to: {_httpClient.BaseAddress}");
                        _isApiAddressInitialized = true;
                        return;
                    }
                }

                // Third, try using JavaScript interop if available
                if (_jsRuntime != null)
                {
                    try
                    {
                        var apiBaseUrl = await _jsRuntime.InvokeAsync<string>("apiConnection.getApiBaseUrl");
                        if (!string.IsNullOrEmpty(apiBaseUrl))
                        {
                            _httpClient.BaseAddress = new Uri(apiBaseUrl);
                            _logger.LogInformation($"Set HTTP client base address via JS interop to: {_httpClient.BaseAddress}");
                            _isApiAddressInitialized = true;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get API base URL via JS interop");
                    }
                }

                // Finally, fall back to auto-discovery
                if (_httpClient.BaseAddress == null ||
                    (!_httpClient.BaseAddress.ToString().Contains("localhost:5191") &&
                     !_httpClient.BaseAddress.ToString().Contains("localhost:7235")))
                {
                    await TryDiscoverApiAsync();
                }
                else
                {
                    _logger.LogInformation($"Using existing HTTP client base address: {_httpClient.BaseAddress}");
                    _isApiAddressInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing API address");

                // Fall back to default if all else fails
                if (_httpClient.BaseAddress == null)
                {
                    _httpClient.BaseAddress = new Uri("https://localhost:5191/");
                    _logger.LogInformation($"Set fallback HTTP client base address to: {_httpClient.BaseAddress}");
                }

                _isApiAddressInitialized = true;
            }
        }

        private async Task TryDiscoverApiAsync()
        {
            _logger.LogInformation("Attempting to auto-discover API...");

            var possibleBaseUrls = new List<string>
            {
                "https://localhost:5191/",
                "https://localhost:7235/",
                "https://localhost:5001/",
                "https://localhost:5002/",
                "https://localhost:7001/",
                "https://localhost:7002/",
                "https://localhost:7202/"
            };

            bool apiFound = false;
            foreach (var baseUrl in possibleBaseUrls)
            {
                try
                {
                    using var tempClient = new HttpClient();
                    tempClient.Timeout = TimeSpan.FromSeconds(2);

                    // Try health endpoint first
                    var response = await tempClient.GetAsync($"{baseUrl}health");

                    if (response.IsSuccessStatusCode)
                    {
                        _httpClient.BaseAddress = new Uri(baseUrl);
                        _logger.LogInformation($"API discovered at {baseUrl} via health endpoint");
                        apiFound = true;

                        // Store the discovered URL
                        if (_localStorage != null)
                        {
                            await _localStorage.SetItemAsync("api_baseUrl", baseUrl);
                        }

                        break;
                    }

                    // Try server-info endpoint as fallback
                    response = await tempClient.GetAsync($"{baseUrl}api/server-info/ports");
                    if (response.IsSuccessStatusCode)
                    {
                        _httpClient.BaseAddress = new Uri(baseUrl);
                        _logger.LogInformation($"API discovered at {baseUrl} via server-info endpoint");
                        apiFound = true;

                        // Store the discovered URL
                        if (_localStorage != null)
                        {
                            await _localStorage.SetItemAsync("api_baseUrl", baseUrl);
                        }

                        break;
                    }
                }
                catch
                {
                    // Continue to next URL if this one fails
                }
            }

            if (!apiFound)
            {
                _logger.LogWarning("Unable to discover API. Using default fallback URL.");
                _httpClient.BaseAddress = new Uri("https://localhost:5191/");
            }

            _isApiAddressInitialized = true;
        }

        public async Task<List<EquipmentModel>> GetAllEquipmentAsync()
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Fetching from: {_httpClient.BaseAddress}{_apiEndpoint}");

                // Create a custom request with timeout and headers
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _apiEndpoint);
                requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Add timeout and retry logic
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(15)); // 15 second timeout

                try
                {
                    _logger.LogInformation("Sending request to API...");
                    var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    _logger.LogInformation($"Response status: {response.StatusCode}");

                    // Handle common HTTP error codes with more specific error messages
                    if (!response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"API request failed with status: {response.StatusCode}");
                        _logger.LogWarning($"Error content: {content}");

                        // Provide more detailed diagnostics based on status code
                        switch (response.StatusCode)
                        {
                            case System.Net.HttpStatusCode.NotFound:
                                _logger.LogWarning($"API endpoint not found. Verify the endpoint '{_apiEndpoint}' exists on the server.");

                                // Try re-discovering the API if endpoint not found
                                _isApiAddressInitialized = false;
                                await EnsureApiAddressAsync();

                                break;
                            case System.Net.HttpStatusCode.Unauthorized:
                                _logger.LogWarning("API requires authentication. Ensure the authentication token is provided.");
                                break;
                            case System.Net.HttpStatusCode.Forbidden:
                                _logger.LogWarning("API access denied. Ensure the user has proper permissions.");
                                break;
                            case System.Net.HttpStatusCode.BadGateway:
                            case System.Net.HttpStatusCode.ServiceUnavailable:
                                _logger.LogWarning("API service is unavailable. The server might be down or restarting.");
                                break;
                            case System.Net.HttpStatusCode.InternalServerError:
                                _logger.LogWarning("API server error. Check the server logs for more details.");
                                break;
                        }

                        return new List<EquipmentModel>();
                    }

                    // Get the response content
                    var responseContent = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation($"Successful response received, content length: {responseContent.Length}");

                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogWarning("Warning: Empty response content");
                        return new List<EquipmentModel>();
                    }

                    // Record the raw response for debugging if needed (limiting size for logs)
                    if (responseContent.Length > 200)
                    {
                        _logger.LogDebug($"Raw API response first 200 chars: {responseContent.Substring(0, 200)}...");
                    }
                    else
                    {
                        _logger.LogDebug($"Raw API response: {responseContent}");
                    }

                    // Try multiple deserialization approaches with better error handling

                    // First attempt - try using a simpler DTO to avoid circular references
                    try
                    {
                        _logger.LogDebug("Attempting to parse using EquipmentApiDto...");
                        var apiEquipment = JsonSerializer.Deserialize<List<EquipmentApiDto>>(responseContent, _jsonOptions);

                        if (apiEquipment != null && apiEquipment.Count > 0)
                        {
                            _logger.LogInformation($"Successfully parsed {apiEquipment.Count} items with DTO");

                            // Map API DTOs to our EquipmentModel
                            return apiEquipment.Select(dto => new EquipmentModel
                            {
                                EquipmentID = dto.Id,
                                Name = dto.Name ?? "Unknown",
                                Description = dto.Description,
                                SerialNumber = dto.SerialNumber,
                                Value = dto.Value,
                                Status = dto.Status ?? "Unknown",
                                Quantity = dto.Quantity,
                                StorageLocation = dto.StorageLocation ?? "Unknown",
                                CheckoutRecords = new List<CheckoutRecord>() // Empty list since API doesn't provide this
                            }).ToList();
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning($"First parsing attempt failed: {jex.Message}");
                    }

                    // Second attempt - try direct deserialization
                    try
                    {
                        _logger.LogDebug("Attempting direct deserialization to EquipmentModel...");
                        var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(responseContent, _jsonOptions);

                        if (equipment != null && equipment.Count > 0)
                        {
                            _logger.LogInformation($"Successfully parsed {equipment.Count} items with direct deserialization");

                            // Initialize CheckoutRecords if null to prevent null reference exceptions
                            foreach (var item in equipment)
                            {
                                item.CheckoutRecords ??= new List<CheckoutRecord>();

                                // Set default values for any null properties
                                item.Name ??= "Unknown";
                                item.Status ??= "Unknown";
                                item.StorageLocation ??= "Unknown";
                            }

                            return equipment;
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning($"Direct deserialization failed: {jex.Message}");
                    }

                    // Final attempt - manual parsing from JsonDocument
                    try
                    {
                        _logger.LogDebug("Attempting manual JSON parsing...");
                        using var document = JsonDocument.Parse(responseContent);
                        var rootElement = document.RootElement;

                        List<EquipmentModel> equipmentList = new();

                        // Handle different JSON structures
                        if (rootElement.ValueKind == JsonValueKind.Array)
                        {
                            // Process each array element
                            foreach (var element in rootElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing array item: {ex.Message}");
                                }
                            }
                        }
                        else if (rootElement.ValueKind == JsonValueKind.Object)
                        {
                            // Check if it's a wrapped array with $values property
                            if (rootElement.TryGetProperty("$values", out var valuesElement) &&
                                valuesElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var element in valuesElement.EnumerateArray())
                                {
                                    try
                                    {
                                        equipmentList.Add(ParseEquipmentFromJson(element));
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning($"Error parsing $values item: {ex.Message}");
                                    }
                                }
                            }
                            else
                            {
                                // It might be a single object
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(rootElement));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing single object: {ex.Message}");
                                }
                            }
                        }

                        if (equipmentList.Count > 0)
                        {
                            _logger.LogInformation($"Successfully parsed {equipmentList.Count} items with manual parsing");
                            return equipmentList;
                        }
                        else
                        {
                            _logger.LogWarning("Manual parsing yielded no results");
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning($"JSON document parsing error: {jex.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Unexpected error during manual parsing: {ex.Message}");
                    }

                    // If all parsing attempts failed, return empty list
                    _logger.LogWarning("All parsing attempts failed. Returning empty list.");
                    return new List<EquipmentModel>();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("The request timed out. The API server might be unresponsive.");

                    // Try rediscovering API on next request if timeout occurs
                    _isApiAddressInitialized = false;

                    return new List<EquipmentModel>();
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed");
                _logger.LogWarning($"Status code: {httpEx.StatusCode}");
                _logger.LogWarning($"Inner exception: {httpEx.InnerException?.Message ?? "none"}");

                // Try rediscovering API on next request if connection fails
                _isApiAddressInitialized = false;

                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAllEquipmentAsync");
                return new List<EquipmentModel>();
            }
        }

        // Improved version of ParseEquipmentFromJson with better error handling
        private EquipmentModel ParseEquipmentFromJson(JsonElement element)
        {
            var equipment = new EquipmentModel
            {
                // Initialize required properties with defaults
                Name = "Unknown",
                Status = "Unknown",
                StorageLocation = "Unknown",
                CheckoutRecords = new List<CheckoutRecord>(),
            };

            try
            {
                // Try both camelCase and PascalCase property names for each field
                // ID field
                if (element.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.EquipmentID = idElement.GetInt32();
                }
                else if (element.TryGetProperty("Id", out idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.EquipmentID = idElement.GetInt32();
                }
                else if (element.TryGetProperty("equipmentID", out idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.EquipmentID = idElement.GetInt32();
                }
                else if (element.TryGetProperty("EquipmentID", out idElement) && idElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.EquipmentID = idElement.GetInt32();
                }

                // Name field
                if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Name = nameElement.GetString() ?? "Unknown";
                }
                else if (element.TryGetProperty("Name", out nameElement) && nameElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Name = nameElement.GetString() ?? "Unknown";
                }

                // Description field
                if (element.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Description = descElement.GetString();
                }
                else if (element.TryGetProperty("Description", out descElement) && descElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Description = descElement.GetString();
                }

                // SerialNumber field
                if (element.TryGetProperty("serialNumber", out var serialElement) && serialElement.ValueKind == JsonValueKind.String)
                {
                    equipment.SerialNumber = serialElement.GetString();
                }
                else if (element.TryGetProperty("SerialNumber", out serialElement) && serialElement.ValueKind == JsonValueKind.String)
                {
                    equipment.SerialNumber = serialElement.GetString();
                }

                // Status field
                if (element.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Status = statusElement.GetString() ?? "Unknown";
                }
                else if (element.TryGetProperty("Status", out statusElement) && statusElement.ValueKind == JsonValueKind.String)
                {
                    equipment.Status = statusElement.GetString() ?? "Unknown";
                }

                // StorageLocation field
                if (element.TryGetProperty("storageLocation", out var locationElement) && locationElement.ValueKind == JsonValueKind.String)
                {
                    equipment.StorageLocation = locationElement.GetString() ?? "Unknown";
                }
                else if (element.TryGetProperty("StorageLocation", out locationElement) && locationElement.ValueKind == JsonValueKind.String)
                {
                    equipment.StorageLocation = locationElement.GetString() ?? "Unknown";
                }

                // Handle Value property - could be string or number
                if (element.TryGetProperty("value", out var valueElement))
                {
                    if (valueElement.ValueKind == JsonValueKind.Number)
                    {
                        equipment.Value = valueElement.GetDecimal();
                    }
                    else if (valueElement.ValueKind == JsonValueKind.String && decimal.TryParse(valueElement.GetString(), out var parsedValue))
                    {
                        equipment.Value = parsedValue;
                    }
                }
                else if (element.TryGetProperty("Value", out valueElement))
                {
                    if (valueElement.ValueKind == JsonValueKind.Number)
                    {
                        equipment.Value = valueElement.GetDecimal();
                    }
                    else if (valueElement.ValueKind == JsonValueKind.String && decimal.TryParse(valueElement.GetString(), out var parsedValue))
                    {
                        equipment.Value = parsedValue;
                    }
                }

                // Handle Quantity property
                if (element.TryGetProperty("quantity", out var qtyElement) && qtyElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.Quantity = qtyElement.GetInt32();
                }
                else if (element.TryGetProperty("Quantity", out qtyElement) && qtyElement.ValueKind == JsonValueKind.Number)
                {
                    equipment.Quantity = qtyElement.GetInt32();
                }
                else if (element.TryGetProperty("quantity", out qtyElement) && qtyElement.ValueKind == JsonValueKind.String &&
                         int.TryParse(qtyElement.GetString(), out var parsedQty))
                {
                    equipment.Quantity = parsedQty;
                }
                else if (element.TryGetProperty("Quantity", out qtyElement) && qtyElement.ValueKind == JsonValueKind.String &&
                         int.TryParse(qtyElement.GetString(), out parsedQty))
                {
                    equipment.Quantity = parsedQty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error in ParseEquipmentFromJson: {ex.Message}");
            }

            return equipment;
        }

        public async Task<EquipmentModel?> GetEquipmentByIdAsync(int id)
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Fetching equipment with ID: {id}");

                var response = await _httpClient.GetAsync($"{_apiEndpoint}/{id}");
                _logger.LogInformation($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Response content: {content}");

                    // Try direct deserialization first
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<EquipmentModel>(content, _jsonOptions);
                        if (equipment != null)
                        {
                            // Initialize empty collections to prevent null reference errors
                            equipment.CheckoutRecords ??= new List<CheckoutRecord>();

                            return equipment;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Direct deserialization failed: {ex.Message}");
                    }

                    // Try manual parsing as fallback
                    try
                    {
                        using var document = JsonDocument.Parse(content);
                        var root = document.RootElement;

                        // Check if we got a direct item or a wrapped item
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            return ParseEquipmentFromJson(root);
                        }
                        else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                        {
                            return ParseEquipmentFromJson(root[0]);
                        }
                        else if (root.TryGetProperty("$values", out var valuesElement) &&
                                 valuesElement.ValueKind == JsonValueKind.Array &&
                                 valuesElement.GetArrayLength() > 0)
                        {
                            return ParseEquipmentFromJson(valuesElement[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Manual parsing failed: {ex.Message}");
                    }

                    _logger.LogWarning("Failed to parse equipment response");
                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"API returned error status: {response.StatusCode}. Content: {errorContent}");

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning($"Equipment with ID {id} not found");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in GetEquipmentByIdAsync for ID {id}");
                return null;
            }
        }

        public async Task<bool> AddEquipmentAsync(EquipmentModel equipment)
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Adding new equipment: {equipment.Name}");
                _logger.LogDebug($"API endpoint: {_httpClient.BaseAddress}{_apiEndpoint}/add");

                var response = await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/add", equipment, _jsonOptions);

                _logger.LogInformation($"Add equipment response: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Error response: {errorContent}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AddEquipmentAsync");
                return false;
            }
        }

        public async Task<bool> UpdateEquipmentAsync(EquipmentModel equipment)
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Updating equipment ID: {equipment.EquipmentID}");

                // Automatically set status to "Unavailable" if quantity is 0
                if (equipment.Quantity == 0)
                {
                    equipment.Status = "Unavailable";
                    _logger.LogInformation($"Equipment ID {equipment.EquipmentID} status set to 'Unavailable' due to zero quantity.");
                }

                // Create a retry policy for transient errors
                const int maxRetries = 3;
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        var response = await _httpClient.PutAsJsonAsync($"{_apiEndpoint}/{equipment.EquipmentID}", equipment, _jsonOptions);
                        _logger.LogInformation($"Update equipment response: {response.StatusCode}");

                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }

                        // If not successful, log the error content
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Update failed (Attempt {retry + 1}/{maxRetries}): {errorContent}");

                        // Special handling for common status codes
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning($"Equipment with ID {equipment.EquipmentID} not found");
                            return false; // Don't retry for 404
                        }

                        // For server errors, wait before retrying
                        if ((int)response.StatusCode >= 500)
                        {
                            await Task.Delay((retry + 1) * 1000); // Progressive delay
                        }
                        else
                        {
                            return false; // Don't retry for client errors other than 404
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        if (retry == maxRetries - 1)
                        {
                            _logger.LogError(httpEx, $"HTTP request failed after {maxRetries} attempts");
                            return false;
                        }

                        _logger.LogWarning($"HTTP request failed (Attempt {retry + 1}/{maxRetries}): {httpEx.Message}");
                        await Task.Delay((retry + 1) * 1000); // Progressive delay
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateEquipmentAsync");
                return false;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(int id)
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Deleting equipment ID: {id}");
                var response = await _httpClient.DeleteAsync($"{_apiEndpoint}/{id}");

                _logger.LogInformation($"Delete equipment response: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Delete failed: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DeleteEquipmentAsync");
                return false;
            }
        }

        public async Task<List<EquipmentModel>> GetAvailableEquipmentAsync()
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation("Fetching available equipment");
                var response = await _httpClient.GetAsync($"{_apiEndpoint}/available");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Available equipment response received");

                    // Try direct deserialization first
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(content, _jsonOptions);
                        if (equipment != null && equipment.Count > 0)
                        {
                            // Initialize empty collections to prevent null reference errors
                            foreach (var item in equipment)
                            {
                                item.CheckoutRecords ??= new List<CheckoutRecord>();
                            }

                            return equipment;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Direct deserialization of available equipment failed: {ex.Message}");
                    }

                    // Try manual parsing as fallback using same pattern as GetAllEquipmentAsync
                    try
                    {
                        using var document = JsonDocument.Parse(content);
                        var rootElement = document.RootElement;
                        List<EquipmentModel> equipmentList = new();

                        if (rootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in rootElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing array item: {ex.Message}");
                                }
                            }
                        }
                        else if (rootElement.ValueKind == JsonValueKind.Object &&
                                 rootElement.TryGetProperty("$values", out var valuesElement))
                        {
                            foreach (var element in valuesElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing $values item: {ex.Message}");
                                }
                            }
                        }

                        if (equipmentList.Count > 0)
                        {
                            return equipmentList;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Manual parsing of available equipment failed: {ex.Message}");
                    }
                }

                _logger.LogWarning($"GetAvailableEquipmentAsync failed with status: {response.StatusCode}");
                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAvailableEquipmentAsync");
                return new List<EquipmentModel>();
            }
        }

        public async Task<List<EquipmentModel>> FilterEquipmentByCategoryAsync(int categoryId)
        {
            await EnsureApiAddressAsync();

            try
            {
                _logger.LogInformation($"Filtering equipment by category ID: {categoryId}");
                var response = await _httpClient.GetAsync($"{_apiEndpoint}/category/{categoryId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Category equipment response received");

                    // Try the same parsing pattern as GetAllEquipmentAsync
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(content, _jsonOptions);
                        if (equipment != null && equipment.Count > 0)
                        {
                            // Initialize empty collections to prevent null reference errors
                            foreach (var item in equipment)
                            {
                                item.CheckoutRecords ??= new List<CheckoutRecord>();
                            }

                            return equipment;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Direct deserialization of category equipment failed: {ex.Message}");
                    }

                    // Try other parsing approaches
                    try
                    {
                        using var document = JsonDocument.Parse(content);
                        var rootElement = document.RootElement;
                        List<EquipmentModel> equipmentList = new();

                        if (rootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in rootElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing array item: {ex.Message}");
                                }
                            }
                        }
                        else if (rootElement.ValueKind == JsonValueKind.Object &&
                                rootElement.TryGetProperty("$values", out var valuesElement))
                        {
                            foreach (var element in valuesElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error parsing $values item: {ex.Message}");
                                }
                            }
                        }

                        if (equipmentList.Count > 0)
                        {
                            return equipmentList;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Manual parsing of category equipment failed: {ex.Message}");
                    }
                }

                _logger.LogWarning($"FilterEquipmentByCategoryAsync failed with status: {response.StatusCode}");
                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in FilterEquipmentByCategoryAsync");
                return new List<EquipmentModel>();
            }
        }

        // Additional methods required by the interface
        public async Task<int> GetAvailableQuantityAsync(int equipmentId, int totalQuantity)
        {
            try
            {
                // Get a specific client for checkout operations
                if (_httpClient.BaseAddress == null)
                {
                    await EnsureApiAddressAsync();
                }

                var response = await _httpClient.GetAsync($"api/equipmentcheckout/equipment/{equipmentId}/in-use-quantity");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(content, out int inUseQuantity))
                    {
                        return Math.Max(0, totalQuantity - inUseQuantity);
                    }
                }

                // If we can't get in-use quantity, assume total quantity is available
                return totalQuantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available quantity for equipment ID {equipmentId}");
                return totalQuantity; // Assume all are available if we can't determine
            }
        }
    }

    // Add this DTO class if you don't already have it
    public class EquipmentApiDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }
        public decimal Value { get; set; }
        public string? Status { get; set; }
        public int Quantity { get; set; }
        public string? StorageLocation { get; set; }
        public int? CategoryId { get; set; }
        public string? ModelNumber { get; set; }
    }

    // You'll also need to define/register these interfaces in Program.cs

}
