using System.Net.Http.Json;
using Blazor_WebAssembly.Models.Equipment;
using Blazor_WebAssembly.Services.Interfaces;

namespace Blazor_WebAssembly.Services.Implementations
{
    public class EquipmentService : IEquipmentService
    {
        private readonly HttpClient _httpClient;

        public EquipmentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://localhost:7235/api/");
        }

        public async Task<List<EquipmentModel>> GetAllEquipmentAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentModel>>("equipment");
            }
            catch (Exception ex)
            {
                // Log the exception
                return new List<EquipmentModel>();
            }
        }

        public async Task<EquipmentModel> GetEquipmentByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EquipmentModel>($"equipment/{id}");
            }
            catch (Exception ex)
            {
                // Log the exception
                return null;
            }
        }

        public async Task<bool> AddEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("equipment", equipment);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> UpdateEquipmentAsync(EquipmentModel equipment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"equipment/{equipment.EquipmentID}", equipment);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> DeleteEquipmentAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"equipment/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<List<EquipmentModel>> GetAvailableEquipmentAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentModel>>("equipment/available");
            }
            catch (Exception ex)
            {
                // Log the exception
                return new List<EquipmentModel>();
            }
        }

        public async Task<List<EquipmentModel>> FilterEquipmentByCategoryAsync(int categoryId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<EquipmentModel>>($"equipment/category/{categoryId}");
            }
            catch (Exception ex)
            {
                // Log the exception
                return new List<EquipmentModel>();
            }
        }
    }
}
