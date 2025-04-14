using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using Domain_Project.DTOs.Domain_Project.DTOs.Domain_Project.Models;
using Domain_Project.Models;
using System.Net.Http.Json;
using System.Text.Json;


namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentService : IEquipmentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint = "api/equipment";

        public EquipmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Don't set BaseAddress here - it's already set in Program.cs
        }

        public async Task<List<EquipmentModel>> GetAllEquipmentAsync()
        {
            try
            {
                Console.WriteLine($"Fetching from: {_httpClient.BaseAddress}{_apiEndpoint}");

                // Get the response and check the status
                var response = await _httpClient.GetAsync(_apiEndpoint);
                Console.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    // Read the raw content first
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response content: {content}");

                    // Try to deserialize the response
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // Check if the response is a direct array
                    var equipmentList = JsonSerializer.Deserialize<List<EquipmentModel>>(content, options);
                    if (equipmentList != null)
                    {
                        Console.WriteLine($"Successfully parsed {equipmentList.Count} items");
                        return equipmentList;
                    }

                    Console.WriteLine("No valid equipment data found in response");
                    return new List<EquipmentModel>();
                }
                else
                {
                    Console.WriteLine($"API request failed with status: {response.StatusCode}");
                    return new List<EquipmentModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllEquipmentAsync: {ex.Message}");
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
            return new EquipmentModel
            {
                EquipmentID = element.GetProperty("id").GetInt32(),
                Name = element.GetProperty("name").GetString() ?? string.Empty,
                Status = element.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty,
                Quantity = element.TryGetProperty("quantity", out var qtyElement) ? qtyElement.GetInt32() : 0,
                StorageLocation = element.TryGetProperty("storageLocation", out var locElement) ? locElement.GetString() ?? string.Empty : string.Empty,
                CheckoutRecords = new List<CheckoutRecordDto>() // Initialize as empty list
            };
        }

        public async Task<bool> AddEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                Console.WriteLine($"Adding new equipment: {equipment.Name}");
                var response = await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/add", equipment);

                Console.WriteLine($"Add equipment response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddEquipmentAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                Console.WriteLine($"Updating equipment ID: {equipment.EquipmentID}");
                var response = await _httpClient.PutAsJsonAsync($"{_apiEndpoint}/{equipment.EquipmentID}", equipment);

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
