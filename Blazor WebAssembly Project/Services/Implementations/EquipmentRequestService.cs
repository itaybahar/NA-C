using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentRequestService : IEquipmentRequestService
    {
        private readonly HttpClient _httpClient;

        public EquipmentRequestService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            //_httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
        }

        public async Task<List<EquipmentRequestModel>> GetPendingRequestsAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<EquipmentRequestModel>>("equipment-requests/pending");
                return result ?? new List<EquipmentRequestModel>(); // Fix for CS8603: Handle possible null return
            }
            catch (Exception)
            {
                // Log the exception (if needed)
                return new List<EquipmentRequestModel>();
            }
        }

        public async Task<bool> CreateEquipmentRequestAsync(EquipmentRequestModel request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("equipment-requests", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception (if needed)
                return false;
            }
        }

        public async Task<bool> ApproveRequestAsync(int requestId)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"equipment-requests/{requestId}/approve", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception (if needed)
                return false;
            }
        }

        public async Task<bool> RejectRequestAsync(int requestId, string reason)
        {
            try
            {
                var response = await _httpClient.PatchAsync(
                    $"equipment-requests/{requestId}/reject",
                    new StringContent(reason)
                );
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception (if needed)
                return false;
            }
        }

        public async Task SendEquipmentRequestAsync(string message)
        {
            try
            {
                var content = new StringContent(message);
                var response = await _httpClient.PostAsync("equipment-requests/send", content);
                if (!response.IsSuccessStatusCode)
                {
                    // Log the failure (if needed)
                }
            }
            catch (Exception)
            {
                // Log the exception (if needed)
            }
        }
        // New method to get checked-out equipment for a specific team
        public async Task<List<EquipmentModel>> GetCheckedOutEquipmentByTeamAsync(int teamId)
        {
            try
            {
                // Fetch the checked-out equipment for the given team ID
                var result = await _httpClient.GetFromJsonAsync<List<JsonElement>>($"EquipmentCheckout/team/{teamId}");

                if (result == null || !result.Any())
                {
                    Console.WriteLine($"No checked-out equipment found for team {teamId}.");
                    return new List<EquipmentModel>();
                }

                // Extract the "equipment" field from each entry
                var equipmentList = new List<EquipmentModel>();
                foreach (var item in result)
                {
                    if (item.TryGetProperty("equipment", out var equipmentElement))
                    {
                        try
                        {
                            var equipment = JsonSerializer.Deserialize<EquipmentModel>(equipmentElement.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (equipment != null)
                            {
                                equipmentList.Add(equipment);
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"Error deserializing equipment: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"Loaded {equipmentList.Count} equipment items for team {teamId}.");
                return equipmentList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching checked-out equipment for team {teamId}: {ex.Message}");
                return new List<EquipmentModel>();
            }
        }

    }
}
