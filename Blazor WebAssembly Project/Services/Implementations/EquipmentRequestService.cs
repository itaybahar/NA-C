using System.Net.Http.Json;
using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentRequestService : IEquipmentRequestService
    {
        private readonly HttpClient _httpClient;

        public EquipmentRequestService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
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
    }
}
