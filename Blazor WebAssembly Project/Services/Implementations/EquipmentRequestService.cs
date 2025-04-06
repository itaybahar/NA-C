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
                return await _httpClient.GetFromJsonAsync<List<EquipmentRequestModel>>("equipment-requests/pending");
            }
            catch (Exception ex)
            {
                // Log the exception
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
            catch (Exception ex)
            {
                // Log the exception
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
            catch (Exception ex)
            {
                // Log the exception
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
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }
    }
}