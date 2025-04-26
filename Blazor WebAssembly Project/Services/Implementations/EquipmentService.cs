using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentService : IEquipmentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint = "api/equipment";
        private readonly JsonSerializerOptions _jsonOptions;

        public EquipmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // Ensure we have the correct base address
            if (_httpClient.BaseAddress == null ||
                (!_httpClient.BaseAddress.ToString().Contains("localhost:5191") &&
                 !_httpClient.BaseAddress.ToString().Contains("localhost:7235")))
            {
                _httpClient.BaseAddress = new Uri("https://localhost:5191/");
                Console.WriteLine($"Set HTTP client base address to: {_httpClient.BaseAddress}");
            }

            // Configure JSON options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true // Add this line
            };
        }

        public async Task<List<EquipmentModel>> GetAllEquipmentAsync()
        {
            try
            {
                Console.WriteLine($"Fetching from: {_httpClient.BaseAddress}{_apiEndpoint}");

                // Explicitly set Accept header for JSON
                var request = new HttpRequestMessage(HttpMethod.Get, _apiEndpoint);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                Console.WriteLine($"Response status: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API request failed with status: {response.StatusCode}");
                    Console.WriteLine($"Error content: {content}");
                    return new List<EquipmentModel>();
                }

                Console.WriteLine("Successful response received, content length: " + content.Length);

                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine("Warning: Empty response content");
                    return new List<EquipmentModel>();
                }
                // Use our new DTO to avoid the checkoutRecords issue
                try
                {
                    var apiEquipment = JsonSerializer.Deserialize<List<EquipmentApiDto>>(content, _jsonOptions);

                    if (apiEquipment != null && apiEquipment.Count > 0)
                    {
                        // Map API DTOs to our EquipmentModel
                        return apiEquipment.Select(dto => new EquipmentModel
                        {
                            EquipmentID = dto.Id,
                            Name = dto.Name,
                            Description = dto.Description,
                            SerialNumber = dto.SerialNumber,
                            Value = dto.Value,
                            Status = dto.Status,
                            Quantity = dto.Quantity,
                            StorageLocation = dto.StorageLocation,
                            CheckoutRecords = new List<CheckoutRecord>() // Empty list since API doesn't provide this
                        }).ToList();
                    }
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"JSON deserialization error: {jex.Message}");
                }
                // Try several deserialization approaches with better error handling
                try
                {
                    // Safely examine the JSON structure first
                    using var document = JsonDocument.Parse(content);
                    var rootElement = document.RootElement;

                    Console.WriteLine($"JSON root type: {rootElement.ValueKind}");

                    // Strategy 1: Try direct deserialization if it's an array
                    if (rootElement.ValueKind == JsonValueKind.Array)
                    {
                        try
                        {
                            var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(content, _jsonOptions);
                            if (equipment != null && equipment.Count > 0)
                            {
                                Console.WriteLine($"Successfully parsed {equipment.Count} items (direct)");
                                return equipment;
                            }
                        }
                        catch (JsonException jex)
                        {
                            Console.WriteLine($"Direct deserialization failed: {jex.Message}");

                            // Try manual parsing since direct deserialization failed
                            var equipmentList = new List<EquipmentModel>();
                            foreach (var element in rootElement.EnumerateArray())
                            {
                                try
                                {
                                    equipmentList.Add(ParseEquipmentFromJson(element));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error parsing array item: {ex.Message}");
                                }
                            }

                            if (equipmentList.Count > 0)
                            {
                                Console.WriteLine($"Successfully parsed {equipmentList.Count} items (manual)");
                                return equipmentList;
                            }
                        }
                    }
                    // Strategy 2: Check for $values property (common in ASP.NET Core serialization)
                    else if (rootElement.ValueKind == JsonValueKind.Object &&
                            rootElement.TryGetProperty("$values", out var valuesElement))
                    {
                        if (valuesElement.ValueKind == JsonValueKind.Array)
                        {
                            var serialized = valuesElement.GetRawText();
                            try
                            {
                                var valuesList = JsonSerializer.Deserialize<List<EquipmentModel>>(serialized, _jsonOptions);
                                if (valuesList != null && valuesList.Count > 0)
                                {
                                    Console.WriteLine($"Successfully parsed {valuesList.Count} items (values)");
                                    return valuesList;
                                }
                            }
                            catch (JsonException jex)
                            {
                                Console.WriteLine($"$values deserialization failed: {jex.Message}");

                                // Try manual parsing
                                var equipmentList = new List<EquipmentModel>();
                                foreach (var element in valuesElement.EnumerateArray())
                                {
                                    try
                                    {
                                        equipmentList.Add(ParseEquipmentFromJson(element));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing $values item: {ex.Message}");
                                    }
                                }

                                if (equipmentList.Count > 0)
                                {
                                    Console.WriteLine($"Successfully parsed {equipmentList.Count} items (manual from $values)");
                                    return equipmentList;
                                }
                            }
                        }
                    }
                    // Strategy 3: Try if it's a single object that should be wrapped in a list
                    else if (rootElement.ValueKind == JsonValueKind.Object)
                    {
                        try
                        {
                            var singleItem = ParseEquipmentFromJson(rootElement);
                            Console.WriteLine("Parsed a single equipment item");
                            return new List<EquipmentModel> { singleItem };
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing single item: {ex.Message}");
                        }
                    }

                    // If we get here, we failed all parsing attempts
                    Console.WriteLine("Unable to parse response content. JSON structure:");
                    Console.WriteLine(JsonSerializer.Serialize(
                        JsonDocument.Parse(content),
                        new JsonSerializerOptions { WriteIndented = true }
                    ));
                }
                catch (JsonException jex)
                {
                    Console.WriteLine($"Invalid JSON: {jex.Message}");
                    Console.WriteLine($"First 100 chars of content: {content.Substring(0, Math.Min(100, content.Length))}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                    Console.WriteLine($"Exception type: {ex.GetType().Name}");
                }

                Console.WriteLine("No valid equipment data found in response");
                return new List<EquipmentModel>();
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP request failed: {httpEx.Message}");
                Console.WriteLine($"Status code: {httpEx.StatusCode}");
                Console.WriteLine($"Inner exception: {httpEx.InnerException?.Message ?? "none"}");
                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllEquipmentAsync: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<EquipmentModel>();
            }
        }

        public async Task<EquipmentModel?> GetEquipmentByIdAsync(int id)
        {
            try
            {
                Console.WriteLine($"Fetching equipment with ID: {id}");

                var response = await _httpClient.GetAsync($"{_apiEndpoint}/{id}");
                Console.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response content: {content}");

                    // Try direct deserialization first
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<EquipmentModel>(content, _jsonOptions);
                        if (equipment != null)
                        {
                            return equipment;
                        }
                    }
                    catch
                    {
                        // Continue to other approaches
                    }

                    using var document = JsonDocument.Parse(content);
                    var root = document.RootElement;

                    try
                    {
                        // Check if we got a direct item or a wrapped item
                        if (root.TryGetProperty("id", out _))
                        {
                            // Direct item
                            return ParseEquipmentFromJson(root);
                        }
                        else if (root.TryGetProperty("$values", out var valuesElement) && valuesElement.GetArrayLength() > 0)
                        {
                            // If API returns a collection with one item, take the first one
                            return ParseEquipmentFromJson(valuesElement[0]);
                        }
                        else
                        {
                            Console.WriteLine("Unexpected JSON structure");
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing equipment: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine($"API returned error status: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetEquipmentByIdAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        // Helper method to parse equipment from JSON
        private EquipmentModel ParseEquipmentFromJson(JsonElement element)
        {
            var model = new EquipmentModel
            {
                // Use TryGetProperty for more resilient parsing
                EquipmentID = element.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : 0,
                Name = GetStringProperty(element, "name"),
                Description = GetStringProperty(element, "description"),
                SerialNumber = GetStringProperty(element, "serialNumber"),
                Status = GetStringProperty(element, "status"),
                StorageLocation = GetStringProperty(element, "storageLocation"),
                CheckoutRecords = new List<CheckoutRecord>()
            };

            // Handle numeric properties safely
            if (element.TryGetProperty("quantity", out var qtyElement) && qtyElement.ValueKind == JsonValueKind.Number)
            {
                model.Quantity = qtyElement.GetInt32();
            }

            if (element.TryGetProperty("value", out var valueElement))
            {
                if (valueElement.ValueKind == JsonValueKind.Number)
                {
                    model.Value = valueElement.GetDecimal();
                }
                else if (valueElement.ValueKind == JsonValueKind.String && decimal.TryParse(valueElement.GetString(), out var value))
                {
                    model.Value = value;
                }
            }

            return model;
        }

        // Helper method to safely get string property with fallback to empty string
        private string GetStringProperty(JsonElement element, string propertyName)
        {
            // Try both camelCase and original property name
            string camelCaseProperty = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);

            if (element.TryGetProperty(propertyName, out var propElement) && propElement.ValueKind == JsonValueKind.String)
            {
                return propElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty(camelCaseProperty, out var camelPropElement) && camelPropElement.ValueKind == JsonValueKind.String)
            {
                return camelPropElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        public async Task<bool> AddEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                // Log full request details for debugging
                Console.WriteLine($"Adding new equipment: {equipment.Name}");
                Console.WriteLine($"API endpoint: {_httpClient.BaseAddress}{_apiEndpoint}/add");

                // Convert to JSON for logging
                var jsonContent = JsonSerializer.Serialize(equipment, _jsonOptions);
                Console.WriteLine($"Request payload: {jsonContent}");

                var response = await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/add", equipment, _jsonOptions);

                Console.WriteLine($"Add equipment response: {response.StatusCode}");

                // If failed, log the response body
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddEquipmentAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> UpdateEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                Console.WriteLine($"Updating equipment ID: {equipment.EquipmentID}");
                var response = await _httpClient.PutAsJsonAsync($"{_apiEndpoint}/{equipment.EquipmentID}", equipment, _jsonOptions);

                Console.WriteLine($"Update equipment response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(int id)
        {
            try
            {
                Console.WriteLine($"Deleting equipment ID: {id}");
                var response = await _httpClient.DeleteAsync($"{_apiEndpoint}/{id}");

                Console.WriteLine($"Delete equipment response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentModel>> GetAvailableEquipmentAsync()
        {
            try
            {
                Console.WriteLine("Fetching available equipment");
                var response = await _httpClient.GetAsync($"{_apiEndpoint}/available");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Available equipment response: {content}");

                    // Try direct deserialization first
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(content, _jsonOptions);
                        if (equipment != null && equipment.Count > 0)
                        {
                            return equipment;
                        }
                    }
                    catch
                    {
                        // Continue to other approaches
                    }

                    // Try to parse the response with the same pattern as GetAllEquipmentAsync
                    using var document = JsonDocument.Parse(content);

                    if (document.RootElement.TryGetProperty("$values", out var valuesElement))
                    {
                        var equipmentList = new List<EquipmentModel>();
                        foreach (var item in valuesElement.EnumerateArray())
                        {
                            try
                            {
                                equipmentList.Add(ParseEquipmentFromJson(item));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing available item: {ex.Message}");
                            }
                        }
                        return equipmentList;
                    }
                    else if (document.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var equipmentList = new List<EquipmentModel>();
                        foreach (var item in document.RootElement.EnumerateArray())
                        {
                            try
                            {
                                equipmentList.Add(ParseEquipmentFromJson(item));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing available item: {ex.Message}");
                            }
                        }
                        return equipmentList;
                    }
                }

                Console.WriteLine($"GetAvailableEquipmentAsync failed with status: {response.StatusCode}");
                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAvailableEquipmentAsync: {ex.Message}");
                return new List<EquipmentModel>();
            }
        }

        public async Task<List<EquipmentModel>> FilterEquipmentByCategoryAsync(int categoryId)
        {
            try
            {
                Console.WriteLine($"Filtering equipment by category ID: {categoryId}");
                var response = await _httpClient.GetAsync($"{_apiEndpoint}/category/{categoryId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Category equipment response: {content}");

                    // Try direct deserialization first
                    try
                    {
                        var equipment = JsonSerializer.Deserialize<List<EquipmentModel>>(content, _jsonOptions);
                        if (equipment != null && equipment.Count > 0)
                        {
                            return equipment;
                        }
                    }
                    catch
                    {
                        // Continue to other approaches
                    }

                    // Try to parse the response with the same pattern as GetAllEquipmentAsync
                    using var document = JsonDocument.Parse(content);

                    if (document.RootElement.TryGetProperty("$values", out var valuesElement))
                    {
                        var equipmentList = new List<EquipmentModel>();
                        foreach (var item in valuesElement.EnumerateArray())
                        {
                            try
                            {
                                equipmentList.Add(ParseEquipmentFromJson(item));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing category item: {ex.Message}");
                            }
                        }
                        return equipmentList;
                    }
                    else if (document.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var equipmentList = new List<EquipmentModel>();
                        foreach (var item in document.RootElement.EnumerateArray())
                        {
                            try
                            {
                                equipmentList.Add(ParseEquipmentFromJson(item));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing category item: {ex.Message}");
                            }
                        }
                        return equipmentList;
                    }
                }

                Console.WriteLine($"FilterEquipmentByCategoryAsync failed with status: {response.StatusCode}");
                return new List<EquipmentModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in FilterEquipmentByCategoryAsync: {ex.Message}");
                return new List<EquipmentModel>();
            }
        }
    }
}
